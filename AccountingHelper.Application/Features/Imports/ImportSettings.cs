namespace AccountingHelper.Application.Features.Imports;

public class ImportSettings
{
    public const string SectionName = "Import";

    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
    public int BatchSize { get; init; } = 500;
}
