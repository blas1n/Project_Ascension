using System.Net.Http.Json;

namespace ProjectAscension.GameServer;

public class ApiReporter
{
    private readonly HttpClient _http;

    public ApiReporter(string apiBaseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    }

    public async Task ReportMonsterKilledAsync(Guid actorId, Guid monsterId)
    {
        var payload = new { actorId, monsterId, killedAt = DateTime.UtcNow };
        try { await _http.PostAsJsonAsync("/api/internal/monster-killed", payload); }
        catch (Exception ex) { Console.WriteLine($"[ApiReporter] {ex.Message}"); }
    }

    public async Task ReportDiscoveryCandidateAsync(Guid actorId, string candidateKey, int progress)
    {
        var payload = new { actorId, candidateKey, progress };
        try { await _http.PostAsJsonAsync("/api/internal/discovery-progress", payload); }
        catch (Exception ex) { Console.WriteLine($"[ApiReporter] {ex.Message}"); }
    }
}
