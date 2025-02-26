using EncryptionService.Helpers;

namespace EncryptionService.Services
{
    public class EncryptionService : IEncryptionService
    {

        private readonly IConfiguration _configuration;
        public EncryptionService(IConfiguration configuration) 
        {
            _configuration = configuration;
        }
        // Шифрує переданий текст, викликаючи метод-обгортку з EncryptionHelper
        public string Encrypt(string plainText)
        {
            var key = _configuration["Encryption:Key"];
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Encryption key is not configured");
            }
            return EncryptionHelper.EncryptString(plainText, key);
        }

        // Дешифрує зашифрований текст, викликаючи метод-обгортку з EncryptionHelper
        public string Decrypt(string cipherText)
        {
            var key = _configuration["Encryption:Key"];
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Encryption key is not configured");
            }
            return EncryptionHelper.DecryptString(cipherText, key);
        }
    }
}
