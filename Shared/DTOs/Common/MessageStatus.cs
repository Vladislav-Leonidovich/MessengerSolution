namespace Shared.DTOs.Common
{
    public enum MessageStatus
    {
        Created = 0,      // Повідомлення створено
        Saved = 10,        // Збережено в БД
        Published = 20,    // Опубліковано через SignalR
        PartiallyDelivered = 30, // Доставлено частині отримувачів
        Delivered = 40,    // Доставлено отримувачам
        Failed = 100 // Помилка доставки
    }
}
