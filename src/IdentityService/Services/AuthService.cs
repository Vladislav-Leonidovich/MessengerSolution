using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Shared.DTOs.Identity;
using IdentityService.Services.Interfaces;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IdentityDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IdentityDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
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

        public async Task<AuthDto?> LoginAsync(LoginDto model, string ipAddress)
        {
            // Пошук користувача по имені
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);
            if (user == null)
                return null;

            // Перевірка пароля
            if (!VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
                return null;

            // Створення JWT токена
            var token = await CreateToken(user, model, ipAddress);
            return token;
        }

        public async Task<AuthDto?> RefreshTokenAsync(string currentRefreshToken, string ipAddress)
        {
            // Найдем запись refresh токена в базе данных
            var refreshTokenEntry = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(rt => rt.RefreshToken == currentRefreshToken);

            if (refreshTokenEntry == null || refreshTokenEntry.IsExpired)
            {
                // Если токен не найден или он просрочен, возвращаем null или кидаем ошибку
                return null;
            }

            // Найдем пользователя
            var user = await _context.Users.FindAsync(refreshTokenEntry.UserId);
            if (user == null)
            {
                return null;
            }

            // Создаем LoginDto с информацией об устройстве из refreshTokenEntry
            var loginInfo = new LoginDto
            {
                UserName = user.UserName, // Требуется для создания JWT токена
                Password = "", // Пустой пароль, так как при обновлении токена мы не проверяем пароль
                DeviceName = refreshTokenEntry.DeviceName,
                DeviceType = refreshTokenEntry.DeviceType,
                OperatingSystem = refreshTokenEntry.OperatingSystem,
                OsVersion = refreshTokenEntry.OsVersion
            };

            // Генерируем новый JWT access token
            var newAccessToken = await CreateToken(user, loginInfo, ipAddress);

            // Обновляем данные о последнем входе и IP-адресе
            refreshTokenEntry.LastLogin = DateTime.UtcNow;
            refreshTokenEntry.IpAddress = ipAddress;

            // За потреби можно згенерувати новий refresh token:
            var newRefreshToken = GenerateRefreshToken();
            refreshTokenEntry.RefreshToken = newRefreshToken;
            refreshTokenEntry.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            // Возвращаем новые токены
            return new AuthDto
            {
                Token = newAccessToken.Token,
                TokenExpiresAt = newAccessToken.TokenExpiresAt,
                RefreshToken = refreshTokenEntry.RefreshToken,
                RefreshTokenExpiresAt = refreshTokenEntry.ExpiresAt
            };
        }

        public async Task<string> GenerateServiceTokenAsync(string serviceName)
        {
            // Створюємо особливі клейми для сервісу
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, serviceName),
            new Claim(ClaimTypes.Role, "Service"), // Спеціальна роль для сервісів
            new Claim("service_name", serviceName)
        };

            // Можна додати більше клеймів для різних сервісів
            if (serviceName == "MessageService")
            {
                claims.Add(new Claim("permission", "message_service_all"));
            }
            else if (serviceName == "ChatService")
            {
                claims.Add(new Claim("permission", "chat_service_all"));
            }
            else if (serviceName == "EncryptionService")
            {
                claims.Add(new Claim("permission", "encryption_service_all"));
            }
            else if (serviceName == "IdentityService")
            {
                claims.Add(new Claim("permission", "identity_service_all"));
            }

            // Генеруємо JWT з довшим часом життя для сервісів
            var token = await GenerateJwtTokenAsync(claims, TimeSpan.FromHours(12));
            return token;
        }

        private async Task<string> GenerateJwtTokenAsync(IEnumerable<Claim> claims, TimeSpan expiration)
        {
            try
            {
                // Отримуємо секретний ключ з конфігурації
                var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ??
                                  throw new InvalidOperationException("JWT Secret Key не налаштовано");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Час видачі та закінчення
                var now = DateTime.UtcNow;
                var expires = now.Add(expiration);

                // Створюємо JWT токен
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    IssuedAt = now,
                    Expires = expires,
                    SigningCredentials = credentials,
                    // Можна також додати issuer та audience, якщо потрібно
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"]
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogDebug("Згенеровано JWT токен з терміном дії до {ExpiryTime}", expires);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при генерації JWT токена");
                throw new ApplicationException("Не вдалося створити JWT токен", ex);
            }
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

        private async Task<UserRefreshToken> CreateRefreshToken(User user, LoginDto loginInfo, string ipAddress)
        {
            if (user == null)
            {
                return new UserRefreshToken();
            }

            // Генерируем новый токен
            var newRefreshToken = GenerateRefreshToken();
            var newExpiration = DateTime.UtcNow.AddDays(7);

            // Ищем существующий токен для этого устройства
            var deviceIdentifier = $"{loginInfo.DeviceName}_{loginInfo.DeviceType}_{loginInfo.OperatingSystem}";
            var existingToken = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(r => r.UserId == user.Id &&
                                         (r.DeviceName == loginInfo.DeviceName &&
                                          r.DeviceType == loginInfo.DeviceType &&
                                          r.OperatingSystem == loginInfo.OperatingSystem));

            if (existingToken != null)
            {
                existingToken.RefreshToken = newRefreshToken;
                existingToken.ExpiresAt = newExpiration;
                existingToken.LastLogin = DateTime.UtcNow;
                existingToken.IpAddress = ipAddress;
                existingToken.IsActive = true;
                existingToken.OsVersion = loginInfo.OsVersion;
            }
            else
            {
                existingToken = new UserRefreshToken
                {
                    UserId = user.Id,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = newExpiration,
                    DeviceName = loginInfo.DeviceName,
                    DeviceType = loginInfo.DeviceType,
                    OperatingSystem = loginInfo.OperatingSystem,
                    OsVersion = loginInfo.OsVersion,
                    IpAddress = ipAddress,
                    LastLogin = DateTime.UtcNow,
                    IsActive = true
                };
                await _context.UserRefreshTokens.AddAsync(existingToken);
            }

            await _context.SaveChangesAsync();
            return existingToken;
        }

        // Створення JWT токена для аутентифікованого користувача
        private async Task<AuthDto> CreateToken(User user, LoginDto loginInfo, string ipAddress)
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
            var refreshToken = await CreateRefreshToken(user, loginInfo, ipAddress);

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
