using MessageService.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace MessageService.Services.Interfaces
{
    public interface IEventPublisher
    {
        Task<OutboxMessage> CreateEventAsync<TEvent>(TEvent @event) where TEvent : class;
        Task PublishInTransactionAsync<TEvent>(TEvent @event, IDbContextTransaction transaction) where TEvent : class;
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
    }
}
