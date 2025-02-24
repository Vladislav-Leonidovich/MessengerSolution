using Microsoft.EntityFrameworkCore;
using ChatService.Models;

namespace ChatService.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
        }

        // Таблиця чатів
        public DbSet<ChatRoom> ChatRooms { get; set; }
        // Таблиця зв'язків користувач-чат (для багатьох-до-багатьох)
        public DbSet<UserChatRoom> UserChatRooms { get; set; }
        // Таблиця папок
        public DbSet<Folder> Folders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування складового ключа для таблиці UserChatRoom
            modelBuilder.Entity<UserChatRoom>()
                .HasKey(uc => new { uc.ChatRoomId, uc.UserId });

            // Визначення зв'язку: один чат має багато записів UserChatRoom
            modelBuilder.Entity<ChatRoom>()
                .HasMany(cr => cr.UserChatRooms)
                .WithOne(uc => uc.ChatRoom)
                .HasForeignKey(uc => uc.ChatRoomId);
            // Налаштування зв'язку між ChatRoom і Folder.
            // Один чат може належати до однієї папки (FolderId може бути null, якщо чат не прив'язаний до папки),
            // а одна папка може містити багато чатів.
            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Folder)
                .WithMany(f => f.ChatRooms)
                .HasForeignKey(cr => cr.FolderId)
                .OnDelete(DeleteBehavior.SetNull); // При видаленні папки, чат не видаляється, а FolderId стає null.
            // Додатково: індексування за UserId для папок
            modelBuilder.Entity<Folder>()
                .HasIndex(f => f.UserId);
            // Забезпечуємо, що UserId обов'язкове:
            modelBuilder.Entity<Folder>()
                .Property(f => f.UserId)
                .IsRequired();
        }
    }
}
