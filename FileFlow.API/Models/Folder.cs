using System;

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
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name cannot be empty.", nameof(name));

            Name = name;
            UserId = userId;
            ParentFolderId = parentFolderId;
            CreatedAt = DateTime.UtcNow;
        }

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Folder name cannot be empty.", nameof(newName));

            Name = newName;
        }

        public void MoveTo(int? parentFolderId)
        {
            if (parentFolderId == Id)
                throw new ArgumentException("A folder cannot be its own parent.", nameof(parentFolderId));

            ParentFolderId = parentFolderId;
        }
    }
}