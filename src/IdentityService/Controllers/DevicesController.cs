using IdentityService.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shared.IdentityServiceDTOs;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IdentityDbContext _context;

        public DevicesController(IdentityDbContext context)
        {
            _context = context;
        }

        // GET: api/devices
        [HttpGet]
        public async Task<IActionResult> GetDevices()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var devices = await _context.UserRefreshTokens
                .Where(rt => rt.UserId == userIdInt && rt.IsActive)
                .Select(rt => new DeviceInfoDto
                {
                    Id = rt.Id,
                    DeviceName = rt.DeviceName,
                    DeviceType = rt.DeviceType,
                    OperatingSystem = rt.OperatingSystem,
                    OsVersion = rt.OsVersion,
                    IpAddress = rt.IpAddress,
                    LastLogin = rt.LastLogin,
                    IsCurrentDevice = rt.RefreshToken == HttpContext.Request.Cookies["refreshToken"]
                })
                .ToListAsync();

            return Ok(devices);
        }

        // DELETE: api/devices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> LogoutDevice(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var token = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.UserId == userIdInt);

            if (token == null)
            {
                return NotFound();
            }

            _context.UserRefreshTokens.Remove(token);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/devices/logout-all
        [HttpDelete("logout-all")]
        public async Task<IActionResult> LogoutAllDevices()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var currentToken = HttpContext.Request.Cookies["refreshToken"];
            var tokens = await _context.UserRefreshTokens
                .Where(rt => rt.UserId == userIdInt && rt.RefreshToken != currentToken)
                .ToListAsync();

            _context.UserRefreshTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
