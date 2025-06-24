namespace Application.Interfaces.Services;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "uploads");
    Task<bool> DeleteFileAsync(string filePath);
    bool IsValidImageFile(string fileName);
    string GetFileUrl(string filePath);
}
