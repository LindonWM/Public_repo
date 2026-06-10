using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Shared.Models;

namespace SkillSnap.Data;

public class ApplicationUser : IdentityUser
{
    // Additional properties can be added here if needed
}

public class SkillSnapContext : IdentityDbContext<ApplicationUser>
{
    public SkillSnapContext(DbContextOptions<SkillSnapContext> options)
        : base(options) { }

    public DbSet<PortfolioUser> PortfolioUsers { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Skill> Skills { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<PortfolioUser>()
            .HasMany(user => user.Projects)
            .WithOne(project => project.PortfolioUser)
            .HasForeignKey(project => project.PortfolioUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
