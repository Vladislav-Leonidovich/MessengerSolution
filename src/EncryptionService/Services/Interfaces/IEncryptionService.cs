namespace EncryptionService.Services.Interfaces
{
    public interface IEncryptionService
    {
        // Зашифрувати вихідний текст із використанням переданого ключа
        string Encrypt(string plainText);

        // Дешифрувати зашифрований текст з використанням переданого ключа
        string Decrypt(string cipherText);
    }
}
