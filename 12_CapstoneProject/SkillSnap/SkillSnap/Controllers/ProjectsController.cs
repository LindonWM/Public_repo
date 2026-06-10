using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private const string ProjectsCacheKey = "projects:all";
        private readonly SkillSnapContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            SkillSnapContext context,
            IMemoryCache cache,
            ILogger<ProjectsController> logger
        )
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            var stopwatch = Stopwatch.StartNew();

            // Read-through cache to avoid repeated DB work for frequently viewed lists.
            if (!_cache.TryGetValue(ProjectsCacheKey, out List<Project>? projects))
            {
                // Return a lean shape that includes owner basics needed by the client.
                projects = await _context
                    .Projects.AsNoTracking()
                    .Include(p => p.PortfolioUser)
                    .Select(p => new Project
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Description = p.Description,
                        ImageUrl = p.ImageUrl,
                        PortfolioUserId = p.PortfolioUserId,
                        PortfolioUser =
                            p.PortfolioUser == null
                                ? null
                                : new PortfolioUser
                                {
                                    Id = p.PortfolioUser.Id,
                                    Name = p.PortfolioUser.Name,
                                    Bio = p.PortfolioUser.Bio,
                                    ProfileImageUrl = p.PortfolioUser.ProfileImageUrl,
                                },
                    })
                    .ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    // Short-lived cache balances freshness and performance.
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                };

                _cache.Set(ProjectsCacheKey, projects, cacheEntryOptions);
                stopwatch.Stop();
                _logger.LogInformation(
                    "Projects GET cache MISS. Count={Count}, DurationMs={DurationMs}",
                    projects.Count,
                    stopwatch.ElapsedMilliseconds
                );
            }
            else
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "Projects GET cache HIT. Count={Count}, DurationMs={DurationMs}",
                    projects?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds
                );
            }

            return Ok(projects);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject(Project project)
        {
            // Ensure every project references a valid portfolio owner.
            if (project.PortfolioUserId <= 0)
            {
                return BadRequest(
                    new { Message = "portfolioUserId is required and must be greater than 0." }
                );
            }

            var portfolioUserExists = await _context.PortfolioUsers.AnyAsync(u =>
                u.Id == project.PortfolioUserId
            );
            if (!portfolioUserExists)
            {
                return BadRequest(
                    new
                    {
                        Message = $"PortfolioUser with id {project.PortfolioUserId} does not exist.",
                    }
                );
            }

            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Ensure next GET returns fresh data after a successful create.
            _cache.Remove(ProjectsCacheKey);

            return Created($"api/projects/{project.Id}", project);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProject(int id, Project project)
        {
            // Keep update validation aligned with create behavior.
            if (project.PortfolioUserId <= 0)
            {
                return BadRequest(
                    new { Message = "portfolioUserId is required and must be greater than 0." }
                );
            }

            var portfolioUserExists = await _context.PortfolioUsers.AnyAsync(u =>
                u.Id == project.PortfolioUserId
            );
            if (!portfolioUserExists)
            {
                return BadRequest(
                    new
                    {
                        Message = $"PortfolioUser with id {project.PortfolioUserId} does not exist.",
                    }
                );
            }

            var existingProject = await _context.Projects.FindAsync(id);
            if (existingProject is null)
            {
                return NotFound(new { Message = $"Project with id {id} was not found." });
            }

            // Copy mutable fields onto the tracked entity before saving.
            existingProject.Title = project.Title;
            existingProject.Description = project.Description;
            existingProject.ImageUrl = project.ImageUrl;
            existingProject.PortfolioUserId = project.PortfolioUserId;

            await _context.SaveChangesAsync();

            // Ensure next GET returns fresh data after a successful update.
            _cache.Remove(ProjectsCacheKey);

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project is null)
            {
                return NotFound(new { Message = $"Project with id {id} was not found." });
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            // Ensure next GET returns fresh data after a successful delete.
            _cache.Remove(ProjectsCacheKey);

            return NoContent();
        }
    }
}
