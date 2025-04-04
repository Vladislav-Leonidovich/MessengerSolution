namespace IdentityService.DTOs
{
    public class LoginDto
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string DeviceName { get; set; } = "Unknown Device";
        public string DeviceType { get; set; } = "Unknown";
        public string OperatingSystem { get; set; } = "Unknown";
        public string OsVersion { get; set; } = "Unknown";
    }
}
