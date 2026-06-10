using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly SkillSnapContext _context;

        public SeedController(SkillSnapContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Seed()
        {
            // Dev-friendly reseed: replace old sample data with the current seed payload.
            if (_context.PortfolioUsers.Any())
            {
                _context.Skills.RemoveRange(_context.Skills);
                _context.Projects.RemoveRange(_context.Projects);
                _context.PortfolioUsers.RemoveRange(_context.PortfolioUsers);
                _context.SaveChanges();

                // Reset SQLite identity counters so IDs start from 1 after reseed.
                _context.Database.ExecuteSqlRaw(
                    "DELETE FROM sqlite_sequence WHERE name IN ('PortfolioUsers', 'Projects', 'Skills')"
                );
            }

            var users = new List<PortfolioUser>
            {
                new PortfolioUser
                {
                    Name = "Jordan Developer",
                    Bio = "Full-stack developer passionate about learning new tech.",
                    ProfileImageUrl = "https://picsum.photos/seed/jordan-profile/300/300",
                    Projects = new List<Project>
                    {
                        new Project
                        {
                            Title = "Task Tracker",
                            Description = "Manage tasks effectively",
                            ImageUrl = "https://picsum.photos/seed/task-tracker/640/360",
                        },
                        new Project
                        {
                            Title = "Weather App",
                            Description = "Forecast weather using APIs",
                            ImageUrl = "https://picsum.photos/seed/weather-app/640/360",
                        },
                    },
                    Skills = new List<Skill>
                    {
                        new Skill { Name = "C#", Level = "Advanced" },
                        new Skill { Name = "Blazor", Level = "Intermediate" },
                    },
                },
                new PortfolioUser
                {
                    Name = "Maya Chen",
                    Bio = "Frontend engineer focused on accessible and polished UI experiences.",
                    ProfileImageUrl = "https://picsum.photos/seed/maya-profile/300/300",
                    Projects = new List<Project>
                    {
                        new Project
                        {
                            Title = "Design System Hub",
                            Description = "Reusable UI components and documentation site",
                            ImageUrl = "https://picsum.photos/seed/design-system-hub/640/360",
                        },
                        new Project
                        {
                            Title = "Event Planner",
                            Description = "Plan and coordinate events with scheduling",
                            ImageUrl = "https://picsum.photos/seed/event-planner/640/360",
                        },
                    },
                    Skills = new List<Skill>
                    {
                        new Skill { Name = "TypeScript", Level = "Advanced" },
                        new Skill { Name = "CSS", Level = "Advanced" },
                    },
                },
                new PortfolioUser
                {
                    Name = "Liam Patel",
                    Bio = "Backend developer building resilient APIs and data pipelines.",
                    ProfileImageUrl = "https://picsum.photos/seed/liam-profile/300/300",
                    Projects = new List<Project>
                    {
                        new Project
                        {
                            Title = "Inventory API",
                            Description = "REST API for stock management and warehouse operations",
                            ImageUrl = "https://picsum.photos/seed/inventory-api/640/360",
                        },
                        new Project
                        {
                            Title = "Log Insights",
                            Description = "Centralized log ingestion and analytics dashboard",
                            ImageUrl = "https://picsum.photos/seed/log-insights/640/360",
                        },
                    },
                    Skills = new List<Skill>
                    {
                        new Skill { Name = "ASP.NET Core", Level = "Advanced" },
                        new Skill { Name = "SQL", Level = "Intermediate" },
                    },
                },
            };

            _context.PortfolioUsers.AddRange(users);
            _context.SaveChanges();
            return Ok("Sample data inserted.");
        }
    }
}
