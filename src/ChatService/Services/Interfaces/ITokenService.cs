namespace ChatService.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
    }
}
