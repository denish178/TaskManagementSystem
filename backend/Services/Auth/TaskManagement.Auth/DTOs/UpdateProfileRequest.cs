using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Auth.DTOs;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;
}