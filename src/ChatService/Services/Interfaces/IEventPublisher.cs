using ChatService.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatService.Services.Interfaces
{
    public interface IEventPublisher
    {
        // Створює подію без збереження
        Task<OutboxMessage> CreateEventAsync<TEvent>(TEvent @event) where TEvent : class;

        // Публікує подію в рамках існуючої транзакції
        Task PublishInTransactionAsync<TEvent>(TEvent @event, IDbContextTransaction transaction) where TEvent : class;

        // Публікує подію з автоматичним створенням транзакції
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
    }
}
