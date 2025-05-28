using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Folder;

namespace MauiClient.Services
{
    public interface IFolderService
    {
        Task<IEnumerable<FolderDto>> GetFoldersAsync();
        Task<FolderDto?> CreateFolderAsync(CreateFolderDto model);
        Task<FolderDto?> UpdateFolderAsync(FolderDto folder);
        Task<bool> DeleteFolderAsync(int folderId);
        Task<bool> AssignPrivateChatToFolderAsync(int folderId, int chatId);
        Task<bool> UnassignPrivateChatFromFolderAsync(int folderId, int chatId);
        Task<bool> AssignGroupChatToFolderAsync(int folderId, int chatId);
        Task<bool> UnassignGroupChatFromFolderAsync(int folderId, int chatId);
    }
}
