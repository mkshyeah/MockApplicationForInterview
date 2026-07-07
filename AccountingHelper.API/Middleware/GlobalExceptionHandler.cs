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
        if (exception is ValidationException validationException)
        {
            _logger.LogWarning(exception, "Validation exception: {Message}", validationException.Message);
            
            var errorsDictionary = validationException.Failures?
                                       .GroupBy(failure => failure.PropertyName)
                                       .ToDictionary(
                                           group => group.Key,
                                           group => group.Select(f => f.ErrorMessage).ToArray()) 
                                   ?? new Dictionary<string, string[]>();
            
            context.Response.StatusCode = validationException.StatusCode;

            await context.Response.WriteAsJsonAsync(new ValidationProblemDetails
            {
                Status = validationException.StatusCode,
                Errors = errorsDictionary,
                Type = "https://tools.ietf.org/html/rfc7807"
            }, ct);
            
            return true;
        }
        
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