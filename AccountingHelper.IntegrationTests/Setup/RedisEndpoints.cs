namespace AccountingHelper.IntegrationTests.Setup;

public static class RedisEndpoints
{
    // Короткие таймауты: тесты не должны платить пятью секундами дефолта за каждый промах.
    public static string Live(string endpoint) =>
        $"{endpoint},abortConnect=false,connectTimeout=1000,syncTimeout=1000,connectRetry=1";

    // 127.0.0.1:1 вместо 6399: свободный порт — договорённость, а не гарантия,
    // и однажды на агенте его кто-нибудь займёт. Первый порт не слушает никто.
    public const string Dead =
        "127.0.0.1:1,abortConnect=false,connectTimeout=200,syncTimeout=200,connectRetry=1";
}