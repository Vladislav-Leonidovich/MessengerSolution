namespace BlazorClient.Services
{
    // Інтерфейс для сервісу автентифікації
    public interface IAuthService
    {
        // Метод для реєстрації користувача
        Task<bool> RegisterAsync(RegisterModel model);

        // Метод для входу користувача, який повертає токен
        Task<string?> LoginAsync(LoginModel model);
    }

    // DTO для реєстрації
    public class RegisterModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // DTO для логіну
    public class LoginModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
