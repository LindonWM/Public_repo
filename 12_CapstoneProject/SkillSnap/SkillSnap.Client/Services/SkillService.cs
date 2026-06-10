using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class SkillService
{
    private readonly HttpClient _httpClient;

    public SkillService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Skill>> GetSkillsAsync()
    {
        var skills = await _httpClient.GetFromJsonAsync<List<Skill>>("api/skills");
        return skills ?? [];
    }

    public async Task<Skill?> AddSkillAsync(Skill newSkill)
    {
        var response = await _httpClient.PostAsJsonAsync("api/skills", newSkill);
        await EnsureSuccessAsync(response, "Add skill");

        return await response.Content.ReadFromJsonAsync<Skill>();
    }

    public async Task UpdateSkillAsync(int id, Skill updatedSkill)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/skills/{id}", updatedSkill);
        await EnsureSuccessAsync(response, "Update skill");
    }

    public async Task DeleteSkillAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/skills/{id}");

        await EnsureSuccessAsync(response, "Delete skill");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string action)
    {
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"{action} failed ({(int)response.StatusCode} {response.ReasonPhrase}). {details}"
            );
        }
    }
}
