using IdentityService.Data;
using IdentityService.Repositories.Interfaces;
using IdentityService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Identity;
using Shared.DTOs.Responses;
using Shared.Exceptions;

namespace IdentityService.Services
{
    public class SearchService : ISearchService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SearchService> _logger;

        public SearchService(IUserRepository userRepository, ILogger<SearchService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<UserDto>> SearchUsersByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ApiResponse<UserDto>.Fail("Username cannot be null or empty.");
                }
                var user = await _userRepository.GetUsersByUsernameAsync(username);
                return ApiResponse<UserDto>.Ok(user, "User found successfully.");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found with username: {Username}", username);
                return ApiResponse<UserDto>.Fail("User not found.");
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Database error occurred while searching for user by username: {Username}", username);
                return ApiResponse<UserDto>.Fail("An error occurred while processing your request. Please try again later.");
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Username cannot be null or empty.");
                return ApiResponse<UserDto>.Fail("Username cannot be null or empty.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for user by username: {Username}", username);
                return ApiResponse<UserDto>.Fail("An error occurred while processing your request.");
            }
        }

        public async Task<ApiResponse<UserDto>> SearchUsersByUserIdAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetUsersByUserIdAsync(userId);
                return ApiResponse<UserDto>.Ok(user, "User found successfully.");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found with userId: {UserId}", userId);
                return ApiResponse<UserDto>.Fail("User not found.");
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Database error occurred while searching for user by userId: {UserId}", userId);
                return ApiResponse<UserDto>.Fail("An error occurred while processing your request. Please try again later.");
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Username cannot be null or empty.");
                return ApiResponse<UserDto>.Fail("Username cannot be null or empty.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for user by userId: {UserId}", userId);
                return ApiResponse<UserDto>.Fail("An error occurred while processing your request.");
            }
        }
    }
}
