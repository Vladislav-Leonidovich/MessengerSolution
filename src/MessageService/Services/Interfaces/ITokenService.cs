namespace MessageService.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
    }
}
