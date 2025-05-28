using Shared.DTOs.Folder;

namespace ChatService.Repositories.Interfaces
{
    public interface IFolderRepository
    {
        Task<IEnumerable<FolderDto>> GetFoldersForUserAsync(int userId);
        Task<FolderDto?> GetFolderByIdAsync(int folderId);
        Task<FolderDto> CreateFolderAsync(CreateFolderDto dto, int userId);
        Task<bool> UpdateFolderAsync(FolderDto folderDto, int userId);
        Task<bool> DeleteFolderAsync(int folderId);
        Task<bool> AssignChatToFolderAsync(int chatId, int folderId, bool isGroupChat);
        Task<bool> UnassignChatFromFolderAsync(int chatId, bool isGroupChat);
        Task<bool> CanAccessFolderAsync(int userId, int folderId);
        Task<bool> IsFolderOwnerAsync(int userId, int folderId);
    }
}
