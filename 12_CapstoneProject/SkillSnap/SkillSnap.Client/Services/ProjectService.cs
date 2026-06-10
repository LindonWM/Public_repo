using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class ProjectService
{
    private readonly HttpClient _httpClient;

    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        var projects = await _httpClient.GetFromJsonAsync<List<Project>>("api/projects");
        return projects ?? [];
    }

    public async Task<Project?> AddProjectAsync(Project newProject)
    {
        var response = await _httpClient.PostAsJsonAsync("api/projects", newProject);
        await EnsureSuccessAsync(response, "Add project");

        return await response.Content.ReadFromJsonAsync<Project>();
    }

    public async Task UpdateProjectAsync(int id, Project updatedProject)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/projects/{id}", updatedProject);
        await EnsureSuccessAsync(response, "Update project");
    }

    public async Task DeleteProjectAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/projects/{id}");

        await EnsureSuccessAsync(response, "Delete project");
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
