using FileFlow.API.Models;
using Microsoft.EntityFrameworkCore;
namespace FileFlow.API.Data
{
   public class FileFlowDbContext : DbContext
    {
        public FileFlowDbContext(DbContextOptions<FileFlowDbContext> options) : base(options)
        {
        }

        public DbSet<FileItem> FileItems { get; set; }
        public DbSet<Folder> Folders { get; set; }
    }
}