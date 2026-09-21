using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FileFlow.API.Controllers;
using FileFlow.API.Data;
using FileFlow.API.DTOs;
using FileFlow.API.Models;
using FileFlow.API.Options;
using FileFlow.API.Services;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace FileFlow.Tests.Controllers
{

    public class FilesControllerTests
    {
        // Creates a new in memory DB so that tests do not interfere with each other
        private FileFlowDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<FileFlowDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new FileFlowDbContext(options);
        }

        private static ClaimsPrincipal CreateUserPrincipal(int userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task GetFile_WhenFileBelongsToAnotherUser_ReturnsNotFound()
        {
            // Arrange
            await using var context = CreateContext();

            var otherUsersFile = new FileItem("secret.pdf", "path/secret.pdf", "application/pdf", 1024, userId: 999, folderId: null);
            context.FileItems.Add(otherUsersFile);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId: 1) }
            };

            // Act — user 1 tries to access user 999's file
            var result = await controller.GetFile(otherUsersFile.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetFiles_OnlyReturnsFilesOwnedByCurrentUser()
        {
            await using var context = CreateContext();

            context.FileItems.Add(new FileItem("mine.txt", "path/mine.txt", "text/plain", 100, userId: 1, folderId: null));
            context.FileItems.Add(new FileItem("theirs.txt", "path/theirs.txt", "text/plain", 100, userId: 2, folderId: null));
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId: 1) }
            };

            var result = await controller.GetFiles();
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var files = Assert.IsAssignableFrom<IEnumerable<FileFlow.API.DTOs.FileResponse>>(okResult.Value);

            Assert.Single(files); // only "mine.txt" should come back
        }

        [Fact]
        public async Task DeleteFile_WhenFileBelongsToAnotherUser_ReturnsNotFoundAndKeepsFile()
        {
            await using var context = CreateContext();
            var otherUsersFile = new FileItem("secret.txt", "path/secret.txt", "text/plain", 100, userId: 2);
            context.FileItems.Add(otherUsersFile);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1);

            var result = await controller.DeleteFile(otherUsersFile.Id);

            Assert.IsType<NotFoundResult>(result);
            Assert.NotNull(await context.FileItems.FindAsync(otherUsersFile.Id));
        }

        [Fact]
        public async Task UploadFile_WhenFileIsTooLarge_ReturnsBadRequest()
        {
            await using var context = CreateContext();
            var controller = CreateController(context, userId: 1, maxFileSizeBytes: 10);
            await using var stream = new MemoryStream(new byte[11]);
            var file = new FormFile(stream, 0, stream.Length, "filePayload", "document.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var result = await controller.UploadFile(new FileUploadRequest(), file);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(context.FileItems);
        }

        [Fact]
        public async Task UploadFile_WhenExtensionIsNotAllowed_ReturnsBadRequest()
        {
            await using var context = CreateContext();
            var controller = CreateController(context, userId: 1, allowedExtensions: new List<string> { ".txt" });
            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var file = new FormFile(stream, 0, stream.Length, "filePayload", "malware.exe")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };

            var result = await controller.UploadFile(new FileUploadRequest(), file);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(context.FileItems);
        }

        private static FilesController CreateController(
            FileFlowDbContext context,
            int userId,
            long maxFileSizeBytes = 100 * 1024 * 1024,
            List<string>? allowedExtensions = null)
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "FileFlowTests", Guid.NewGuid().ToString());
            var controller = new FilesController(
                context,
                new TestWebHostEnvironment(rootPath),
                new FolderPathResolver(context),
                Options.Create(new FileUploadOptions
                {
                    MaxFileSizeBytes = maxFileSizeBytes,
                    AllowedExtensions = allowedExtensions ?? new List<string> { ".txt", ".pdf" }
                }));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId) }
            };

            return controller;
        }

        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public TestWebHostEnvironment(string contentRootPath)
            {
                Directory.CreateDirectory(contentRootPath);
                ContentRootPath = contentRootPath;
                ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
            }

            public string ApplicationName { get; set; } = "FileFlow.Tests";
            public IFileProvider ContentRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; } = "Test";
            public string WebRootPath { get; set; } = string.Empty;
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}