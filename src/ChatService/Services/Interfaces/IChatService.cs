﻿using ChatServiceDTOs.Chats;
using ChatService.Models;
using Shared.Responses;

namespace ChatService.Services.Interfaces
{
    public interface IChatService
    {
        Task<ApiResponse<dynamic>> GetChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<ChatRoomDto>> GetPrivateChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<GroupChatRoomDto>> GetGroupChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForUserAsync(int userId);
        Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForUserAsync(int userId);
        Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForFolderAsync(int folderId, int userId);
        Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForFolderAsync(int folderId, int userId);
        Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsWithoutFolderAsync(int userId);
        Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsWithoutFolderAsync(int userId);
        Task<ApiResponse<ChatRoomDto>> CreatePrivateChatAsync(CreatePrivateChatRoomDto dto, int userId);
        Task<ApiResponse<GroupChatRoomDto>> CreateGroupChatAsync(CreateGroupChatRoomDto dto, int userId);
        Task<ApiResponse<bool>> DeleteChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<bool>> DeletePrivateChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<bool>> DeleteGroupChatByIdAsync(int chatRoomId, int userId);
        Task<bool> IsUserInChatAsync(int userId, int chatRoomId);
    }
}
