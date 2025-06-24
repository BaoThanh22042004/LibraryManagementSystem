using Application.Interfaces.Services;

namespace Web.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "uploads")
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("File stream is empty or null");

        if (!IsValidImageFile(fileName))
            throw new ArgumentException("Invalid file type. Only image files are allowed.");

        if (fileStream.Length > _maxFileSize)
            throw new ArgumentException($"File size exceeds the maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

        try
        {
            // Ensure all folders are under uploads directory
            string uploadFolder;
            if (folder == "uploads" || folder.StartsWith("uploads/"))
            {
                uploadFolder = folder;
            }
            else
            {
                uploadFolder = $"uploads/{folder}";
            }

            // Create upload directory if it doesn't exist
            var uploadPath = Path.Combine(_environment.WebRootPath, uploadFolder);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(fileName).ToLower();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Save file
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            // Return relative path for URL
            return $"/{uploadFolder}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw new InvalidOperationException("An error occurred while uploading the file");
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            // Convert URL path to physical path
            var physicalPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            
            if (File.Exists(physicalPath))
            {
                await Task.Run(() => File.Delete(physicalPath));
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public bool IsValidImageFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLower();
        return _allowedExtensions.Contains(extension);
    }

    public string GetFileUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        // If it's already a full URL, return as is
        if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
            return filePath;

        // If it's a relative path, ensure it starts with /
        return filePath.StartsWith("/") ? filePath : $"/{filePath}";
    }
}

// Extension methods to work with IFormFile
public static class FileUploadExtensions
{
    public static async Task<string> UploadFileAsync(this IFileUploadService service, IFormFile file, string folder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        using var stream = file.OpenReadStream();
        return await service.UploadFileAsync(stream, file.FileName, folder);
    }

    public static bool IsValidImageFile(this IFileUploadService service, IFormFile file)
    {
        return file != null && service.IsValidImageFile(file.FileName);
    }
}
