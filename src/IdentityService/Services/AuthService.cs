using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IdentityDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(IdentityDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username || u.Email == email);
        }

        public async Task<User> RegisterAsync(RegisterModel model)
        {
            if (await UserExistsAsync(model.UserName, model.Email))
            {
                throw new Exception("Користувач із таким ім'ям або email вже існує");
            }

            // Генерація хеша пароля
            CreatePasswordHash(model.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<AuthResponse?> LoginAsync(LoginModel model)
        {
            // Пошук користувача по имені
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);
            if (user == null)
                return null;

            // Перевірка пароля
            if (!VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
                return null;

            // Створення JWT токена
            var token = CreateToken(user);
            return token;
        }

        // Допоміжний метод для створення хеша пароля
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key; // Випадковий ключ генерується автоматично
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        // Перевірка введеного пароля зі збереженим хешем
        private bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            using (var hmac = new HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(storedHash);
            }
        }

        // Створення JWT токена для аутентифікованого користувача
        private AuthResponse CreateToken(User user)
        {
            var jwtSecretKey = _configuration["Jwt:SecretKey"] ?? "SuperSecretKey12345";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // Визначаємо клейми токена
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
                // Можна додати додаткові клейма (наприклад, роль)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AuthResponse
            {
                Token = tokenHandler.WriteToken(token),
                ExpiresAt = tokenDescriptor.Expires.Value
            };
        }
    }
}
