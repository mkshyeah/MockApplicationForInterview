using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace AccountingHelper.Application.Features.Imports.Parsing;

// RecordIndex counts records and is what the checkpoint skips by; RowNumber is the physical line the
// operator sees in an editor (for a multi-line record, its last line).
public sealed record ParsedCsvRow(int RecordIndex, int RowNumber, EmployeeCsvRow Row);

public static class EmployeeCsvReader
{
    private static CsvConfiguration CreateConfiguration() => new(CultureInfo.InvariantCulture)
    {
        // Excel in ru/kk locales saves CSV with ';', and those files are the common case here.
        DetectDelimiter = true,
        PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
        TrimOptions = TrimOptions.Trim,
        // Both would throw mid-file by default. A short row becomes empty fields and a stray quote is
        // taken literally; row validation reports either as an error of that row.  
        MissingFieldFound = null,
        BadDataFound = null,
    };
    
    // The first pass: counts records, not newlines, so a quoted multi-line field is one record.
    public static int CountRecords(byte[] content)
    {
        using var csv = Open(content);
        ReadHeader(csv);

        var count = 0;
        while (csv.Read())
            count++;
        
        return count;
    }

    public static IEnumerable<ParsedCsvRow> Read(byte[] content, int skip)
    {
        using var csv = Open(content);
        ReadHeader(csv);

        var index = 0;
        while (csv.Read())
        {
            if (index >= skip)
                yield return new ParsedCsvRow(index, csv.Parser.RawRow, csv.GetRecord<EmployeeCsvRow>());

            index++;
        }
    }

    private static CsvReader Open(byte[] content)
    {
        var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var csv = new CsvReader(reader, CreateConfiguration());
        csv.Context.RegisterClassMap<EmployeeCsvRowMap>();
        return csv;
    }

    private static void ReadHeader(CsvReader csv)
    {
        if(!csv.Read())
            throw new InvalidImportFileException("The file is empty.");
        
        csv.ReadHeader();

        try
        {
            csv.ValidateHeader<EmployeeCsvRow>();
        }
        catch (HeaderValidationException ex)
        {
            var missing = ex.InvalidHeaders.Select(h => string.Join("/", h.Names));
            throw new InvalidImportFileException($"Missing required columns: {string.Join(", ", missing)}.");
        }
    }
}