using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.DTOs.Chat;
using Shared.DTOs.Message;
using Shared.DTOs.Responses;

namespace MauiClient.Services
{
    // Реалізація сервісу роботи з чатами
    public class ChatService : IChatService
    {
        private readonly System.Net.Http.HttpClient _httpClient;
        private HubConnection? _connection;
        private string _hubUrl = "https://localhost:7100/chatHub";
        public event Action? OnChatCreated;
        private bool _isConnected = false;

        public ChatService(System.Net.Http.HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task StartConnectionAsync()
        {
            if (_isConnected)
                return;

            _connection = new HubConnectionBuilder()
               .WithUrl(_hubUrl, options =>
               {
                   options.HttpMessageHandlerFactory = (handler) =>
                   {
                       if (handler is HttpClientHandler clientHandler)
                       {
                           clientHandler.ServerCertificateCustomValidationCallback +=
                               (sender, cert, chain, sslPolicyErrors) => true;
                       }
                       return handler;
                   };
               })
               .WithAutomaticReconnect()
               .Build();

            _connection.On("ChatCreated", () =>
            {
                OnChatCreated?.Invoke();
            });

            try
            {
                await _connection.StartAsync();
                _isConnected = true;
                Console.WriteLine("Подключено к SignalR хабу");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения к SignalR: {ex.Message}");
                throw;
            }
        }

        // Добавьте методы для присоединения/покидания группы чата
        public async Task JoinChatRoomAsync(int chatRoomId)
        {
            if (!_isConnected)
                await StartConnectionAsync();

            Console.WriteLine($"Присоединяемся к группе чата: {chatRoomId}");
            await _connection!.InvokeAsync("JoinGroup", chatRoomId.ToString());
        }

        public async Task LeaveChatRoomAsync(int chatRoomId)
        {
            if (!_isConnected)
                return;

            Console.WriteLine($"Покидаем группу чата: {chatRoomId}");
            await _connection!.InvokeAsync("LeaveGroup", chatRoomId.ToString());
        }

        public async Task<ChatRoomDto?> CreatePrivateChatRoomAsync(CreatePrivateChatRoomDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat/create-private", model);
            if (response.IsSuccessStatusCode)
            {
                var chatRoom = await response.Content.ReadFromJsonAsync<ApiResponse<ChatRoomDto>>();
                return chatRoom?.Data;
            }
            return null;
        }

        public async Task<GroupChatRoomDto?> CreateGroupChatRoomAsync(CreateGroupChatRoomDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat/create-group", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GroupChatRoomDto>();
            }
            return null;
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsWithoutFolderAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<ChatRoomDto>>>("api/chat/private/no-folder");
            return response?.Data ?? new List<ChatRoomDto>();
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsWithoutFolderAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<GroupChatRoomDto>>("api/chat/group/no-folder");
            return response ?? new List<GroupChatRoomDto>();
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsForFolderAsync(int folderId)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<ChatRoomDto>>>($"api/chat/private/{folderId}");
            return response?.Data ?? new List<ChatRoomDto>();
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsForFolderAsync(int folderId)
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<GroupChatRoomDto>>($"api/chat/group/{folderId}");
            return response ?? new List<GroupChatRoomDto>();
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsAsync()
        {
            try
            {
                // Отримуємо відповідь
                var httpResponse = await _httpClient.GetAsync("api/chat/private");
                httpResponse.EnsureSuccessStatusCode();

                var jsonContent = await httpResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Відповідь сервера: {jsonContent}");

                // Важливо: налаштовуємо опції десеріалізації для ігнорування регістру
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Це ключове налаштування!
                };

                try
                {
                    // Використовуємо опції при десеріалізації
                    var response = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<IEnumerable<ChatRoomDto>>>(
                        jsonContent, options);

                    // Перевіряємо результат
                    if (response?.Data != null)
                    {
                        var chatList = response.Data.ToList();
                        Console.WriteLine($"Успішно десеріалізовано {chatList.Count} чатів:");

                        foreach (var chat in chatList)
                        {
                            Console.WriteLine($"  - Чат #{chat.Id}: {chat.Name}");
                        }

                        return chatList;
                    }
                    else
                    {
                        Console.WriteLine("Дані після десеріалізації відсутні (Data = null або порожній список)");
                        return new List<ChatRoomDto>();
                    }
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    Console.WriteLine($"Помилка десеріалізації JSON: {jsonEx.Message}");
                    Console.WriteLine($"Стек викликів: {jsonEx.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Загальна помилка при отриманні чатів: {ex.Message}");
                return new List<ChatRoomDto>();
            }
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<GroupChatRoomDto>>("api/chat/group");
            return response ?? new List<GroupChatRoomDto>();
        }

        public async Task<ChatRoomDto> GetPrivateChatRoomAsync(int chatId)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ChatRoomDto>>($"api/chat/private/{chatId}");
            return response?.Data ?? new ChatRoomDto();
        }

        public async Task<GroupChatRoomDto> GetGroupChatRoomAsync(int chatId)
        {
            var response = await _httpClient.GetFromJsonAsync<GroupChatRoomDto>($"api/chat/group/{chatId}");
            return response ?? new GroupChatRoomDto();
        }

        public async Task<bool> DeletePrivateChatRoomAsync(int chatId)
        {
            var response = await _httpClient.DeleteAsync($"api/chat/delete-private/{chatId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteGroupChatRoomAsync(int chatId)
        {
            var response = await _httpClient.DeleteAsync($"api/chat/delete-group/{chatId}");
            return response.IsSuccessStatusCode;
        }
    }
}
