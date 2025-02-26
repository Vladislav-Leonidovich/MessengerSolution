namespace EncryptionServiceDTOs
{
    public class DecryptionRequest
    {
        // Зашифрований текст (у форматі Base64)
        public string CipherText { get; set; } = null!;
    }
}
