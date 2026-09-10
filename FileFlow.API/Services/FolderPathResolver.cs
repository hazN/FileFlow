using FileFlow.API.Data;

namespace FileFlow.API.Services
{
    public class FolderPathResolver
    {
        private readonly FileFlowDbContext _context;

        public FolderPathResolver(FileFlowDbContext context)
        {
            _context = context;
        }

        // Returns something like "Folder/Folder2" for a nested folder,
        public async Task<string> GetRelativePathAsync(int? folderId)
        {
            var segments = new List<string>();
            var currentId = folderId;

            // Loop until we hit the root
            int safety = 0;
            while (currentId != null && safety < 100)
            {
                var folder = await _context.Folders.FindAsync(currentId.Value);
                if (folder == null) break;

                segments.Insert(0, folder.Name);
                currentId = folder.ParentFolderId;
                safety++;
            }

            return Path.Combine(segments.ToArray());
        }
    }
}