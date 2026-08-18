using System.ComponentModel.DataAnnotations;

namespace WashTrack.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string SecurityQuestion { get; set; } = string.Empty;

        [Required]
        public string SecurityAnswer { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}