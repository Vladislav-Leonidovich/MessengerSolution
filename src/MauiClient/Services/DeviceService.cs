using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Shared.IdentityServiceDTOs;

namespace MauiClient.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly HttpClient _httpClient;

        public DeviceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<DeviceInfoDto>> GetDevicesAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<DeviceInfoDto>>("api/devices");
            return response ?? new List<DeviceInfoDto>();
        }

        public async Task<bool> LogoutDeviceAsync(int deviceId)
        {
            var response = await _httpClient.DeleteAsync($"api/devices/{deviceId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> LogoutAllDevicesAsync()
        {
            var response = await _httpClient.DeleteAsync("api/devices/logout-all");
            return response.IsSuccessStatusCode;
        }
    }
}
