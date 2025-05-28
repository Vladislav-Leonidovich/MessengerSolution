namespace Shared.DTOs.Encryption
{
    // Модель запиту на шифрування
    public class EncryptionRequest
    {
        // Вихідний текст, який потрібно зашифрувати
        public string PlainText { get; set; } = null!;
    }
}
