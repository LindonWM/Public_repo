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
    public class SkillsController : ControllerBase
    {
        private const string SkillsCacheKey = "skills:all";
        private readonly SkillSnapContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SkillsController> _logger;

        public SkillsController(
            SkillSnapContext context,
            IMemoryCache cache,
            ILogger<SkillsController> logger
        )
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
        {
            var stopwatch = Stopwatch.StartNew();

            // Read-through cache to reduce repeated DB reads for listing endpoints.
            if (!_cache.TryGetValue(SkillsCacheKey, out List<Skill>? skills))
            {
                // Project to a trimmed graph to avoid serializing full EF navigation trees.
                skills = await _context
                    .Skills.AsNoTracking()
                    .Include(s => s.PortfolioUser)
                    .Select(s => new Skill
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Level = s.Level,
                        IconUrl = s.IconUrl,
                        PortfolioUserId = s.PortfolioUserId,
                        PortfolioUser =
                            s.PortfolioUser == null
                                ? null
                                : new PortfolioUser
                                {
                                    Id = s.PortfolioUser.Id,
                                    Name = s.PortfolioUser.Name,
                                    Bio = s.PortfolioUser.Bio,
                                    ProfileImageUrl = s.PortfolioUser.ProfileImageUrl,
                                },
                    })
                    .ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    // Short TTL keeps list responses fast while still reflecting recent writes.
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                };

                _cache.Set(SkillsCacheKey, skills, cacheEntryOptions);
                stopwatch.Stop();
                _logger.LogInformation(
                    "Skills GET cache MISS. Count={Count}, DurationMs={DurationMs}",
                    skills.Count,
                    stopwatch.ElapsedMilliseconds
                );
            }
            else
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "Skills GET cache HIT. Count={Count}, DurationMs={DurationMs}",
                    skills?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds
                );
            }

            return Ok(skills);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Skill>> CreateSkill(Skill skill)
        {
            // Enforce that each skill is linked to a valid portfolio owner.
            if (skill.PortfolioUserId <= 0)
            {
                return BadRequest(
                    new { Message = "portfolioUserId is required and must be greater than 0." }
                );
            }

            var portfolioUserExists = await _context.PortfolioUsers.AnyAsync(u =>
                u.Id == skill.PortfolioUserId
            );
            if (!portfolioUserExists)
            {
                return BadRequest(
                    new
                    {
                        Message = $"PortfolioUser with id {skill.PortfolioUserId} does not exist.",
                    }
                );
            }

            await _context.Skills.AddAsync(skill);
            await _context.SaveChangesAsync();

            // Ensure next GET returns fresh data after a successful create.
            _cache.Remove(SkillsCacheKey);

            return Created($"api/skills/{skill.Id}", skill);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSkill(int id, Skill skill)
        {
            // Keep validation consistent with create so clients get predictable errors.
            if (skill.PortfolioUserId <= 0)
            {
                return BadRequest(
                    new { Message = "portfolioUserId is required and must be greater than 0." }
                );
            }

            var portfolioUserExists = await _context.PortfolioUsers.AnyAsync(u =>
                u.Id == skill.PortfolioUserId
            );
            if (!portfolioUserExists)
            {
                return BadRequest(
                    new
                    {
                        Message = $"PortfolioUser with id {skill.PortfolioUserId} does not exist.",
                    }
                );
            }

            var existingSkill = await _context.Skills.FindAsync(id);
            if (existingSkill is null)
            {
                return NotFound(new { Message = $"Skill with id {id} was not found." });
            }

            // Apply only mutable fields from payload onto the tracked entity.
            existingSkill.Name = skill.Name;
            existingSkill.Level = skill.Level;
            existingSkill.IconUrl = skill.IconUrl;
            existingSkill.PortfolioUserId = skill.PortfolioUserId;

            await _context.SaveChangesAsync();

            // Ensure next GET returns fresh data after a successful update.
            _cache.Remove(SkillsCacheKey);

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill is null)
            {
                return NotFound(new { Message = $"Skill with id {id} was not found." });
            }

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();

            // Ensure next GET returns fresh data after a successful delete.
            _cache.Remove(SkillsCacheKey);

            return NoContent();
        }
    }
}
