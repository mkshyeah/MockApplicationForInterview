using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Imports;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ImportJobCreateTests : IntegrationTestBase
{
    public ImportJobCreateTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string Route = "v1/imports";

    private static readonly byte[] Csv = Encoding.UTF8.GetBytes("FirstName,LastName,Email\nAigerim,Nurlanova,a@test.com\n");

    private long MaxFileSizeBytes =>
        Services.GetRequiredService<IOptions<ImportSettings>>().Value.MaxFileSizeBytes;

    private static MultipartFormDataContent Form(byte[] content, string fileName, string contentType)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private Task AssertNothingStoredAsync() =>
        WithDbContextAsync(async db =>
        {
            (await db.ImportJobs.CountAsync()).Should().Be(0);
            (await db.ImportFiles.CountAsync()).Should().Be(0);
        });

    [Fact]
    public async Task CreateImportJob_Returns202_WithLocation_AndStoresPendingJobWithFile()
    {
        // ACT
        var resp = await Client.PostAsync(Route, Form(Csv, "employees.csv", "text/csv; charset=utf-8"));

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await resp.Content.ReadFromJsonAsync<ImportJobResponse>(Json);
        body!.FileName.Should().Be("employees.csv");
        body.Status.Should().Be(ImportJobStatus.Pending);

        resp.Headers.Location.Should().NotBeNull();
        var followed = await Client.GetAsync(resp.Headers.Location);
        followed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await followed.Content.ReadFromJsonAsync<ImportJobResponse>(Json))!.Id.Should().Be(body.Id);

        await WithDbContextAsync(async db =>
        {
            var job = await db.ImportJobs.SingleAsync();
            job.Id.Should().Be(body.Id);
            job.Status.Should().Be(ImportJobStatus.Pending);
            // Zero until the consumer counts: the API does not parse the file.
            job.TotalRows.Should().Be(0);
            job.StartedAt.Should().BeNull();

            var file = await db.ImportFiles.SingleAsync();
            file.ImportJobId.Should().Be(body.Id);
            file.Content.Should().Equal(Csv);
            // The parsed media type, not the raw header: parameters are dropped before they can outgrow the column.
            file.ContentType.Should().Be("text/csv");
        });
    }

    [Fact]
    public async Task CreateImportJob_WithClientPathInFileName_StoresBareName()
    {
        // ACT
        var resp = await Client.PostAsync(Route, Form(Csv, @"C:\Users\op\employees.csv", "text/csv"));

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await resp.Content.ReadFromJsonAsync<ImportJobResponse>(Json))!.FileName.Should().Be("employees.csv");
    }

    [Fact]
    public async Task CreateImportJob_WithoutCsvExtension_Returns400()
    {
        // ACT
        var resp = await Client.PostAsync(Route, Form(Csv, "employees.xlsx", "application/octet-stream"));

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Errors.Should().ContainKey("fileName");
        problem.Extensions.Should().ContainKey("correlationId");

        await AssertNothingStoredAsync();
    }

    [Fact]
    public async Task CreateImportJob_WithDisallowedContentType_Returns400()
    {
        // ACT
        var resp = await Client.PostAsync(Route, Form(Csv, "employees.csv", "application/pdf"));

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Errors.Should().ContainKey("contentType");

        await AssertNothingStoredAsync();
    }

    [Fact]
    public async Task CreateImportJob_WhenFileIsOneByteOverLimit_Returns400FromValidator()
    {
        // ARRANGE
        // Inside the multipart headroom, so the body is read and the validator gets to answer.
        var content = new byte[MaxFileSizeBytes + 1];

        // ACT
        var resp = await Client.PostAsync(Route, Form(content, "employees.csv", "text/csv"));

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Errors.Should().ContainKey("content");

        await AssertNothingStoredAsync();
    }

    [Fact]
    public async Task CreateImportJob_WhenBodyExceedsFormLimit_Returns400BeforeValidator()
    {
        // ARRANGE
        // Past the multipart headroom: the form read itself is cut off before the validator runs.
        var content = new byte[MaxFileSizeBytes + 128 * 1024];

        // ACT
        var resp = await Client.PostAsync(Route, Form(content, "employees.csv", "text/csv"));

        // ASSERT
        // Checked, not assumed: a cut-off form read comes back as 400, not as a 500 through GlobalExceptionHandler.
        // Kestrel's bare 413 only applies above MaxRequestBodySize, which ValidateOnStart keeps out of reach.
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await AssertNothingStoredAsync();
    }

    [Fact]
    public async Task CreateImportJob_WithoutFile_Returns400()
    {
        // ARRANGE
        var form = new MultipartFormDataContent { { new StringContent("x"), "notAFile" } };

        // ACT
        var resp = await Client.PostAsync(Route, form);

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await AssertNothingStoredAsync();
    }

    [Fact]
    public async Task GetImportJob_WhenJobDoesNotExist_Returns404()
    {
        // ACT
        var resp = await Client.GetAsync($"{Route}/{Guid.NewGuid()}");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Extensions.Should().ContainKey("correlationId");
    }
}
