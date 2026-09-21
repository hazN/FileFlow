namespace FileFlow.API.Options
{

    public class FileUploadOptions
    {
        public long MaxFileSizeBytes { get; set; }
        public List<string> AllowedExtensions { get; set; } = new();
    }
}