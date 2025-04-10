using ChatServiceDTOs.Folders;
using ChatService.Models;
using Shared.Responses;
namespace ChatService.Services
{
    public interface IFolderService
    {
        Task<ApiResponse<IEnumerable<FolderDto>>> GetFoldersAsync(int userId);
        Task<ApiResponse<FolderDto>> GetFolderByIdAsync(int folderId, int userId);
        Task<ApiResponse<FolderDto>> CreateFolderAsync(CreateFolderDto model, int userId);
        Task<ApiResponse<bool>> UpdateFolderAsync(FolderDto folderDto, int userId);
        Task<ApiResponse<bool>> DeleteFolderAsync(int folderId, int userId);
        Task<ApiResponse<bool>> AssignChatToFolderAsync(int chatId, int folderId, bool isGroupChat, int userId);
        Task<ApiResponse<bool>> UnassignChatFromFolderAsync(int chatId, bool isGroupChat, int userId);
    }
}
