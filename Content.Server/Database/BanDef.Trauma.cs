using System.Net.Http;
using System.Text.Json;

namespace Content.Server.Database;

/// <summary>
/// Trauma - helper for looking up admin name from the guid
/// </summary>
public sealed partial class BanDef
{
    private static string GetUsername(string? userId)
    {
        if (userId == null)
            return "Unknown";

        using (var client = new HttpClient())
        {
            string apiUrl = "https://auth.spacestation14.com/api/query/userid?userid=" + userId;

            HttpResponseMessage response = client.Send(new HttpRequestMessage(HttpMethod.Get, apiUrl));

            if (!response.IsSuccessStatusCode)
                return "Unknown";

            string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var jsonObject = JsonDocument.Parse(jsonResponse).RootElement;

            return jsonObject.GetProperty("userName").GetString() ?? "Unknown";
        }
    }
}
