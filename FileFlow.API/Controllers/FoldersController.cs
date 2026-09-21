using FileFlow.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FileFlow.API.DTOs;
using FileFlow.API.Models;
using FileFlow.API.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FileFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FoldersController : ControllerBase
    {
        private readonly FileFlowDbContext _context;

        // Physical location of storage folder on disk
        private readonly string _storagePath;

        // Resolves a FolderId into a real on-disk folder path
        private readonly FolderPathResolver _pathResolver;

        public FoldersController(FileFlowDbContext context, FolderPathResolver pathResolver, IWebHostEnvironment env)
        {
            _context = context;
            _pathResolver = pathResolver;
            _storagePath = Path.Combine(env.ContentRootPath, "Storage");

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        // GET: api/Folders
        // Return all folders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FolderResponse>>> GetFolders()
        {
            int userId = GetCurrentUserId();
            var folders = await _context.Folders
                .Where(folder => folder.UserId == userId)
                .ToListAsync();

            var response = folders.Select(FolderResponse.FromFolder).ToList();

            return Ok(response);
        }

        // GET: api/Folders/id
        // Return specific folder
        [HttpGet("{id}")]
        public async Task<ActionResult<FolderResponse>> GetFolder(int id)
        {
            var folder = await _context.Folders.FindAsync(id);

            if (folder == null || folder.UserId != GetCurrentUserId())
            {
                return NotFound();
            }

            var response = FolderResponse.FromFolder(folder);

            return Ok(response);
        }

        // GET: api/Folders/id/content
        // Return the contents of a specific folder
        [HttpGet("{id}/contents")]
        public async Task<ActionResult<IEnumerable<FileResponse>>> GetFolderContents(int id)
        {
            var folder = await _context.Folders.FindAsync(id);

            int userId = GetCurrentUserId();
            if (folder == null || folder.UserId != userId)
            {
                return NotFound();
            }

            var subFolders = await _context.Folders
                .Where(f => f.ParentFolderId == id && f.UserId == userId)
                .ToListAsync();
            var files = await _context.FileItems
                .Where(f => f.FolderId == id && f.UserId == userId)
                .ToListAsync();

            return Ok(new
            {
                Folder = FolderResponse.FromFolder(folder),
                SubFolders = subFolders,
                Files = files
            });
        }

        // POST: api/Folders
        // Create a new folder
        [HttpPost]
        public async Task<ActionResult<FolderResponse>> CreateFolder(FolderCreateRequest request)
        {
            int userId = GetCurrentUserId();

            Folder folder;
            try
            {
                folder = new Folder(request.Name, userId, request.ParentFolderId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();

            // Resolve the parent's path, then append this folder's own name,
            // and actually create the physical directory on disk
            string parentRelativePath = await _pathResolver.GetRelativePathAsync(request.ParentFolderId);
            string thisFolderRelativePath = Path.Combine(parentRelativePath, folder.Name);
            string fullDiskPath = Path.Combine(_storagePath, thisFolderRelativePath);

            Directory.CreateDirectory(fullDiskPath);

            var response = FolderResponse.FromFolder(folder);

            return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, response);
        }

        // PUT: api/Folders/id
        // Update an existing folder (move/rename)
        [HttpPut("{id}")]
        public async Task<ActionResult<FolderResponse>> UpdateFolder(int id, FolderCreateRequest request)
        {
            var folder = await _context.Folders.FindAsync(id);

            if (folder == null || folder.UserId != GetCurrentUserId())
            {
                return NotFound();
            }

            string oldParentRelative = await _pathResolver.GetRelativePathAsync(folder.ParentFolderId);
            string oldRelativePath = Path.Combine(oldParentRelative, folder.Name);
            string oldFullPath = Path.Combine(_storagePath, oldRelativePath);

            try
            {
                folder.Rename(request.Name);
                folder.MoveTo(request.ParentFolderId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            string newParentRelative = await _pathResolver.GetRelativePathAsync(folder.ParentFolderId);
            string newRelativePath = Path.Combine(newParentRelative, folder.Name);
            string newFullPath = Path.Combine(_storagePath, newRelativePath);

            // Directory.Move handles rename AND move-to-new-parent in one call
            if (oldFullPath != newFullPath && Directory.Exists(oldFullPath))
            {
                Directory.Move(oldFullPath, newFullPath);
            }

            await _context.SaveChangesAsync();

            var response = FolderResponse.FromFolder(folder);

            return Ok(response);
        }

        // DELETE: api/Folders/id
        // Delete an existing folder
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var folder = await _context.Folders.FindAsync(id);

            if (folder == null || folder.UserId != GetCurrentUserId())
            {
                return NotFound();
            }

            bool hasFiles = await _context.FileItems.AnyAsync(f => f.FolderId == id);
            bool hasSubFolders = await _context.Folders.AnyAsync(f => f.ParentFolderId == id);

            if (hasFiles || hasSubFolders)
            {
                return BadRequest("Cannot delete a folder that contains files or subfolders.");
            }

            // Resolve this folder's physical path and delete it from disk too
            string parentRelative = await _pathResolver.GetRelativePathAsync(folder.ParentFolderId);
            string relativePath = Path.Combine(parentRelative, folder.Name);
            string fullPath = Path.Combine(_storagePath, relativePath);

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath);
            }

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();

            return NoContent();
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
}