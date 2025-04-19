using System.Collections.Generic;
using System.Reflection.Emit;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;

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

        // При необходимости можно переопределить OnModelCreating для дополнительной конфигурации моделей
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Пример: можно задать индексы, ограничения, отношения и т.д.
            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ChatRoomId);
        }
    }
}
