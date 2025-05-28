using IdentityService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Identity;

namespace IdentityService.Services
{
    public class SearchService : ISearchService
    {
        private readonly IdentityDbContext _context;

        public SearchService(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<UserDto?> SearchUsersByUsernameAsync(string username)
        {
            return await _context.Users
                .Where(u => u.UserName == username)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    DisplayName = u.DisplayName
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserDto?> SearchUsersByUserIdAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    DisplayName = u.DisplayName
                })
                .FirstOrDefaultAsync();
        }
    }
}
