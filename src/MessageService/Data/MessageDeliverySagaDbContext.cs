using System.Text.Json;
using MessageService.Sagas.MessageDelivery;
using Microsoft.EntityFrameworkCore;

namespace MessageService.Data
{
    public class MessageDeliverySagaDbContext : DbContext
    {
        public MessageDeliverySagaDbContext(DbContextOptions<MessageDeliverySagaDbContext> options)
            : base(options)
        {
        }

        public DbSet<MessageDeliverySagaState> MessageDeliverySagas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Налаштування мапінгу для стану саги
            modelBuilder.Entity<MessageDeliverySagaState>(e =>
            {
                e.HasKey(x => x.CorrelationId);
                e.Property(x => x.CurrentState).HasMaxLength(64);
                e.Property(x => x.EncryptedContent).HasMaxLength(4000);
                e.Property(x => x.ErrorMessage).HasMaxLength(1024);

                // Зберігаємо список користувачів, яким доставлено, як JSON
                e.Property(x => x.DeliveredToUserIds)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                        v => JsonSerializer.Deserialize<List<int>>(v, JsonSerializerOptions.Default) ?? new List<int>()
                    );
            });
        }
    }
}
