using System;
using System.ComponentModel.DataAnnotations;

namespace FileFlow.API.Models
{
    public class User
    {
        public int Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }

        private User() { }
        public User(string email, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentNullException(nameof(email), "Email is required.");
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                throw new ArgumentException("Email is not valid.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required.", nameof(passwordHash));

            Email = email;
            PasswordHash = passwordHash;
            CreatedAt = DateTime.UtcNow;
        }
    }
}