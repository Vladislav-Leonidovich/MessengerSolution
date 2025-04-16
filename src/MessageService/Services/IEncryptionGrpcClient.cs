namespace MessageService.Services
{
    public interface IEncryptionGrpcClient : IDisposable
    {
        Task<string> EncryptAsync(string plainText);
        Task<string> DecryptAsync(string cipherText);
        Task<List<string>> EncryptBatchAsync(List<string> plainTexts);
        Task<List<string>> DecryptBatchAsync(List<string> cipherTexts);
    }
}
