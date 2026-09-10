using FileFlow.API.Models;

namespace FileFlow.API.DTOs;

// Data sent back to the user when they request folder info
public class FolderResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentFolderId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static FolderResponse FromFolder(Folder folder) => new()
    {
        Id = folder.Id,
        Name = folder.Name,
        ParentFolderId = folder.ParentFolderId,
        CreatedAt = folder.CreatedAt
    };
}
