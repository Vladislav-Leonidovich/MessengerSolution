using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Identity
{
    public class DeviceInfoDto
    {
        public int Id { get; set; }
        public string DeviceName { get; set; } = "Unknown Device";
        public string DeviceType { get; set; } = "Unknown";
        public string OperatingSystem { get; set; } = "Unknown";
        public string OsVersion { get; set; } = "Unknown";
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public bool IsCurrentDevice { get; set; }
    }
}
