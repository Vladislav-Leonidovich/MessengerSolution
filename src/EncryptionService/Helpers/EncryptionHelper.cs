using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace EncryptionService.Helpers
{
    public class EncryptionHelper
    {
        /*Шифрує переданий текст з використанням алгоритму AES.
        Під час шифрування генерується випадковий вектор ініціалізації (IV), який записується на початок результату.*/
        public static string EncryptString(string plainText, string key)
        {
            // Приводимо ключ до потрібної довжини за допомогою хешування (SHA256 дає 32 байти, що підходить для AES-256)
            byte[] keyBytes = GetKeyBytes(key);

            using Aes aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using MemoryStream ms = new MemoryStream();
            // Спочатку записуємо IV у потік, щоб потім використовувати його під час дешифрування
            ms.Write(iv, 0, iv.Length);

            // Створюємо шифратор і записуємо зашифровані дані в потік
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter writer = new StreamWriter(cs))
            {
                writer.Write(plainText);
            }
            // Перетворюємо результат у Base64 рядок для передавання мережею
            return Convert.ToBase64String(ms.ToArray());
        }

        /*Дешифрує зашифрований текст(Base64) з використанням алгоритму AES.
        Витягується IV, записаний на початку шифротексту.*/
        public static string DecryptString(string cipherText, string key)
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            byte[] keyBytes = GetKeyBytes(key);

            using Aes aes = Aes.Create();
            aes.Key = keyBytes;

            // Витягуємо вектор ініціалізації (IV) з початку зашифрованого масиву
            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Використовуємо решту даних як зашифроване повідомлення
            using MemoryStream ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader reader = new StreamReader(cs);
            return reader.ReadToEnd();
        }

        // Генерує 256-бітний ключ на основі переданого рядка
        private static byte[] GetKeyBytes(string key)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
}
