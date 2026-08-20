namespace AccountingHelper.Application.Caching;

public static class CacheKeys
{
    // Префикс InstanceName ("accounting-helper:") добавляет StackExchangeRedisCache сам —
    // здесь его повторять нельзя, иначе получится accounting-helper:accounting-helper:...
    // Суффикс версии — часть контракта: меняется форма ответа, меняется и v1, иначе после
    // деплоя записи старой формы будут десериализоваться в новый тип.
    public const string ReportingDashboard = "reporting:dashboard:v1";
    public const string LookupsDepartments = "lookups:departments:v1";
    public const string LookupsPositions = "lookups:positions:v1";

    // Всё, что кэшируется статическим ключом. Потребитель сегодня — уборка кэша в
    // IntegrationFixture.ResetAsync (без неё значение из одного теста утекает в другой
    // через L1, переживая и Respawn, и очистку Redis); в фазе 4 сюда придёт событийная
    // эвикция. Ограничение: перечислить можно только ключи без параметров. Как только
    // появится ключ вида employee:{id}:..., этот список перестанет покрывать кэш целиком,
    // и уборку придётся переводить на теги (RemoveByTagAsync).
    public static readonly IReadOnlyList<string> All =
    [
        ReportingDashboard,
        LookupsDepartments,
        LookupsPositions
    ];
}
