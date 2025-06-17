using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.Contracts;
using Shared.DTOs.Message;
using Shared.DTOs.Responses;

namespace MauiClient.Services
{
    public class MessageService : IMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private HubConnection? _connection;
        private List<MessageDto> _messages = new List<MessageDto>();
        public event Action<MessageDto>? OnNewMessageReceived;
        private string _hubUrl = "https://localhost:7100/messageHub";
        private bool _isConnected = false;

        public MessageService(HttpClient httpClient, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
        }

        public async Task StartConnectionAsync()
        {
            // Отримуємо JWT токен
            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Токен аутентифікації відсутній або пустий");
            }

            if (_isConnected)
                return;

            _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                // Передаємо токен через AccessTokenProvider
                options.AccessTokenProvider = async () => await Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

            // Регистрация обработчика сообщений
            _connection.On<MessageDto>("ReceiveMessage", (message) =>
            {
                Console.WriteLine($"Получено новое сообщение: {message.Content}");
                OnNewMessageReceived?.Invoke(message);
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

        public async Task<MessageDto?> SendMessageAsync(int chatRoomId, string content)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/message/send/{chatRoomId}", content);
            if(response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>();
                return message?.Data;
            }
            return null;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int startIndex, int count)
        {
            var httpResponse = await _httpClient.GetAsync($"api/message/chat/{chatRoomId}?startIndex={startIndex}&count={count}");
            httpResponse.EnsureSuccessStatusCode();

            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Це ключове налаштування!
            };

            var response = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<List<MessageDto>>>(jsonContent, options);
            return response?.Data ?? new List<MessageDto>();
        }

        public async Task<MessageDto?> MarkMessageAsRead(int messageId)
        {
            var response = await _httpClient.PostAsync($"api/message/mark-read/{messageId}", null);
            if (response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>();
                return message?.Data;
            }
            return null;
        }

        public async Task<bool> DeleteMessageAsync(int messageId)
        {
            var response = await _httpClient.DeleteAsync($"api/message/delete/{messageId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteMessagesByChatRoomId(int chatRoomId)
        {
            var response = await _httpClient.DeleteAsync($"api/message/delete-by-chat/{chatRoomId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<ulong?> GetMessagesCountByChatRoomIdAsync(int chatRoomId)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ulong>>($"api/message/count/{chatRoomId}");
            return response?.Data;
        }
    }
}
