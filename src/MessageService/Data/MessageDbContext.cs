using System.Collections.Generic;
using System.Reflection.Emit;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Outbox;

namespace MessageService.Data
{
    public class MessageDbContext : DbContext
    {
        public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options)
        {
        }

        // Таблица сообщений
        public DbSet<Message> Messages { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<MessageOperation> MessageOperations { get; set; }
        public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

        // При необходимости можно переопределить OnModelCreating для дополнительной конфигурации моделей
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Пример: можно задать индексы, ограничения, отношения и т.д.
            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ChatRoomId);

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.CorrelationId);

            modelBuilder.Entity<Shared.Outbox.OutboxMessage>()
        .Property(x => x.NextRetryAt);

            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(o => o.ProcessedAt);

            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(o => o.CreatedAt);

            modelBuilder.Entity<ProcessedEvent>()
                .HasKey(p => new { p.EventId, p.EventType });

            modelBuilder.Entity<ProcessedEvent>()
                .HasIndex(p => p.ProcessedAt);

            modelBuilder.Entity<MessageOperation>()
                .HasKey(mo => mo.CorrelationId);

            modelBuilder.Entity<MessageOperation>()
                .HasIndex(mo => mo.MessageId);

            modelBuilder.Entity<MessageOperation>()
                .HasIndex(mo => mo.ChatRoomId);

            modelBuilder.Entity<MessageOperation>()
                .HasIndex(mo => mo.Status);

            modelBuilder.Entity<MessageOperation>()
                .HasIndex(mo => mo.CreatedAt);
        }
    }
}
