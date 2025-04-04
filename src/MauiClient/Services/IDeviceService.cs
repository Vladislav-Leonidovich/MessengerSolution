using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.IdentityServiceDTOs;

namespace MauiClient.Services
{
    public interface IDeviceService
    {
        Task<IEnumerable<DeviceInfoDto>> GetDevicesAsync();
        Task<bool> LogoutDeviceAsync(int deviceId);
        Task<bool> LogoutAllDevicesAsync();
    }
}
