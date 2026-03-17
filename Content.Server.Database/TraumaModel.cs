using System.Collections.Generic;
using System.Text.Json;

namespace Content.Server.Database;

public static class TraumaModel
{
    /// <summary>
    /// Trauma - i hate this language
    /// </summary>
    internal static List<string> DeserializeStrings(string s)
    {
        if (string.IsNullOrEmpty(s))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(s) ?? new();
        }
        catch
        {
            return []; // i dont care if it failed to load dont kill the server
        }
    }
}
