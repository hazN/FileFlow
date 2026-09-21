using System.IO;

namespace FileFlow.API.Validation
{
    public static class FolderNameValidator
    {
        public static bool IsValid(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Contains("..") || name.Contains('/') || name.Contains('\\'))
                return false;

            return name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}