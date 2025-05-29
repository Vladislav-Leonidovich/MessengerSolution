
using IdentityService.Services.Interfaces;

namespace IdentityService.Services
{
    public class DeviceService : IDeviceService
    {
        public Task<bool> GetDevices()
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogoutAllDevices()
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogoutDevice(int id)
        {
            throw new NotImplementedException();
        }
    }
}
