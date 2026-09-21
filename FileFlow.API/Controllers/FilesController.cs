using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FileFlow.API.Data;
using FileFlow.API.Models;
using FileFlow.API.DTOs;
using FileFlow.API.Services;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Data.SqlTypes;

namespace FileFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly FileFlowDbContext _context;

    // Physical location of storage folder on disk
    private readonly string _storagePath;

    // Turns a FolderId into a real folder path
    private readonly FolderPathResolver _pathResolver;

    public FilesController(FileFlowDbContext context, IWebHostEnvironment env, FolderPathResolver pathResolver)
    {
        _context = context;
        _pathResolver = pathResolver;
        _storagePath = Path.Combine(env.ContentRootPath, "Storage");

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    // GET /api/files
    // Gets all files from the database and maps them to DTO responses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileResponse>>> GetFiles()
    {
        int userId = GetCurrentUserId();
        var files = await _context.FileItems
            .Where(file => file.UserId == userId)
            .ToListAsync();

        // Convert the database entries into the response
        var response = files.Select(file => FileResponse.FromEntity(file)).ToList();
        return Ok(response);
    }

    // GET /api/files/search?query=resume
    // Search files by name 
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<FileResponse>>> SearchFiles([FromQuery] string query,
     [FromQuery] string? sort = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Search cannot be empty.");
        }

        int userId = GetCurrentUserId();
        var filesQuery = _context.FileItems
            .Where(file => file.UserId == userId && file.Name.Contains(query));

        filesQuery = sort switch
        {
            "name_asc" => filesQuery.OrderBy(f => f.Name),
            "name_desc" => filesQuery.OrderByDescending(f => f.Name),
            "size_asc" => filesQuery.OrderBy(f => f.Size),
            "size_desc" => filesQuery.OrderByDescending(f => f.Size),
            "created_asc" => filesQuery.OrderBy(f => f.CreatedAt),
            "created_desc" => filesQuery.OrderByDescending(f => f.CreatedAt),
            _ => filesQuery
        };

        var files = await filesQuery.ToListAsync();
        var response = files.Select(FileResponse.FromEntity).ToList();

        return Ok(response);
    }

    // GET /api/files/{id}
    // Finds a specific file by its ID
    [HttpGet("{id}")]
    public async Task<ActionResult<FileResponse>> GetFile(int id)
    {
        var file = await _context.FileItems.FindAsync(id);

        if (file == null)
        {
            return NotFound();
        }

        if (file.UserId != GetCurrentUserId())
        {
            return NotFound();
        }

        return Ok(FileResponse.FromEntity(file));
    }

    // POST /api/files/upload
    // Receives a folder destination and a file from the user
    [HttpPost("upload")]
    public async Task<ActionResult<FileResponse>> UploadFile([FromForm] FileUploadRequest request, IFormFile filePayload)
    {
        if (filePayload == null || filePayload.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        int userId = GetCurrentUserId();

        // Use random unique name because user file name may include characters like ../ that break the path
        string randomName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()); // Gets "hfgfwctf"

        // Keep extension like png/jpg
        string extension = Path.GetExtension(filePayload.FileName);

        string generatedFileName = randomName + extension; // Combines them into "hfgfwctf.jpg"

        // Resolve which real on-disk subfolder this file belongs in,
        // based on the folder tree (e.g. "Documents/School")
        string relativeFolderPath = await _pathResolver.GetRelativePathAsync(request.FolderId);
        string fullFolderPath = Path.Combine(_storagePath, relativeFolderPath);

        // Make sure that folder actually exists on disk before writing into it
        if (!Directory.Exists(fullFolderPath))
        {
            Directory.CreateDirectory(fullFolderPath);
        }

        // StoredFileName now holds the RELATIVE path (folder + filename)
        // so download/delete can find the file later, wherever it lives
        string generatedStoragePath = Path.Combine(relativeFolderPath, generatedFileName);

        // Path where the physical bytes will be
        string fullDiskPath = Path.Combine(_storagePath, generatedStoragePath);

        // Write the file to disk
        using (var stream = new FileStream(fullDiskPath, FileMode.Create))
        {
            await filePayload.CopyToAsync(stream);
        }

        // Construct new file from payload
        var fileItem = new FileItem(
            filePayload.FileName,
            generatedStoragePath,
            filePayload.ContentType,
            filePayload.Length,
            userId,
            request.FolderId
        );

        _context.FileItems.Add(fileItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFile), new { id = fileItem.Id }, FileResponse.FromEntity(fileItem));
    }

    // GET /api/files/{id}/download
    // Pulls metadata from DB
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var file = await _context.FileItems.FindAsync(id);

        if (file == null)
        {
            return NotFound();
        }

        if (file.UserId != GetCurrentUserId())
        {
            return NotFound();
        }

        var fullDiskPath = Path.Combine(_storagePath, file.StoredFileName);

        if (!System.IO.File.Exists(fullDiskPath))
        {
            return NotFound();
        }

        return PhysicalFile(Path.GetFullPath(fullDiskPath), file.ContentType, file.Name);
    }

    // DELETE /api/files/{id}
    // Deletes the file record out of the database
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var file = await _context.FileItems.FindAsync(id);

        if (file == null)
        {
            return NotFound();
        }

        if (file.UserId != GetCurrentUserId())
        {
            return NotFound();
        }

        string fullDiskPath = Path.Combine(_storagePath, file.StoredFileName);

        if (System.IO.File.Exists(fullDiskPath))
        {
            System.IO.File.Delete(fullDiskPath);
        }

        _context.FileItems.Remove(file);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT /api/files/{id}/move
    // Moves a file to a different folder
    [HttpPut("{id}/move")]
    public async Task<ActionResult<FileResponse>> MoveFile(int id, [FromBody] FileMoveRequest request)
    {
        var file = await _context.FileItems.FindAsync(id);

        if (file == null)
        {
            return NotFound();
        }

        if (file.UserId != GetCurrentUserId())
        {
            return NotFound();
        }

        string oldFullPath = Path.Combine(_storagePath, file.StoredFileName);

        file.MoveToFolder(request.FolderId);

        string newRelativeFolderPath = await _pathResolver.GetRelativePathAsync(request.FolderId);
        string newFolderFullPath = Path.Combine(_storagePath, newRelativeFolderPath);

        if (!Directory.Exists(newFolderFullPath))
        {
            Directory.CreateDirectory(newFolderFullPath);
        }

        string fileName = Path.GetFileName(file.StoredFileName);
        string newStoredFileName = Path.Combine(newRelativeFolderPath, fileName);
        string newFullPath = Path.Combine(_storagePath, newStoredFileName);

        if (oldFullPath != newFullPath && System.IO.File.Exists(oldFullPath))
        {
            System.IO.File.Move(oldFullPath, newFullPath);
        }

        file.UpdateStoredFileName(newStoredFileName);

        await _context.SaveChangesAsync();

        return Ok(FileResponse.FromEntity(file));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException("No user ID found in token.");
        }

        return int.Parse(userIdClaim);
    }
}