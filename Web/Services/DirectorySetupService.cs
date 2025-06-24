using Application.Interfaces.Services;

namespace Web.Services;

/// <summary>
/// Service to ensure required directories exist at startup
/// </summary>
public class DirectorySetupService : IHostedService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DirectorySetupService> _logger;

    public DirectorySetupService(IWebHostEnvironment environment, ILogger<DirectorySetupService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {        try
        {
            // Ensure upload directories exist - all files go under uploads folder
            var directories = new[]
            {
                "uploads",
                "uploads/categories", 
                "uploads/books"
            };

            foreach (var directory in directories)
            {
                var path = Path.Combine(_environment.WebRootPath, directory);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _logger.LogInformation("Created upload directory: {Directory}", path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating upload directories");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
