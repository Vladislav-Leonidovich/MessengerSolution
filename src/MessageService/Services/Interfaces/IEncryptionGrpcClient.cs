namespace MessageService.Services.Interfaces
{
    public interface IEncryptionGrpcClient
    {
        Task<string> EncryptAsync(string plainText);
        Task<string> DecryptAsync(string cipherText);
        Task<List<string>> EncryptBatchAsync(List<string> plainTexts);
        Task<List<string>> DecryptBatchAsync(List<string> cipherTexts);
    }
}
