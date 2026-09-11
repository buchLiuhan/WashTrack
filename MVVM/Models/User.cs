using System.ComponentModel.DataAnnotations;

namespace WashTrack.Models
{
    // The shop owner's account. WashTrack is single-user: exactly one User row
    // exists, created on first launch (see LoginViewModel.RegisterAsync).
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        // SHA-256 hex hash, never the plain password. Set via HashText().
        [Required]
        public string Password { get; set; } = string.Empty;

        // Chosen from LoginViewModel.SecurityQuestions. Stored as plain text —
        // it's shown back to the user during password recovery.
        [Required]
        public string SecurityQuestion { get; set; } = string.Empty;

        // Hashed like the password, but lower-cased and trimmed first so
        // recovery isn't case-sensitive (see NormalizeAnswer).
        [Required]
        public string SecurityAnswer { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}