using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioUsersController : ControllerBase
    {
        private readonly SkillSnapContext _context;

        public PortfolioUsersController(SkillSnapContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortfolioUser>>> GetPortfolioUsers()
        {
            var users = await _context
                .PortfolioUsers.AsNoTracking()
                .OrderBy(u => u.Name)
                .Select(u => new PortfolioUser
                {
                    Id = u.Id,
                    Name = u.Name,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
