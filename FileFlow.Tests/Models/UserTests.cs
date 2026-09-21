using FileFlow.API.Models;
using Xunit;

namespace FileFlow.Tests.Models
{
    public class UserTests
    {
        [Fact]
        public void Constructor_WithValidData_CreatesUser()
        {
            var user = new User("test@example.com", "hashedpassword123");

            Assert.Equal("test@example.com", user.Email);
            Assert.Equal("hashedpassword123", user.PasswordHash);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithEmptyEmail_ThrowsArgumentException(string invalidEmail)
        {
            Assert.ThrowsAny<ArgumentException>(() => new User(invalidEmail, "hashedpassword123"));
        }

        [Fact]
        public void Constructor_WithoutAtSymbol_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new User("notanemail", "hashedpassword123"));
        }

        [Fact]
        public void Constructor_WithEmptyPasswordHash_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new User("test@example.com", ""));
        }
    }
}