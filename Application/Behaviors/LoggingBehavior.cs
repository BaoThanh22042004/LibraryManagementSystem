using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var result = await next();
            _logger.LogInformation("Handled {RequestName}", requestName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestName}", requestName);
            throw;
        }
    }
}