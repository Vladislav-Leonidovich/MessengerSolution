using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared.DTOs.Encryption;
using EncryptionService.Services.Interfaces;

namespace EncryptionService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EncryptionController : ControllerBase
    {
        private readonly IEncryptionService _encryptionService;
        public EncryptionController(IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        /* POST: api/encryption/encrypt
        Приймає об'єкт EncryptionRequest і повертає зашифрований текст */
        [HttpPost("encrypt")]
        public IActionResult Encrypt([FromBody] EncryptionRequest request)
        {
            try
            {
                var cipherText = _encryptionService.Encrypt(request.PlainText);
                return Ok(new { CipherText = cipherText });
            }
            catch (Exception ex)
            {
                // В случае ошибки возвращаем подробное сообщение (в продакшене лучше скрывать детали)
                return BadRequest(new { Message = ex.Message });
            }
        }

        /* POST: api/encryption/decrypt
        Приймає об'єкт DecryptionRequest і повертає вихідний (розшифрований) текст */
        [HttpPost("decrypt")]
        public IActionResult Decrypt([FromBody] DecryptionRequest request)
        {
            try
            {
                var plainText = _encryptionService.Decrypt(request.CipherText);
                return Ok(new { PlainText = plainText });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
