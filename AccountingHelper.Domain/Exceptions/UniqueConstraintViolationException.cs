namespace AccountingHelper.Domain.Exceptions;

// Raised by the persistence layer when the database rejects a write on a unique index.
// It lives in Domain because Infrastructure throws it and Application catches it, and those two
// layers share nothing else — Application's HTTP-shaped exceptions are not visible from here, and
// Npgsql's PostgresException must not become visible there.
public sealed class UniqueConstraintViolationException : Exception
{
    public string? ConstraintName { get; }

    public UniqueConstraintViolationException(string? constraintName, Exception innerException)
        : base($"A unique constraint was violated: {constraintName ?? "unknown"}.", innerException)
    {
        ConstraintName = constraintName;
    }
}
