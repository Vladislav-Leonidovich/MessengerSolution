using ChatService.Mappers.Interfaces;
using ChatService.Services.Interfaces;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Shared.DTOs.Message;
using Shared.Protos;

namespace ChatService.Services
{
    [Authorize]
    public class MessageGrpcClient : IMessageGrpcClient
    {
        private readonly Shared.Protos.MessageGrpcService.MessageGrpcServiceClient _client;
        private readonly ILogger<MessageGrpcClient> _logger;
        private readonly IMapperFactory _mapperFactory;

        public MessageGrpcClient(
            Shared.Protos.MessageGrpcService.MessageGrpcServiceClient client,
            ILogger<MessageGrpcClient> logger,
            IMapperFactory mapperFactory)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapperFactory = mapperFactory;
        }

        /// <inheritdoc/>
        public async Task<MessageDto> GetLastMessageAsync(int chatRoomId)
        {
            try
            {
                _logger.LogInformation("Отримання останнього повідомлення для чату {ChatRoomId} через gRPC", chatRoomId);

                var request = new LastMessageRequest
                {
                    ChatRoomId = chatRoomId
                };

                var response = await _client.GetLastMessageAsync(request);
                if (response != null && response.Data != null)
                {
                    return await _mapperFactory.GetMapper<MessageData, MessageDto>().MapToDtoAsync(response.Data);
                }

                return new MessageDto();
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning("Повідомлення для чату {ChatRoomId} не знайдено", chatRoomId);
                return new MessageDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останнього повідомлення для чату {ChatRoomId} через gRPC", chatRoomId);
                return new MessageDto();
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, MessageDto>> GetLastMessagesBatchAsync(IEnumerable<int> chatRoomIds)
        {
            try
            {
                _logger.LogInformation("Отримання останніх повідомлень для {Count} чатів через gRPC", chatRoomIds.Count());

                var request = new LastMessagesBatchRequest();
                request.ChatRoomIds.AddRange(chatRoomIds);

                var response = await _client.GetLastMessagesBatchAsync(request);

                var result = new Dictionary<int, MessageDto>();
                foreach (var pair in response.Messages)
                {
                    result.Add(pair.Key, await _mapperFactory.GetMapper<MessageData, MessageDto>().MapToDtoAsync(pair.Value));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останніх повідомлень для чатів через gRPC");
                return new Dictionary<int, MessageDto>();
            }
        }
    }
}
