
using System.Net;
using System.Text.Json;

namespace Master;

public class SlaveClient
{
    private readonly HttpClient _http;
    public SlaveClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> SetAsync(string slaveUrl, string key, string value)
    {
        string url = $"{slaveUrl}/setKey?key={Uri.EscapeDataString(key)}&value={Uri.EscapeDataString(value)}";

        using HttpResponseMessage response = await _http.GetAsync(url); 
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> GetAsync(string slaveUrl, string key)
    {
        string url = $"{slaveUrl}/getKey?key={Uri.EscapeDataString(key)}";

        using HttpResponseMessage response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null; 
        
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("value").GetString();
        
    }

    public async Task<string?> PopAsync(string slaveUrl, string key)
    {
        string url = $"{slaveUrl}/pop?key={Uri.EscapeDataString(key)}";

        using HttpResponseMessage response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("value").GetString();

    }

    public async  Task<Dictionary<string, string>> GetDataAsync(string slaveUrl)
    {
        using HttpResponseMessage response = await _http.GetAsync($"{slaveUrl}/getData");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var result = new Dictionary<string, string>();
        foreach (JsonProperty entry in body.GetProperty("data").EnumerateObject())
            result[entry.Name] = entry.Value.GetString() ?? "";

        return result;

    }
}