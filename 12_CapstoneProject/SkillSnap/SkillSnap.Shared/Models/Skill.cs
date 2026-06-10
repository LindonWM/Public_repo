using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Shared.Models;

public class Skill
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Level { get; set; }

    public string? IconUrl { get; set; }

    [ForeignKey("PortfolioUser")]
    public int PortfolioUserId { get; set; }

    public PortfolioUser? PortfolioUser { get; set; }
}
