using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Shared.IdentityServiceDTOs;

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

        public async Task<User> RegisterAsync(RegisterDto model)
        {
            if (await UserExistsAsync(model.UserName, model.Email))
            {
                throw new Exception("Користувач із таким ім'ям або email вже існує");
            }

            // Автоматически добавляем "@" если отсутствует
            var userName = model.UserName.StartsWith("@") ? model.UserName : "@" + model.UserName;

            // Генерація хеша пароля
            CreatePasswordHash(model.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                UserName = userName,
                Email = model.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                DisplayName = model.UserName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<AuthDto?> LoginAsync(LoginDto model)
        {
            // Пошук користувача по имені
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);
            if (user == null)
                return null;

            // Перевірка пароля
            if (!VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
                return null;

            // Створення JWT токена
            var token = await CreateToken(user);
            return token;
        }

        public async Task<AuthDto?> RefreshTokenAsync(string currentRefreshToken)
        {
            // Знайдемо запис refresh токена в базі даних
            var refreshTokenEntry = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(rt => rt.RefreshToken == currentRefreshToken);

            if (refreshTokenEntry == null || refreshTokenEntry.IsExpired)
            {
                // Якщо токен не знайдено або він прострочений, повертаємо null або кидаємо помилку
                return null;
            }

            // Знайдемо користувача
            var user = await _context.Users.FindAsync(refreshTokenEntry.UserId);
            if (user == null)
            {
                return null;
            }

            // Генеруємо новий JWT access token (реалізація аналогічна вашому методу CreateToken)
            var newAccessToken = await CreateToken(user); // Цей метод повертає AuthDto

            // За потреби можна згенерувати новий refresh token:
            var newRefreshToken = GenerateRefreshToken();

            // Оновлюємо refresh токен у базі даних (можна видалити старий, або замінити його)
            refreshTokenEntry.RefreshToken = newRefreshToken;
            // Також оновлюємо дату закінчення терміну, наприклад:
            refreshTokenEntry.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            // Повертаємо нові токени
            return new AuthDto
            {
                Token = newAccessToken.Token, // access token
                TokenExpiresAt = newAccessToken.TokenExpiresAt,
                RefreshToken = refreshTokenEntry.RefreshToken,
                RefreshTokenExpiresAt = refreshTokenEntry.ExpiresAt
            };
        }

        private string GenerateRefreshToken()
        {
            // Простий приклад генерації refresh токена:
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
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

        private async Task<UserRefreshToken> CreateRefreshToken(User user)
        {
            if (user == null)
            {
                return new UserRefreshToken();
            }

            var existingToken = await _context.UserRefreshTokens
        .FirstOrDefaultAsync(r => r.UserId == user.Id);


            var newRefreshToken = GenerateRefreshToken();
            var newExpiration = DateTime.UtcNow.AddDays(7);
            if (existingToken != null)
            {
                _context.UserRefreshTokens.Remove(existingToken);
            }

            existingToken = new UserRefreshToken
            {
                UserId = user.Id,
                RefreshToken = newRefreshToken,
                ExpiresAt = newExpiration
            };
            await _context.UserRefreshTokens.AddAsync(existingToken);

            await _context.SaveChangesAsync();
            return existingToken; ;
        }

        // Створення JWT токена для аутентифікованого користувача
        private async Task<AuthDto> CreateToken(User user)
        {
            var jwtSecretKey = _configuration["JWT_SECRET_KEY"];
            if (string.IsNullOrEmpty(jwtSecretKey))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured");
            }
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
            var refreshToken = await CreateRefreshToken(user);

            return new AuthDto
            {
                Token = tokenHandler.WriteToken(token),
                TokenExpiresAt = tokenDescriptor.Expires.Value,
                RefreshToken = refreshToken.RefreshToken,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }
    }
}
