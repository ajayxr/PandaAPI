using System.ComponentModel.DataAnnotations;

namespace PandaAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        [MaxLength(200)]
        public string Name { get; set; } = String.Empty;
        [MaxLength(200)]
        public string Email { get; set; }  = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
