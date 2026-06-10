using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class PortfolioUserService
{
    private readonly HttpClient _httpClient;

    public PortfolioUserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<PortfolioUser>> GetPortfolioUsersAsync()
    {
        var users = await _httpClient.GetFromJsonAsync<List<PortfolioUser>>("api/portfoliousers");
        return users ?? [];
    }
}
