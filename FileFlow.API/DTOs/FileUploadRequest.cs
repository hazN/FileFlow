namespace FileFlow.API.DTOs
{
    // What the user sends when uploading a file
    public class FileUploadRequest
    {
        public int? FolderId { get; set; }
    }
}