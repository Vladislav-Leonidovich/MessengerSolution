namespace EncryptionService.DTOs
{
    public class DecryptionRequest
    {
        // Зашифрований текст (у форматі Base64)
        public string CipherText { get; set; } = null!;

        // Ключ, за допомогою якого проводилося шифрування
        public string Key { get; set; } = null!;
    }
}
