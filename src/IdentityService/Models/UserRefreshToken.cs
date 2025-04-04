namespace IdentityService.Models
{
    public class UserRefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        // Information about the device used to generate the refresh token
        public string DeviceName { get; set; } = "Unknown Device";
        public string DeviceType { get; set; } = "Unknown";
        public string OperatingSystem { get; set; } = "Unknown";
        public string OsVersion { get; set; } = "Unknown";
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
