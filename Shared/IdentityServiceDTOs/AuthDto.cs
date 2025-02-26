namespace IdentityService.DTOs
{
    public class AuthDto
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
