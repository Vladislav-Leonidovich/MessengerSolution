using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using MessageServiceDTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.Contracts;

namespace MauiClient.Services
{
    public class MessageService : IMessageService
    {
        private readonly HttpClient _httpClient;
        private HubConnection? _connection;
        private List<MessageDto> _messages = new List<MessageDto>();
        public event Action<MessageDto>? OnNewMessageReceived;
        private string _hubUrl = "https://localhost:7100/messageHub";
        private bool _isConnected = false;

        public MessageService(HttpClient httpClient)
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

        public async Task<MessageDto?> SendMessageAsync(SendMessageDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/message/send", model);
            if(response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MessageDto>();
            }
            return null;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int startIndex, int count)
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<MessageDto>>($"api/message/chat/{chatRoomId}?startIndex={startIndex}&count={count}");
            return response ?? new List<MessageDto>();
        }

        public async Task<MessageDto?> MarkMessageAsRead(int messageId)
        {
            var response = await _httpClient.PostAsync($"api/message/mark-read/{messageId}", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MessageDto>();
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

        public async Task<ulong> GetMessagesCountByChatRoomIdAsync(int chatRoomId)
        {
            var response = await _httpClient.GetFromJsonAsync<ulong>($"api/message/count/{chatRoomId}");
            return response;
        }
    }
}
