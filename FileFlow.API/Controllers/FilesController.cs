using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileFlow.API.Data;
using FileFlow.API.Models;
using FileFlow.API.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly FileFlowDbContext _context;

    public FilesController(FileFlowDbContext context)
    {
        _context = context;
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
        string generatedStoragePath = Path.GetRandomFileName();

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

        // Temp returning data details
        return Ok(new { Message = "Ready to download", Path = file.StoredFileName });
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

        _context.FileItems.Remove(file);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
