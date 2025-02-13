using EncryptionService.Helpers;

namespace EncryptionService.Services
{
    public class EncryptionService : IEncryptionService
    {
        // Шифрує переданий текст, викликаючи метод-обгортку з EncryptionHelper
        public string Encrypt(string plainText, string key)
        {
            return EncryptionHelper.EncryptString(plainText, key);
        }

        // Дешифрує зашифрований текст, викликаючи метод-обгортку з EncryptionHelper
        public string Decrypt(string cipherText, string key)
        {
            return EncryptionHelper.DecryptString(cipherText, key);
        }
    }
}
