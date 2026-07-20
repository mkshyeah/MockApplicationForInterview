using System.Diagnostics.Eventing.Reader;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace AccountingHelper.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly IHostEnvironment _environment;
    
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        ICorrelationIdAccessor correlationIdAccessor,
        IHostEnvironment environment)
    {
        _logger = logger;
        _correlationIdAccessor = correlationIdAccessor;
        _environment = environment;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;

        LogException(exception, correlationId, context);

        var problemDetails = CreateProblemresponse(exception, correlationId, context);
        
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails, ct);

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

    private ProblemDetails CreateProblemresponse(Exception exception, string correlationId, HttpContext context)
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
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5"
                };
                break;
            default:
                problemDetails = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
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