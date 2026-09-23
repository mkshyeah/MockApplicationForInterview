using AccountingHelper.Application.Features.Imports;
using AccountingHelper.Application.Features.Imports.Commands.CreateImportJob;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;

namespace AccountingHelper.UnitTests.Application.Validators;

public class CreateImportJobCommandValidatorTests
{
    // Tiny on purpose: the boundary is what is under test, not the production number.
    private const long MaxBytes = 16;

    private readonly CreateImportJobCommandValidator _validator =
        new(Options.Create(new ImportSettings { MaxFileSizeBytes = MaxBytes }));

    private static CreateImportJobCommand Command(
        string fileName = "employees.csv",
        string contentType = "text/csv",
        byte[]? content = null) =>
        new(fileName, contentType, content ?? [1, 2, 3]);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Command()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("text/csv")]
    [InlineData("application/csv")]
    [InlineData("text/plain")]
    [InlineData("application/vnd.ms-excel")]
    [InlineData("application/octet-stream")]
    [InlineData("TEXT/CSV")]
    [InlineData("text/csv; charset=utf-8")]
    public void Validate_WithAllowedContentType_Passes(string contentType)
    {
        _validator.TestValidate(Command(contentType: contentType))
            .ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    [InlineData("garbage;;")]
    public void Validate_WithDisallowedContentType_Fails(string contentType)
    {
        // Exactly one error: the cascade must stop an empty value before the allow-list adds a second.
        var result = _validator.TestValidate(Command(contentType: contentType));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateImportJobCommand.ContentType));
    }

    [Theory]
    [InlineData("employees.csv")]
    [InlineData("EMPLOYEES.CSV")]
    public void Validate_WithCsvExtension_Passes(string fileName)
    {
        _validator.TestValidate(Command(fileName: fileName))
            .ShouldNotHaveValidationErrorFor(x => x.FileName);
    }

    [Theory]
    [InlineData("employees.txt")]
    [InlineData("employees.xlsx")]
    [InlineData("employees")]
    [InlineData("employees.csv.exe")]
    public void Validate_WithoutCsvExtension_Fails(string fileName)
    {
        _validator.TestValidate(Command(fileName: fileName))
            .ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_WithEmptyFileName_ReportsOneError()
    {
        var result = _validator.TestValidate(Command(fileName: ""));

        // The cascade stops the extension rule from adding a second error to an empty name.
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateImportJobCommand.FileName));
    }

    [Fact]
    public void Validate_WithFileNameAtColumnLength_Passes()
    {
        var fileName = new string('a', ImportJob.FileNameMaxLength - ".csv".Length) + ".csv";

        _validator.TestValidate(Command(fileName: fileName))
            .ShouldNotHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_WithFileNameOverColumnLength_Fails()
    {
        var fileName = new string('a', ImportJob.FileNameMaxLength - ".csv".Length + 1) + ".csv";

        _validator.TestValidate(Command(fileName: fileName))
            .ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_WithFileExactlyAtLimit_Passes()
    {
        _validator.TestValidate(Command(content: new byte[MaxBytes]))
            .ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WithFileOneByteOverLimit_Fails()
    {
        _validator.TestValidate(Command(content: new byte[MaxBytes + 1]))
            .ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WithEmptyFile_Fails()
    {
        _validator.TestValidate(Command(content: []))
            .ShouldHaveValidationErrorFor(x => x.Content);
    }
}
