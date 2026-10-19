namespace AccountingHelper.Application.Features.Imports.Parsing;

// The file cannot be read as a whole: empty, or missing required columns. The run handler turns it
// into a Failed job; a bad row never throws this.
public sealed class InvalidImportFileException(string message) : Exception(message);