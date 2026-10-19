namespace AccountingHelper.Domain.Exceptions;

// Names that UniqueConstraintViolationException.ConstraintName can carry and handlers branch on.
// The EF configurations take their index names from here, so renaming an index cannot silently
// break a handler that still compares against the old string.
public static class DatabaseConstraints
{
    public const string EmployeeEmail = "ix_employees_email";
    public const string ImportErrorRow = "ix_import_errors_import_job_id_row_number";
}