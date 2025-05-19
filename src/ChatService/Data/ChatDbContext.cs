using Microsoft.EntityFrameworkCore;
using ChatService.Models;
using Shared.Contracts;

namespace ChatService.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
        }

        // Загальна таблиця чатів (TPH)
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<PrivateChatRoom> PrivateChatRooms { get; set; }
        public DbSet<GroupChatRoom> GroupChatRooms { get; set; }
        public DbSet<UserChatRoom> UserChatRooms { get; set; }
        public DbSet<GroupChatMember> GroupChatMembers { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<ProcessedEvent> ProcessedEvents { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PrivateChatRoom>().ToTable("PrivateChatRooms");
            modelBuilder.Entity<GroupChatRoom>().ToTable("GroupChatRooms");

            // Складовий ключ для приватного чату
            modelBuilder.Entity<UserChatRoom>()
                .HasKey(uc => new { uc.PrivateChatRoomId, uc.UserId });

            modelBuilder.Entity<PrivateChatRoom>()
                .HasMany(pt => pt.UserChatRooms)
                .WithOne(uc => uc.PrivateChatRoom)
                .HasForeignKey(uc => uc.PrivateChatRoomId);

            // Складовий ключ для групового чату
            modelBuilder.Entity<GroupChatMember>()
                .HasKey(gm => new { gm.GroupChatRoomId, gm.UserId });

            modelBuilder.Entity<GroupChatRoom>()
                .HasMany(g => g.GroupChatMembers)
                .WithOne(gm => gm.GroupChatRoom)
                .HasForeignKey(gm => gm.GroupChatRoomId);

            // Встановлюємо зв'язок між ChatRoom та Folder, використовуючи наявну властивість FolderId
            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Folder)
                .WithMany(f => f.ChatRooms)
                .HasForeignKey(cr => cr.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Налаштування для Folder
            modelBuilder.Entity<Folder>()
                .HasIndex(f => f.UserId);
            modelBuilder.Entity<Folder>()
                .Property(f => f.UserId)
                .IsRequired();

            // Конфігурація для ProcessedEvent
            modelBuilder.Entity<ProcessedEvent>()
                .HasKey(p => new { p.EventId, p.EventType });

            modelBuilder.Entity<ProcessedEvent>()
                .HasIndex(p => p.ProcessedAt);

            // Конфігурація для OutboxMessage
            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(o => o.ProcessedAt);

            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(o => o.CreatedAt);
        }
    }
}
