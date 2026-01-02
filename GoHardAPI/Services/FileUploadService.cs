using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace GoHardAPI.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };
        private readonly string _uploadPath = "uploads/profiles";

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Upload a profile photo for a user
        /// </summary>
        /// <param name="userId">User ID for the photo</param>
        /// <param name="file">Photo file to upload</param>
        /// <returns>Public URL path to the uploaded photo</returns>
        public async Task<string> UploadProfilePhotoAsync(int userId, IFormFile file)
        {
            // Validate file
            if (!ValidateFile(file, out string errorMessage))
            {
                throw new Exception(errorMessage);
            }

            // Generate unique filename
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = GenerateFileName(userId, extension);

            // Create full path
            var uploadsFolder = Path.Combine(_environment.WebRootPath, _uploadPath);
            Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return public URL path
            return $"/{_uploadPath}/{fileName}";
        }

        /// <summary>
        /// Delete a profile photo
        /// </summary>
        /// <param name="photoUrl">URL path of the photo to delete</param>
        /// <returns>True if deleted successfully</returns>
        public bool DeleteProfilePhoto(string photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl))
                return false;

            try
            {
                // Convert URL path to physical path
                var fileName = Path.GetFileName(photoUrl);
                var filePath = Path.Combine(_environment.WebRootPath, _uploadPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate uploaded file
        /// </summary>
        private bool ValidateFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                errorMessage = "No file uploaded";
                return false;
            }

            if (file.Length > _maxFileSize)
            {
                errorMessage = $"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024}MB";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                errorMessage = $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generate unique filename for profile photo
        /// </summary>
        private string GenerateFileName(int userId, string extension)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"user_{userId}_{timestamp}{extension}";
        }
    }
}
