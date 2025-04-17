using EncryptionService.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace EncryptionService.Services
{
    [Authorize]
    public class EncryptionGrpcService : Protos.EncryptionGrpcService.EncryptionGrpcServiceBase
    {
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<EncryptionGrpcService> _logger;

        public EncryptionGrpcService(IEncryptionService encryptionService, ILogger<EncryptionGrpcService> logger)
        {
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public override Task<EncryptResponse> Encrypt(EncryptRequest request, ServerCallContext context)
        {
            try
            {
                var cipherText = _encryptionService.Encrypt(request.PlainText);
                return Task.FromResult(new EncryptResponse
                {
                    CipherText = cipherText,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час шифрування повідомлення");
                return Task.FromResult(new EncryptResponse
                {
                    Success = false,
                    ErrorMessage = "Сталася помилка під час шифрування"
                });
            }
        }

        public override Task<DecryptResponse> Decrypt(DecryptRequest request, ServerCallContext context)
        {
            try
            {
                var plainText = _encryptionService.Decrypt(request.CipherText);
                return Task.FromResult(new DecryptResponse
                {
                    PlainText = plainText,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час дешифрування повідомлення");
                return Task.FromResult(new DecryptResponse
                {
                    Success = false,
                    ErrorMessage = "Сталася помилка під час дешифрування"
                });
            }
        }

        public override Task<BatchEncryptResponse> EncryptBatch(BatchEncryptRequest request, ServerCallContext context)
        {
            try
            {
                var response = new BatchEncryptResponse { Success = true };

                foreach (var plainText in request.PlainTexts)
                {
                    var cipherText = _encryptionService.Encrypt(plainText);
                    response.CipherTexts.Add(cipherText);
                }

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час пакетного шифрування повідомлень");
                return Task.FromResult(new BatchEncryptResponse
                {
                    Success = false,
                    ErrorMessage = "Сталася помилка під час пакетного шифрування"
                });
            }
        }

        public override Task<BatchDecryptResponse> DecryptBatch(BatchDecryptRequest request, ServerCallContext context)
        {
            try
            {
                var response = new BatchDecryptResponse { Success = true };

                foreach (var cipherText in request.CipherTexts)
                {
                    var plainText = _encryptionService.Decrypt(cipherText);
                    response.PlainTexts.Add(plainText);
                }

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час пакетного дешифрування повідомлень");
                return Task.FromResult(new BatchDecryptResponse
                {
                    Success = false,
                    ErrorMessage = "Сталася помилка під час пакетного дешифрування"
                });
            }
        }
    }
}
