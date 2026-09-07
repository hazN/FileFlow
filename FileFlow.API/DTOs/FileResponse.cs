using FileFlow.API.Models;

namespace FileFlow.API.DTOs;

// Data that is sent back to the user
public class FileResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? FolderId { get; set; }

    // Convertg database entity into response object
    public static FileResponse FromEntity(FileItem file) => new()
    {
        Id = file.Id,
        Name = file.Name,
        ContentType = file.ContentType,
        Size = file.Size,
        CreatedAt = file.CreatedAt,
        FolderId = file.FolderId
    };
}
