using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Imports.Commands.RunImport;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ImportErrorReportTests : IntegrationTestBase
{
    public ImportErrorReportTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string Header = "FirstName,LastName,Email,Department,Position,SalaryAmount,SalaryType,HireDate\n";

    private static string BadRow(int i) =>
        $"First{i},Last{i},user{i}@import.test,Marketing,Developer,1000,Monthly,2024-01-15\n";

    private static string GoodRow(int i) =>
        $"First{i},Last{i},user{i}@import.test,Engineering,Developer,1000,Monthly,2024-01-15\n";

    private static string Errors(Guid jobId) => $"v1/imports/{jobId}/errors";

    // Uploads through the API and runs the import in-process: what is under test is the report, not delivery.
    private async Task<Guid> ImportAsync(string csv)
    {
        await SeedDepartmentAsync("Engineering");
        await SeedPositionAsync("Developer", EmployeeGrade.Junior);

        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");

        var resp = await Client.PostAsync("v1/imports", new MultipartFormDataContent { { file, "file", "employees.csv" } });
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var jobId = (await resp.Content.ReadFromJsonAsync<ImportJobResponse>(Json))!.Id;

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunImportCommand(jobId));

        return jobId;
    }

    private async Task<PagedResponse<ImportErrorResponse>> GetPageAsync(string url)
    {
        var resp = await Client.GetAsync(url);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await resp.Content.ReadFromJsonAsync<PagedResponse<ImportErrorResponse>>(Json))!;
    }

    [Fact]
    public async Task GetImportErrors_WithoutPaging_ReturnsFirstPageInRowOrder()
    {
        // ARRANGE
        var jobId = await ImportAsync(Header + string.Concat(Enumerable.Range(0, 25).Select(BadRow)));

        // ACT
        var page = await GetPageAsync(Errors(jobId));

        // ASSERT
        // PaginationRequest's defaults, and the order of the unique (import_job_id, row_number) index.
        page.Total.Should().Be(25);
        page.Offset.Should().Be(0);
        page.Limit.Should().Be(10);
        page.Returned.Should().Be(10);
        page.Items.Select(e => e.RowNumber).Should().Equal(Enumerable.Range(2, 10));
        page.Items.Should().AllSatisfy(e => e.ErrorMessage.Should().Be("Unknown department 'Marketing'."));
    }

    [Fact]
    public async Task GetImportErrors_LastPage_ReturnsWhatIsLeft()
    {
        // ARRANGE
        var jobId = await ImportAsync(Header + string.Concat(Enumerable.Range(0, 25).Select(BadRow)));

        // ACT
        var page = await GetPageAsync($"{Errors(jobId)}?offset=20&limit=10");

        // ASSERT
        page.Total.Should().Be(25);
        page.Returned.Should().Be(5);
        page.Items.Select(e => e.RowNumber).Should().Equal(22, 23, 24, 25, 26);
    }

    [Fact]
    public async Task GetImportErrors_WalkingEveryPage_SeesEachFailedRowExactlyOnce()
    {
        // ARRANGE
        // Bad rows interleaved with good ones, so the report's line numbers have gaps, and a page size that
        // does not divide the total: what a stable order has to survive.
        var rows = Enumerable.Range(0, 30).Select(i => i % 3 == 0 ? GoodRow(i) : BadRow(i));
        var jobId = await ImportAsync(Header + string.Concat(rows));

        // ACT
        var seen = new List<int>();
        PagedResponse<ImportErrorResponse> page;
        var offset = 0;

        do
        {
            page = await GetPageAsync($"{Errors(jobId)}?offset={offset}&limit=7");
            seen.AddRange(page.Items.Select(e => e.RowNumber));
            offset += page.Limit;
        } while (offset < page.Total);

        // ASSERT
        // One error row per failed file row: the report's length is FailedCount.
        var expected = Enumerable.Range(0, 30).Where(i => i % 3 != 0).Select(i => i + 2);
        seen.Should().Equal(expected);
        page.Total.Should().Be(20);
    }

    [Fact]
    public async Task GetImportErrors_ForJobWithoutErrors_ReturnsEmptyPage()
    {
        // ARRANGE
        var jobId = await ImportAsync(Header + GoodRow(1) + GoodRow(2));

        // ACT
        var page = await GetPageAsync(Errors(jobId));

        // ASSERT
        // "No errors" is a real answer about a real job — a 200 with an empty page, never a 404.
        page.Total.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetImportErrors_ForFailedJob_ShowsTheReasonOnTheHeaderLine()
    {
        // ARRANGE
        var jobId = await ImportAsync("FirstName,LastName,Email\nA,B,a@import.test\n");

        // ACT
        var page = await GetPageAsync(Errors(jobId));

        // ASSERT
        // A Failed job explains itself through the same report as row errors, so a client needs one read path.
        var error = page.Items.Should().ContainSingle().Subject;
        error.RowNumber.Should().Be(1);
        error.ErrorMessage.Should().StartWith("Missing required columns");
    }

    [Theory]
    [InlineData("limit=0", "limit")]
    [InlineData("limit=101", "limit")]
    [InlineData("offset=-1", "offset")]
    public async Task GetImportErrors_WithPagingOutOfRange_Returns400(string queryString, string field)
    {
        // ARRANGE
        var jobId = await ImportAsync(Header + BadRow(1));

        // ACT
        var resp = await Client.GetAsync($"{Errors(jobId)}?{queryString}");

        // ASSERT
        // The shared PagedQueryValidator, so "how big a page" means the same thing on every endpoint.
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Errors.Should().ContainKey(field);
    }

    [Fact]
    public async Task GetImportErrors_WhenJobDoesNotExist_Returns404()
    {
        // ACT
        var resp = await Client.GetAsync(Errors(Guid.NewGuid()));

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Extensions.Should().ContainKey("correlationId");
    }
}
