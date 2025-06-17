namespace ChatService.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
        Task<string> GetServiceToServiceTokenAsync();
        Task<string> GetTokenWithFallbackAsync();
    }
}
