namespace Shared.DTOs.Identity
{
    public class AuthDto
    {
        public string Token { get; set; } = null!;
        public DateTime TokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
