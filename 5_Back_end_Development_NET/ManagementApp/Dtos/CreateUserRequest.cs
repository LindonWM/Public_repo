using System.ComponentModel.DataAnnotations;

namespace ManagementApp.Dtos;

public class CreateUserRequest
{
    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "FirstName cannot be empty or whitespace.")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "LastName cannot be empty or whitespace.")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Email cannot be empty or whitespace.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [RegularExpression(@"^\+?[0-9()\-\s]{7,20}$", ErrorMessage = "PhoneNumber format is invalid.")]
    public string? PhoneNumber { get; set; }

    [StringLength(255)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? Position { get; set; }
}