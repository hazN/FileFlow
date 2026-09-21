using System.Security.Claims;
using FileFlow.API.Controllers;
using FileFlow.API.Data;
using FileFlow.API.Models;
using FileFlow.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace FileFlow.Tests.Controllers
{
    public class FoldersControllerTests
    {
        [Fact]
        public async Task GetFolder_WhenFolderBelongsToAnotherUser_ReturnsNotFound()
        {
            await using var context = CreateContext();
            var otherUsersFolder = new Folder("Private", 2);
            context.Folders.Add(otherUsersFolder);
            await context.SaveChangesAsync();

            var controller = CreateController(context, 1);

            var result = await controller.GetFolder(otherUsersFolder.Id);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetFolder_WhenFolderBelongsToCurrentUser_ReturnsFolder()
        {
            await using var context = CreateContext();
            var usersFolder = new Folder("Documents", 1);
            context.Folders.Add(usersFolder);
            await context.SaveChangesAsync();

            var controller = CreateController(context, 1);

            var result = await controller.GetFolder(usersFolder.Id);

            var response = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("Documents", ((FileFlow.API.DTOs.FolderResponse)response.Value!).Name);
        }

        private static FileFlowDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<FileFlowDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new FileFlowDbContext(options);
        }

        private static FoldersController CreateController(FileFlowDbContext context, int userId)
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "FileFlowTests", Guid.NewGuid().ToString());
            var controller = new FoldersController(context, new FolderPathResolver(context), new TestWebHostEnvironment(rootPath));
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "Test");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
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