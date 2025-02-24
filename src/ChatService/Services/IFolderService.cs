using ChatService.DTOs;
using ChatService.Models;
namespace ChatService.Services
{
    public interface IFolderService
    {
        // Створення нової папки
        Task<FolderDto> CreateFolderAsync(CreateFolderDto model);

        // Отримання списку папок
        Task<IEnumerable<FolderDto>> GetFoldersAsync();

        // Оновлення даних папки
        Task<bool> UpdateFolderAsync(FolderDto folderDto);

        // Видалення папки
        Task<bool> DeleteFolderAsync(int folderId);

        // Призначення чату до папки
        Task<bool> AssignChatToFolderAsync(int chatId, int folderId);

        // Відв'язування чату від папки
        Task<bool> UnassignChatFromFolderAsync(int chatId);
    }
}
