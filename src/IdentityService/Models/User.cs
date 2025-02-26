using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression("^@.*", ErrorMessage = "UserName must start with '@'")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "UserName має бути від 3 до 50 символів")]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress(ErrorMessage = "Невірний формат пошти")]
        public string Email { get; set; } = null!;

        [Required]
        public byte[] PasswordHash { get; set; } = null!;

        [Required]
        public byte[] PasswordSalt { get; set; } = null!;

        [Required]
        public string DisplayName { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
