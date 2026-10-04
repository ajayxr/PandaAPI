using System.ComponentModel.DataAnnotations;


namespace PandaAPI.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [PandaAPI.Validation.ValidEmail]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}

