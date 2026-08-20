using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.Caching;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class DashboardCacheTests : IntegrationTestBase
{
    public DashboardCacheTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string Route = "v1/reporting/dashboard";

    private async Task<DashboardResponse> GetDashboardAsync()
    {
        var resp = await Client.GetAsync(Route);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await resp.Content.ReadFromJsonAsync<DashboardResponse>(Json))!;
    }

    private Task RemoveDashboardKeyAsync() =>
        Services.GetRequiredService<HybridCache>()
            .RemoveAsync(CacheKeys.ReportingDashboard)
            .AsTask();

    // Обе половины в одном тесте намеренно. Первая половина сама по себе зелёная
    // и при наглухо сломанном кэше — она доказывает только то, что ответ не изменился.
    // Смысл появляется вместе со второй: сняли ключ — значение обязано обновиться.
    [Fact]
    public async Task GetDashboard_WithinTtl_ServesStaleValue_UntilKeyIsRemoved()
    {
        // ARRANGE
        var itId = await SeedDepartmentAsync("IT");
        var juniorId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        var employee = await CreateEmployeeAsync(itId, juniorId, salary: 1000m);

        var before = await GetDashboardAsync();
        before.TotalMonthlyPayroll.Should().Be(1_000.00m);

        var changeResp = await Client.PutAsJsonAsync($"v1/employees/{employee.Id}/salaries", new
        {
            amount = 10_000m, salaryType = "Monthly"
        });
        changeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // ACT — первая половина: в пределах TTL дашборд имеет право отставать
        var stale = await GetDashboardAsync();

        // ASSERT
        stale.TotalMonthlyPayroll.Should().Be(1_000.00m);

        // ACT — вторая половина: снимаем ключ, значение обязано обновиться
        await RemoveDashboardKeyAsync();
        var fresh = await GetDashboardAsync();

        // ASSERT
        fresh.TotalMonthlyPayroll.Should().Be(10_000.00m);
    }

    // Доказательство «второй запрос не ходит в БД» без единого мока: данные меняются
    // напрямую, мимо API, и старое значение в ответе означает ровно одно — в базу не ходили.
    // Тот же приём, что в тесте про уволенного с незакрытой зарплатой.
    [Fact]
    public async Task GetDashboard_SecondCall_DoesNotReadTheDatabase()
    {
        // ARRANGE
        var itId = await SeedDepartmentAsync("IT");
        var juniorId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        await CreateEmployeeAsync(itId, juniorId, salary: 1000m);

        var warmed = await GetDashboardAsync();
        warmed.TotalMonthlyPayroll.Should().Be(1_000.00m);

        await WithDbContextAsync(async db =>
        {
            var salary = await db.Salaries.SingleAsync(s => s.EndDate == null);
            salary.Amount = 7_777m;
            await db.SaveChangesAsync();
        });

        // ACT
        var second = await GetDashboardAsync();

        // ASSERT
        second.TotalMonthlyPayroll.Should().Be(1_000.00m);

        // и контроль: после снятия ключа новое значение из базы всё-таки видно —
        // иначе тест был бы зелёным и на эндпоинте, который вообще перестал читать БД
        await RemoveDashboardKeyAsync();
        (await GetDashboardAsync()).TotalMonthlyPayroll.Should().Be(7_777.00m);
    }

    // Единственная прямая проверка L2. Запись у HybridCache fire-and-forget — ключ появляется
    // после ответа, поэтому здесь ожидание с таймаутом, а не мгновенная проверка.
    // IDistributedCache сам приклеивает InstanceName, так что попадание означает, что запись
    // легла именно под accounting-helper:reporting:dashboard:v1.
    [Fact]
    public async Task GetDashboard_WritesEntryToRedis_UnderConfiguredInstancePrefix()
    {
        // ARRANGE
        var l2 = Services.GetRequiredService<IDistributedCache>();

        // ACT
        await GetDashboardAsync();

        // ASSERT
        var stopwatch = Stopwatch.StartNew();
        byte[]? payload = null;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            payload = await l2.GetAsync(CacheKeys.ReportingDashboard);
            if (payload is not null) break;

            await Task.Delay(50);
        }

        payload.Should().NotBeNull("запись в L2 асинхронная, но она обязана появиться");
    }

    // Симметрия сериализации: HybridCache кладёт DTO своим System.Text.Json, без
    // StrictEnumConverterFactory, поэтому Grade уезжает в Redis числом. Клиенту в обоих
    // случаях отвечает MVC, то есть строкой — и ответы обязаны совпадать байт в байт.
    [Fact]
    public async Task GetDashboard_ResponseFromCache_IsByteIdenticalToResponseFromDatabase()
    {
        // ARRANGE
        var itId = await SeedDepartmentAsync("IT");
        var seniorId = await SeedPositionAsync("Senior Developer", EmployeeGrade.Senior);
        await CreateEmployeeAsync(itId, seniorId, salary: 5000m);

        // ACT
        var fromDatabase = await (await Client.GetAsync(Route)).Content.ReadAsStringAsync();
        var fromCache = await (await Client.GetAsync(Route)).Content.ReadAsStringAsync();

        // ASSERT
        fromCache.Should().Be(fromDatabase);
        fromCache.Should().Contain("\"grade\":\"Senior\"");
    }
}