using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Services
{
    public class ImageService : IImageService
    {
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

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", folder);
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/images/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading image: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return true;

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImagePath}", imagePath);
                return false;
            }
        }

        public string GetImageUrl(string fileName, string folder)
        {
            return $"/images/{folder}/{fileName}";
        }
    }
}