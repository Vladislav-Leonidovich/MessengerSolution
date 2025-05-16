using ChatService.Repositories.Interfaces;
using MassTransit;
using Shared.Sagas;

namespace ChatService.Sagas.ChatCreation.Consumers
{
    public class CreateChatRoomCommandConsumer : IConsumer<CreateChatRoomCommand>
    {
        private readonly ILogger<CreateChatRoomCommandConsumer> _logger;
        private readonly IChatRoomRepository _chatRoomRepository;

        public CreateChatRoomCommandConsumer(
            ILogger<CreateChatRoomCommandConsumer> logger,
            IChatRoomRepository chatRoomRepository)
        {
            _logger = logger;
            _chatRoomRepository = chatRoomRepository;
        }
        public async Task Consume(ConsumeContext<CreateChatRoomCommand> context)
        {
            // Реалізуйте логіку створення кімнати чату
            _logger.LogInformation("Створення кімнати чату з ID {ChatRoomId} для користувача {CreatorUserId}", context.Message.ChatRoomId, context.Message.CreatorUserId);
            
        }
    }
}
