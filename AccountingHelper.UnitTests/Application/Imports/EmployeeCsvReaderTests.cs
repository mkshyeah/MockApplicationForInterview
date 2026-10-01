using System.Text;
using AccountingHelper.Application.Features.Imports.Parsing;
using FluentAssertions;

namespace AccountingHelper.UnitTests.Application.Imports;

public class EmployeeCsvReaderTests
{
    private const string Header = "FirstName,LastName,Email,Department,Position,SalaryAmount,SalaryType,HireDate\n";

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public void Read_MapsColumnsByHeaderName()
    {
        // ARRANGE
        var content = Utf8(Header + "Aigerim,Nurlanova,a@test.com,Engineering,Developer,1000.50,Monthly,2024-01-15\n");

        // ACT
        var row = EmployeeCsvReader.Read(content, skip: 0).Single().Row;

        // ASSERT
        row.FirstName.Should().Be("Aigerim");
        row.LastName.Should().Be("Nurlanova");
        row.Email.Should().Be("a@test.com");
        row.Department.Should().Be("Engineering");
        row.Position.Should().Be("Developer");
        row.SalaryAmount.Should().Be("1000.50");
        row.SalaryType.Should().Be("Monthly");
        row.HireDate.Should().Be("2024-01-15");
    }

    [Fact]
    public void Read_NumbersRowsByPhysicalLine_AndRecordsFromZero()
    {
        // ARRANGE
        var content = Utf8(Header + "A,B,a@test.com,E,P,1,Monthly,2024-01-15\nC,D,c@test.com,E,P,2,Hourly,2024-02-01\n");

        // ACT
        var rows = EmployeeCsvReader.Read(content, skip: 0).ToList();

        // ASSERT
        // Line 1 is the header, so the first record is line 2: the number the operator sees in an editor.
        rows.Select(r => (r.RecordIndex, r.RowNumber)).Should().Equal((0, 2), (1, 3));
    }

    [Fact]
    public void Read_WithSemicolonDelimiterAndBom_AsExcelSavesIt_ParsesColumns()
    {
        // ARRANGE
        var text = Header.Replace(',', ';') + "A;B;a@test.com;Engineering;Developer;1000;Monthly;15.01.2024\n";
        var content = Encoding.UTF8.GetPreamble().Concat(Utf8(text)).ToArray();

        // ACT
        var row = EmployeeCsvReader.Read(content, skip: 0).Single().Row;

        // ASSERT
        // A BOM left in place would glue itself to "FirstName" and fail header validation.
        row.FirstName.Should().Be("A");
        row.Department.Should().Be("Engineering");
        row.HireDate.Should().Be("15.01.2024");
    }

    [Fact]
    public void Read_MatchesHeadersIgnoringCaseAndSurroundingSpaces()
    {
        // ARRANGE
        var content = Utf8("firstname, LASTNAME ,email,department,position,salaryamount,salarytype,hiredate\n" +
                           "A,B,a@test.com,E,P,1,Monthly,2024-01-15\n");

        // ACT
        var row = EmployeeCsvReader.Read(content, skip: 0).Single().Row;

        // ASSERT
        row.LastName.Should().Be("B");
    }

    [Fact]
    public void Read_WithQuotedMultiLineField_ReturnsOneRecord_NumberedByItsLastLine()
    {
        // ARRANGE
        var content = Utf8(Header + "\"Multi\nLine\",B,a@test.com,E,P,1,Monthly,2024-01-15\nC,D,c@test.com,E,P,2,Hourly,2024-02-01\n");

        // ACT
        var rows = EmployeeCsvReader.Read(content, skip: 0).ToList();

        // ASSERT
        rows.Should().HaveCount(2);
        rows[0].Row.FirstName.Should().Be("Multi\nLine");
        rows.Select(r => (r.RecordIndex, r.RowNumber)).Should().Equal((0, 3), (1, 4));
    }

    [Fact]
    public void Read_SkipsBlankLines_WithoutShiftingLineNumbers()
    {
        // ARRANGE
        var content = Utf8(Header + "A,B,a@test.com,E,P,1,Monthly,2024-01-15\n\nC,D,c@test.com,E,P,2,Hourly,2024-02-01\n");

        // ACT
        var rows = EmployeeCsvReader.Read(content, skip: 0).ToList();

        // ASSERT
        rows.Select(r => (r.RecordIndex, r.RowNumber)).Should().Equal((0, 2), (1, 4));
    }

    [Fact]
    public void Read_WithShortRow_ReturnsEmptyFields_InsteadOfThrowing()
    {
        // ARRANGE
        var content = Utf8(Header + "A,B\nC,D,c@test.com,E,P,2,Hourly,2024-02-01\n");

        // ACT
        var rows = EmployeeCsvReader.Read(content, skip: 0).ToList();

        // ASSERT
        // The short row is left for row validation to report; the file keeps going.
        rows.Should().HaveCount(2);
        rows[0].Row.Email.Should().BeNullOrEmpty();
        rows[1].Row.Email.Should().Be("c@test.com");
    }

    [Fact]
    public void Read_WithSkip_ResumesAtThatRecord_KeepingOriginalNumbers()
    {
        // ARRANGE
        var content = Utf8(Header +
                           "A,B,a@test.com,E,P,1,Monthly,2024-01-15\n" +
                           "C,D,c@test.com,E,P,2,Hourly,2024-02-01\n" +
                           "E,F,e@test.com,E,P,3,Daily,2024-03-01\n");

        // ACT
        var rows = EmployeeCsvReader.Read(content, skip: 2).ToList();

        // ASSERT
        // The checkpoint path: records already handled are read past, and the rest keep the index and
        // line they had on the first delivery, so error rows cannot land on a different row number.
        rows.Should().ContainSingle();
        rows[0].RecordIndex.Should().Be(2);
        rows[0].RowNumber.Should().Be(4);
        rows[0].Row.Email.Should().Be("e@test.com");
    }

    [Fact]
    public void CountRecords_CountsRecordsNotLines()
    {
        // ARRANGE
        var content = Utf8(Header +
                           "\"Multi\nLine\",B,a@test.com,E,P,1,Monthly,2024-01-15\n" +
                           "\n" +
                           "C,D,c@test.com,E,P,2,Hourly,2024-02-01\n");

        // ACT
        var count = EmployeeCsvReader.CountRecords(content);

        // ASSERT
        count.Should().Be(2);
    }

    [Fact]
    public void CountRecords_WithHeaderOnly_ReturnsZero()
    {
        EmployeeCsvReader.CountRecords(Utf8(Header)).Should().Be(0);
    }

    [Fact]
    public void Read_WithMissingColumns_ThrowsInvalidImportFile_NamingThem()
    {
        // ARRANGE
        var content = Utf8("FirstName,LastName,Email\nA,B,a@test.com\n");

        // ACT
        // ToList forces the iterator: Read is lazy and would otherwise not even look at the header.
        var act = () => EmployeeCsvReader.Read(content, skip: 0).ToList();

        // ASSERT
        act.Should().Throw<InvalidImportFileException>()
            .WithMessage("*Department*Position*SalaryAmount*SalaryType*HireDate*");
    }

    [Fact]
    public void CountRecords_WithEmptyFile_ThrowsInvalidImportFile()
    {
        var act = () => EmployeeCsvReader.CountRecords([]);

        act.Should().Throw<InvalidImportFileException>().WithMessage("*empty*");
    }
}
