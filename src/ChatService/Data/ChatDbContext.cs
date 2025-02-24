using Microsoft.EntityFrameworkCore;
using ChatService.Models;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування TPH для ChatRoom із дискрімінацією
            modelBuilder.Entity<ChatRoom>()
                .HasDiscriminator<string>("ChatRoomType")
                .HasValue<PrivateChatRoom>("Private")
                .HasValue<GroupChatRoom>("Group");

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

            // Зв'язок між ChatRoom та Folder, як раніше
            modelBuilder.Entity<ChatRoom>()
                .HasOne<Folder>()
                .WithMany(f => f.ChatRooms)
                .HasForeignKey("FolderId")
                .OnDelete(DeleteBehavior.SetNull);

            // Налаштування папок
            modelBuilder.Entity<Folder>()
                .HasIndex(f => f.UserId);
            modelBuilder.Entity<Folder>()
                .Property(f => f.UserId)
                .IsRequired();
        }
    }
}
