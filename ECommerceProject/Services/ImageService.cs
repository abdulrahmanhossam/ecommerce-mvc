using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Services
{
    public class ImageService : IImageService
    {
        private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"];
        private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp"];
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            if (file.Length > MaxFileSize)
            {
                throw new InvalidOperationException($"File size exceeds {MaxFileSize / 1024 / 1024}MB limit.");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                throw new InvalidOperationException($"File type '{ext}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");
            }

            if (string.IsNullOrEmpty(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"Content type '{file.ContentType}' is not allowed.");
            }

            var sanitizedFolder = SanitizeFolderName(folder);
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", sanitizedFolder);

            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/{sanitizedFolder}/{fileName}";
        }

        public Task<bool> DeleteImageAsync(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return Task.FromResult(true);

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                var fullPathResolved = Path.GetFullPath(fullPath);
                var wwwRootResolved = Path.GetFullPath(_environment.WebRootPath);

                if (!fullPathResolved.StartsWith(wwwRootResolved, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Path traversal attempt blocked: {ImagePath}", imagePath);
                    return Task.FromResult(false);
                }

                if (File.Exists(fullPathResolved))
                {
                    File.Delete(fullPathResolved);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImagePath}", imagePath);
                return Task.FromResult(false);
            }
        }

        private static string SanitizeFolderName(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return "general";

            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(folder.Where(ch => !invalid.Contains(ch)).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "general" : sanitized;
        }
    }
}