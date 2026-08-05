using System.Text.Json;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace AccountingHelper.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    public const string ProblemJsonContentType = "application/problem+json";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        // AddExceptionHandler registers this handler as a singleton, so a scoped
        // ICorrelationIdAccessor injected through the constructor would be captured from the
        // root scope — the instance no middleware ever writes to. Resolve it per request instead.
        var correlationId = context.RequestServices
            .GetRequiredService<ICorrelationIdAccessor>()
            .CorrelationId;

        LogException(exception, correlationId, context);

        var problemDetails = CreateProblemResponse(exception, correlationId, context);
        
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        // the content type has to be passed to WriteAsJsonAsync itself: setting
        // Response.ContentType beforehand is overwritten with "application/json" by the call
        await context.Response.WriteAsJsonAsync(
            problemDetails,
            problemDetails.GetType(),
            // the cast picks the JsonSerializerOptions overload over the JsonSerializerContext one;
            // null means "use the options registered in DI"
            (JsonSerializerOptions?)null,
            ProblemJsonContentType,
            ct);

        return true;
        
    }
    
    private void LogException(Exception exception, string correlationId, HttpContext context)
    {
        var logContext = new
        {
            CorrelationId = correlationId,
            RequestPath = context.Request.Path.ToString(),
            RequestMethod = context.Request.Method,
            Exception = exception.GetType().Name
        };

        if (exception is AccountingHelperException)
        {
            _logger.LogWarning(
                exception,
                "Application exception occurred. Context: {@Context}",
                logContext);
        }
        else if (exception is OperationCanceledException)
        {
            _logger.LogInformation("Request was cancelled by the client. CorrelationId: {CorrelationId}", correlationId);
        }
        else
        {
            _logger.LogError(
                exception, 
                "Unhandled exception occurred. Context: {@Context}", 
                logContext);
        }
    }

    private ProblemDetails CreateProblemResponse(Exception exception, string correlationId, HttpContext context)
    {
        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                problemDetails = new ValidationProblemDetails(validationException.Errors)
                {
                    Title = "Validation Failed",
                    Detail = validationException.Message,
                    Status = validationException.StatusCode,
                    Type = validationException.ErrorType
                };
                break;
            
            case AccountingHelperException appEx:
                problemDetails = new ProblemDetails
                {
                    Title = appEx.GetType().Name.Replace("Exception", ""),
                    Detail = appEx.Message,
                    Status = appEx.StatusCode,
                    Type = appEx.ErrorType
                };
                break;
            
            case OperationCanceledException:
                problemDetails = new ProblemDetails
                {
                    Title = "Request Cancelled",
                    Detail = "The request was cancelled by the client.",
                    Status = 499, 
                    Type = ProblemTypes.Cancelled
                };
                break;
            default:
                problemDetails = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = ProblemTypes.InternalServerError,
                    Detail = _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An unexpected error occurred. Please try again later."
                };
                break;
        }
        
        problemDetails.Instance = context.Request.Path;

        problemDetails.Extensions["correlationId"] = correlationId;

        return problemDetails;
    }
}