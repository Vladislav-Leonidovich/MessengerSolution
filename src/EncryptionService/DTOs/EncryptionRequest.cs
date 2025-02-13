namespace EncryptionService.DTOs
{
    // Модель запиту на шифрування
    public class EncryptionRequest
    {
        // Вихідний текст, який потрібно зашифрувати
        public string PlainText { get; set; } = null!;

        // Ключ шифрування (передається клієнтом, або можна використовувати значення за замовчуванням)
        public string Key { get; set; } = null!;
    }
}
