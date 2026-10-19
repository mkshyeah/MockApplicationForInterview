namespace AccountingHelper.Domain.Services;

public static class EmailNormalizer
{
    // RFC lets the local part be case-sensitive; no mail provider treats it that way, and an
    // employer holding Ivan@ and ivan@ as two people is a bug, not a feature. Lowercasing the
    // whole address is therefore deliberate. This function is the definition of "the same email": 
    // the unique index enforces it, and every writer must agree with it or the index will reject
    // rows the writer thought were distinct.
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}