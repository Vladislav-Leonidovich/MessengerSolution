using ChatServiceDTOs.Folders;
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

        // Призначення приватного чату до папки
        Task<bool> AssignPrivateChatToFolderAsync(int chatId, int folderId);
        // Відв'язування приватного чату від папки

        Task<bool> UnassignPrivateChatFromFolderAsync(int chatId);
        // Призначення групового чату до папки

        Task<bool> AssignGroupChatToFolderAsync(int chatId, int folderId);

        // Відв'язування групового чату від папки
        Task<bool> UnassignGroupChatFromFolderAsync(int chatId);
    }
}
