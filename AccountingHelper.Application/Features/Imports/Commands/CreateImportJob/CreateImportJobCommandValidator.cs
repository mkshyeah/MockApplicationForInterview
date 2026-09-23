using System.Collections.Frozen;
using System.Net.Http.Headers;
using AccountingHelper.Domain.Models;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AccountingHelper.Application.Features.Imports.Commands.CreateImportJob;

public class CreateImportJobCommandValidator : AbstractValidator<CreateImportJobCommand>
{
    // Browsers disagree on what a .csv is: Windows with Excel sends vnd.ms-excel, Safari sends octet-stream.
    // Octet-stream is only safe together with the extension rule on FileName.
    private static readonly FrozenSet<string> AllowedContentTypes = new[]
    {
        "text/csv",
        "application/csv",
        "text/plain",
        "application/vnd.ms-excel",
        "application/octet-stream"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public CreateImportJobCommandValidator(IOptions<ImportSettings> settings)
    {
        var maxBytes = settings.Value.MaxFileSizeBytes;

        RuleFor(x => x.FileName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("File name cannot be empty.")
            .MaximumLength(ImportJob.FileNameMaxLength)
            .WithMessage($"File name must be at most {ImportJob.FileNameMaxLength} characters.")
            // The allow-list has to keep octet-stream for Safari, so the extension carries the operator's intent.
            .Must(n => Path.GetExtension(n).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .csv files can be imported.");

        RuleFor(x => x.ContentType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Content type cannot be empty.")
            .Must(IsAllowedContentType)
            .WithMessage(x => $"Content type '{x.ContentType}' is not accepted for a CSV import.");

        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Content cannot be empty.")
            .Must(c => c.LongLength <= maxBytes)
            .WithMessage($"File must be at most {maxBytes} bytes.");
    }

    private static bool IsAllowedContentType(string contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var parsed)
        && parsed.MediaType is not null
        && AllowedContentTypes.Contains(parsed.MediaType);
}
