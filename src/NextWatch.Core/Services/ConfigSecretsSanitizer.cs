using System.Text.Json;
using System.Text.Json.Nodes;

namespace NextWatch.Core.Services;

public static class ConfigSecretsSanitizer
{
    private static readonly string[] SecretPropertyNames = ["community", "password", "secret"];

    public static string? SanitizeParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj)
                return json;

            RedactSecretProperties(obj);
            return obj.ToJsonString();
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static void RedactSecretProperties(JsonObject obj)
    {
        foreach (var key in obj.Select(static p => p.Key).ToList())
        {
            if (SecretPropertyNames.Any(s => key.Equals(s, StringComparison.OrdinalIgnoreCase)))
                obj.Remove(key);
        }
    }
}
