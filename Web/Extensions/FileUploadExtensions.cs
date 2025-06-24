using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Web.Extensions;

/// <summary>
/// Extension methods for handling file upload operations in controllers
/// </summary>
public static class FileUploadControllerExtensions
{    /// <summary>
    /// Handles file upload for cover images with consistent error handling and validation
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="fileUploadService">The file upload service</param>
    /// <param name="file">The uploaded file</param>
    /// <param name="folder">The subfolder under uploads to store the file in (e.g., "categories", "books")</param>
    /// <param name="currentImageUrl">Current image URL to delete if replacing</param>
    /// <returns>Tuple containing success flag, uploaded URL (if successful), and error message (if failed)</returns>
    public static async Task<(bool Success, string? UploadedUrl, string? ErrorMessage)> HandleCoverImageUploadAsync(
        this Controller controller,
        IFileUploadService fileUploadService,
        IFormFile? file,
        string folder,
        string? currentImageUrl = null)
    {
        if (file == null || file.Length == 0)
        {
            return (true, null, null); // No file uploaded is OK
        }        // Validate file type
        if (!fileUploadService.IsValidImageFile(file.FileName))
        {
            return (false, null, "Please select a valid image file (jpg, jpeg, png, gif, bmp, webp).");
        }        try
        {
            // Delete old image if replacing and it's not a URL
            if (!string.IsNullOrEmpty(currentImageUrl) && !currentImageUrl.StartsWith("http"))
            {
                await fileUploadService.DeleteFileAsync(currentImageUrl);
            }
            
            // Upload new file (FileUploadService will ensure it goes under uploads/)
            using var stream = file.OpenReadStream();
            var uploadedUrl = await fileUploadService.UploadFileAsync(stream, file.FileName, folder);
            return (true, uploadedUrl, null);
        }
        catch (ArgumentException ex)
        {
            return (false, null, ex.Message);
        }
        catch (Exception)
        {
            return (false, null, "Error uploading the image. Please try again.");
        }
    }

    /// <summary>
    /// Safely removes a cover image file
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="fileUploadService">The file upload service</param>
    /// <param name="imageUrl">The image URL to remove</param>
    /// <returns>True if removed successfully or no action needed, false if an error occurred</returns>
    public static async Task<bool> SafelyRemoveCoverImageAsync(
        this Controller controller,
        IFileUploadService fileUploadService,
        string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl) || imageUrl.StartsWith("http"))
        {
            return true; // Nothing to delete or it's an external URL
        }

        try
        {
            return await fileUploadService.DeleteFileAsync(imageUrl);
        }
        catch (Exception)
        {
            // Log the error but don't fail the operation
            return false;
        }
    }
}

/// <summary>
/// Extension methods for working with entities that have cover images
/// </summary>
public static class CoverImageExtensions
{
    /// <summary>
    /// Gets the absolute URL for a cover image, handling both relative paths and external URLs
    /// </summary>
    /// <param name="fileUploadService">The file upload service</param>
    /// <param name="coverImageUrl">The cover image URL</param>
    /// <returns>The absolute URL or empty string if no image</returns>
    public static string GetCoverImageUrl(this IFileUploadService fileUploadService, string? coverImageUrl)
    {
        return fileUploadService.GetFileUrl(coverImageUrl ?? string.Empty);
    }

    /// <summary>
    /// Determines if a cover image URL is a local file (as opposed to an external URL)
    /// </summary>
    /// <param name="coverImageUrl">The cover image URL</param>
    /// <returns>True if it's a local file, false if it's an external URL or null/empty</returns>
    public static bool IsLocalFile(this string? coverImageUrl)
    {
        return !string.IsNullOrEmpty(coverImageUrl) &&
               !coverImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
               !coverImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
