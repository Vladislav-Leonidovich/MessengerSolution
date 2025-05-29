namespace IdentityService.Services.Interfaces
{
    public interface IDeviceService
    {
        Task<bool> GetDevices();
        Task<bool> LogoutDevice(int id);
        Task<bool> LogoutAllDevices();
    }
}
