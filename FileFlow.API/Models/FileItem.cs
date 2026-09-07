using System;

namespace FileFlow.API.Models
{
    public class FileItem
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string StoredFileName { get; private set; } = string.Empty;
        public string ContentType { get; private set; } = string.Empty;
        public long Size { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public int? FolderId { get; private set; }
        public int UserId { get; private set; }

        private FileItem() { }
        public FileItem(string name, string storedFileName, string contentType, long size, int userId, int? folderId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("File name cannot be empty.", nameof(Name));
            if (size <= 0)
                throw new ArgumentException("File size must be greater than zero.", nameof(size));

            Name = name;
            StoredFileName = storedFileName;
            ContentType = contentType;
            Size = size;
            CreatedAt = DateTime.UtcNow;
            UserId = userId;
            FolderId = folderId;
        }

        public void MoveToFolder(int? folderId)
        {
            FolderId = folderId;
        }

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("File name cannot be empty.", nameof(newName));

            Name = newName;
        }
    }
}