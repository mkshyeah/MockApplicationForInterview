using System.Text.Json;

namespace AccountingHelper.Application.Exceptions;

/// <summary>
/// Converts a CLR property path into the camelCase name the client actually sent,
/// so that validation error keys match the request body field for field.
/// </summary>
public static class JsonPropertyName
{
    /// <summary>
    /// Each segment is converted on its own: JsonNamingPolicy.CamelCase.ConvertName
    /// only lowers the leading letter, so "Salaries[0].Amount" would come back as
    /// "salaries[0].Amount" if the path were converted in one piece.
    /// </summary>
    public static string FromPropertyPath(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
