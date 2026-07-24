using Microsoft.AspNetCore.Http;

namespace AshtavinayakAPP.Services.DocumentStorage
{
    public class DocumentStorageService : IDocumentStorageService
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        // Extension -> the one content-type it's allowed to be declared as (not just "some
        // allowed type") — checking each allowlist independently would let a .pdf declared as
        // image/png slip through as long as some other extension separately allows image/png.
        private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        private readonly string _rootPath;
        private readonly ILogger<DocumentStorageService> _logger;

        public DocumentStorageService(IConfiguration configuration, ILogger<DocumentStorageService> logger)
        {
            _logger = logger;

            _rootPath = configuration["DocumentStorage:RootPath"]
                ?? throw new InvalidOperationException(
                    "Document storage root path 'DocumentStorage:RootPath' is not configured. " +
                    "In Development, set it in appsettings.Development.json. " +
                    "In Production, set the environment variable 'DocumentStorage__RootPath'.");
        }

        public async Task<(bool Success, string Message, string? RelativePath)> SaveAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
                return (false, "No file was provided.", null);

            if (file.Length > MaxFileSizeBytes)
                return (false, "File exceeds the 5MB size limit.", null);

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !ExtensionToContentType.TryGetValue(extension, out var expectedContentType))
                return (false, "Only PDF, JPG, and PNG files are allowed.", null);

            if (string.IsNullOrEmpty(file.ContentType) || !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
                return (false, "Only PDF, JPG, and PNG files are allowed.", null);

            if (!await MatchesSignatureAsync(file, extension))
                return (false, "The file's content does not match its extension.", null);

            var storageDirectory = Path.Combine(_rootPath, subfolder);
            Directory.CreateDirectory(storageDirectory);

            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var absolutePath = Path.Combine(storageDirectory, storedFileName);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine(subfolder, storedFileName);
            _logger.LogInformation("[DocumentStorage] Saved document to {RelativePath}", relativePath);
            return (true, "File saved.", relativePath);
        }

        public Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(string relativePath)
        {
            var absolutePath = Path.Combine(_rootPath, relativePath);

            // Guard against the resolved path escaping the configured root (path traversal).
            var fullRoot = Path.GetFullPath(_rootPath);
            var fullTarget = Path.GetFullPath(absolutePath);
            if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullTarget))
                return Task.FromResult<(Stream, string, string)?>(null);

            var extension = Path.GetExtension(fullTarget).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            Stream stream = new FileStream(fullTarget, FileMode.Open, FileAccess.Read);
            return Task.FromResult<(Stream, string, string)?>((stream, contentType, Path.GetFileName(fullTarget)));
        }

        // Verifies the file's actual bytes match its claimed extension — ContentType/extension
        // alone are client-supplied and trivially spoofable, and these are compliance documents.
        private static async Task<bool> MatchesSignatureAsync(IFormFile file, string extension)
        {
            var header = new byte[8];
            using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
                if (read < 4) return false;
            }

            return extension.ToLowerInvariant() switch
            {
                ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46, // %PDF
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                _ => false
            };
        }
    }
}
