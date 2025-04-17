using ChatServiceDTOs.Folders;
using ChatService.Models;
using ChatService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ChatService.Authorization;
using ChatService.Repositories.Interfaces;
using Shared.Exceptions;
using Shared.Responses;
using ChatService.Services.Interfaces;

namespace ChatService.Services
{
    public class FolderService : IFolderService
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IChatAuthorizationService _authService;
        private readonly ILogger<FolderService> _logger;

        public FolderService(
            IFolderRepository folderRepository,
            IChatAuthorizationService authService,
            ILogger<FolderService> logger)
        {
            _folderRepository = folderRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<FolderDto>>> GetFoldersAsync(int userId)
        {
            try
            {
                var folders = await _folderRepository.GetFoldersForUserAsync(userId);
                return ApiResponse<IEnumerable<FolderDto>>.Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні папок для користувача {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<FolderDto>> GetFolderByIdAsync(int folderId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessFolder(userId, folderId);

                var folder = await _folderRepository.GetFolderByIdAsync(folderId);
                if (folder == null)
                {
                    throw new EntityNotFoundException("Folder", folderId);
                }

                return ApiResponse<FolderDto>.Ok(folder);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<FolderDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні папки {FolderId}", folderId);
                throw;
            }
        }

        public async Task<ApiResponse<FolderDto>> CreateFolderAsync(CreateFolderDto model, int userId)
        {
            try
            {
                // Валідація даних
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    throw new ValidationException("Назва папки не може бути порожньою");
                }

                var folder = await _folderRepository.CreateFolderAsync(model, userId);
                return ApiResponse<FolderDto>.Ok(folder, "Папку успішно створено");
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<FolderDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні папки для користувача {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<bool>> UpdateFolderAsync(FolderDto folderDto, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessFolder(userId, folderDto.Id);
                // Валідація даних
                if (string.IsNullOrWhiteSpace(folderDto.Name))
                {
                    throw new ValidationException("Назва папки не може бути порожньою");
                }
                var result = await _folderRepository.UpdateFolderAsync(folderDto, userId);
                return ApiResponse<bool>.Ok(result, "Папку успішно оновлено");
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні папки {FolderId} для користувача {UserId}", folderDto.Id, userId);
                throw;
            }
        }

        public async Task<ApiResponse<bool>> DeleteFolderAsync(int folderId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessFolder(userId, folderId);
                var result = await _folderRepository.DeleteFolderAsync(folderId);
                return ApiResponse<bool>.Ok(result, "Папку успішно видалено");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні папки {FolderId} для користувача {UserId}", folderId, userId);
                throw;
            }
        }

        public async Task<ApiResponse<bool>> AssignChatToFolderAsync(int chatId, int folderId, bool isGroupChat, int userId)
        {
            try
            {
                // Перевірка доступу до папки
                await _authService.EnsureCanAccessFolder(userId, folderId);
                // Перевірка доступу до чату
                if (!await _authService.CanAccessChatRoom(userId, chatId))
                {
                    throw new ForbiddenAccessException($"У вас немає доступу до чату з ID {chatId}");
                }
                var result = await _folderRepository.AssignChatToFolderAsync(chatId, folderId, isGroupChat);
                return ApiResponse<bool>.Ok(result, "Чат успішно призначено на папку");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при призначенні чату {ChatId} на папку {FolderId}", chatId, folderId);
                throw;
            }
        }

        public async Task<ApiResponse<bool>> UnassignChatFromFolderAsync(int chatId, bool isGroupChat, int userId)
        {
            try
            {
                // Перевірка доступу до чату
                if (!await _authService.CanAccessChatRoom(userId, chatId))
                {
                    throw new ForbiddenAccessException($"У вас немає доступу до чату з ID {chatId}");
                }
                var result = await _folderRepository.UnassignChatFromFolderAsync(chatId, isGroupChat);
                return ApiResponse<bool>.Ok(result, "Чат успішно видалено з папки");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні чату {ChatId} з папки", chatId);
                throw;
            }
        }
    }
}
