using System;
using System.IO;

namespace FileFlow.API.Models
{
    public class Folder
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public int? ParentFolderId { get; private set; }
        public int UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Folder() { }

        public Folder(string name, int userId, int? parentFolderId = null)
        {
            ValidateName(name);

            Name = name;
            UserId = userId;
            ParentFolderId = parentFolderId;
            CreatedAt = DateTime.UtcNow;
        }

        public void Rename(string newName)
        {
            ValidateName(newName);

            Name = newName;
        }

        public void MoveTo(int? parentFolderId)
        {
            if (parentFolderId == Id)
                throw new ArgumentException("A folder cannot be its own parent.", nameof(parentFolderId));

            ParentFolderId = parentFolderId;
        }
        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name cannot be empty.", nameof(name));

            if (name.Contains("..") || name.Contains('/') || name.Contains('\\'))
                throw new ArgumentException("Folder name cannot contain path separators or '..'.", nameof(name));

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Folder name contains invalid characters.", nameof(name));
        }
    }
}