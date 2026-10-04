using System.ComponentModel.DataAnnotations;

namespace PandaAPI.DTOs.Auth;

public class LoginDto
{
    [Required]
    [StringLength(200)]
    [PandaAPI.Validation.ValidEmail]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Password { get; set; } = string.Empty;
}
