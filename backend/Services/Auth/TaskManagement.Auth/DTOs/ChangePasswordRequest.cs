using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Auth.DTOs;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}