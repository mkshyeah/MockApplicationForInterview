using FluentValidation.Results;

namespace AccountingHelper.Application.Exceptions;

public class AccountingHelperException : Exception
{
    public int StatusCode { get; }
    public string ErrorType { get; }

    protected AccountingHelperException(string message, int statusCode, string errorType)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
    }
}

public class NotFoundException : AccountingHelperException
{
    public NotFoundException(string resource, object id) 
        : base($"{resource} with ID '{id}' was not found", 404, "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4") { }
    
    public NotFoundException(string message)
        : base(message, 404, "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4") { }
}

public class ValidationException : AccountingHelperException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string message)
        : base("One or more validation errors occurred.", 400,
            "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1")
    {
        Errors = new Dictionary<string, string[]> { { field, new[] { message } } };
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("One or more validation errors occurred.", 400,
            "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}

public class ConflictException : AccountingHelperException
{
    public ConflictException(string message) 
        : base(message, 409, "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8") { }
}
public class BusinessRuleException : AccountingHelperException
{
    public BusinessRuleException(string message) 
        : base(message, 422, "https://datatracker.ietf.org/doc/html/rfc2518#section-10.3") { }
}


