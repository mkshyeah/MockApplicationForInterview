using AccountingHelper.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is AccountingHelperException appException)
        {
            _logger.LogWarning(exception, "Application exception: {Message}", exception.Message);
            
            context.Response.StatusCode = appException.StatusCode;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = appException.StatusCode,
                Detail = appException.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            }, ct);
            
            return true;
        }
        
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Internal Server Error",
            Type = "https://tools.ietf.org/html/rfc7807"
        }, ct);
        
        return true;
    }
}