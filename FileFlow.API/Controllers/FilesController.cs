using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileFlow.API.Data;
using FileFlow.API.Models;
using FileFlow.API.DTOs;
using FileFlow.API.Services;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Data.SqlTypes;

namespace FileFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var files = await _context.FileItems.ToListAsync();

        // Convert the database entries into the response
        var response = files.Select(file => FileResponse.FromEntity(file)).ToList();
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

        // Temporary user ID since authentication isn't set up yet
        int mockUserId = 1;

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
            mockUserId,
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

        string fullDiskPath = Path.Combine(_storagePath, file.StoredFileName);

        if (System.IO.File.Exists(fullDiskPath))
        {
            System.IO.File.Delete(fullDiskPath);
        }

        _context.FileItems.Remove(file);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}