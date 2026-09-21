namespace FileFlow.API.Services;

// Verifies files by their actual binary signature  not just their filename extension
public static class FileSignatureValidator
{
    private static readonly Dictionary<string, List<byte[]>> _signatures = new()
    {
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        { ".zip", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
    };

    public static bool IsValid(string extension, Stream fileStream)
    {
        if (!_signatures.TryGetValue(extension, out var validSignatures))
        {
            return true;
        }

        using var reader = new BinaryReader(fileStream, System.Text.Encoding.Default, leaveOpen: true);
        byte[] headerBytes = reader.ReadBytes(validSignatures.Max(s => s.Length));
        fileStream.Position = 0; // reset so the file can still be saved afterward

        return validSignatures.Any(signature =>
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }
}