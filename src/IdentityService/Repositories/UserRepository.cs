using IdentityService.Data;
using IdentityService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Identity;
using Shared.Exceptions;

namespace IdentityService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IdentityDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IdentityDbContext context, ILogger<UserRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<UserDto> GetUsersByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Отримання користувача з ID {UserId}", userId);
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        DisplayName = u.DisplayName
                    })
                    .FirstOrDefaultAsync();
                if (user == null)
                {
                    _logger.LogError("Користувач з ID {UserId} не знайдений", userId);
                    throw new EntityNotFoundException("User", userId);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні користувача з ID {UserId}", userId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<UserDto> GetUsersByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    _logger.LogError("Порожнє ім'я користувача передано для отримання користувача");
                    throw new ArgumentException("Ім'я користувача не може бути порожнім", nameof(username));
                }
                _logger.LogInformation("Отримання користувача з іменем користувача {Username}", username);
                var user = await _context.Users
                    .Where(u => u.UserName == username)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        DisplayName = u.DisplayName
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogError("Користувач з іменем користувача {Username} не знайдений", username);
                    throw new EntityNotFoundException("User", username);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні користувача з іменем користувача {Username}", username);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }
    }
}
