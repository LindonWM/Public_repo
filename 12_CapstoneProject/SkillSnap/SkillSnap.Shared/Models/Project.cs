using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Shared.Models;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int PortfolioUserId { get; set; }

    public PortfolioUser? PortfolioUser { get; set; }
}
