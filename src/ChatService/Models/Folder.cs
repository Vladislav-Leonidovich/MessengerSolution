namespace ChatService.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // Порядок відображення папок
        public int Order { get; set; }
        // Додаткове поле для зв’язку з користувачем
        public int UserId { get; set; }
        // Колекція чатів, що належать до папки
        public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();
    }
}
