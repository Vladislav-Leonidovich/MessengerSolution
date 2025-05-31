using Shared.DTOs.Message;

namespace ChatService.Services.Interfaces
{
    public interface IMessageGrpcService
    {
        /// <summary>
        /// Отримує останнє повідомлення для чату
        /// </summary>
        /// <param name="chatRoomId">Ідентифікатор чату</param>
        /// <returns>Об'єкт повідомлення або null, якщо повідомлень немає</returns>
        Task<MessageDto> GetLastMessageAsync(int chatRoomId);

        /// <summary>
        /// Отримує останні повідомлення для декількох чатів
        /// </summary>
        /// <param name="chatRoomIds">Список ідентифікаторів чатів</param>
        /// <returns>Словник, де ключ - ідентифікатор чату, значення - останнє повідомлення</returns>
        Task<Dictionary<int, MessageDto>> GetLastMessagesBatchAsync(IEnumerable<int> chatRoomIds);
    }
}

