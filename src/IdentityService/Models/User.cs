namespace IdentityService.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
