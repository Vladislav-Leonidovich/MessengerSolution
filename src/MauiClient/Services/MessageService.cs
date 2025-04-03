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

        public MessageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7103/chathub") // URL SignalR хабу
            .Build();

            _connection.On<MessageCreatedEvent>("ReceiveMessage", (message) =>
            {
                var newMessage = new MessageDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,
                    SenderUserId = message.SenderUserId,
                    ChatRoomId = message.ChatRoomId,
                    ChatRoomType = message.ChatRoomType,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };
                _messages.Add(newMessage);
                OnNewMessageReceived?.Invoke(newMessage);
            });
        }

        public async Task StartConnectionAsync()
        {
            var hubUrl = "https://localhost:7100/messageHub";
            _connection = new HubConnectionBuilder()
           .WithUrl(hubUrl, options =>
           {
               // (Опционально) для разработки можно разрешить самоподписанные сертификаты:
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

            // Подписываемся на события от хаба (если необходимо)
            _connection.On<MessageDto>("ReceiveMessage", (message) =>
            {
                OnNewMessageReceived?.Invoke(message);
            });

            await _connection.StartAsync();
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
