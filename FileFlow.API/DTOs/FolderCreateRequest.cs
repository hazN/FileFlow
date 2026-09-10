namespace FileFlow.API.DTOs
{
    public class FolderCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
    }
}