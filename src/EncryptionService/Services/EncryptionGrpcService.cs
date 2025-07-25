﻿using Shared.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using EncryptionService.Services.Interfaces;

namespace EncryptionService.Services
{
    [Authorize]
    public class EncryptionGrpcService : Shared.Protos.EncryptionGrpcService.EncryptionGrpcServiceBase
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

        public override async Task<BatchEncryptResponse> EncryptBatch(BatchEncryptRequest request, ServerCallContext context)
        {
            try
            {
                var response = new BatchEncryptResponse { Success = true };

                var encryptionTasks = request.PlainTexts.Select(plainText =>
                Task.Run(() => _encryptionService.Encrypt(plainText))).ToArray();

                // Очікуємо завершення всіх завдань
                var results = await Task.WhenAll(encryptionTasks);

                // Додаємо результати до відповіді
                response.CipherTexts.AddRange(results);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час пакетного шифрування повідомлень");
                return await Task.FromResult(new BatchEncryptResponse
                {
                    Success = false,
                    ErrorMessage = "Сталася помилка під час пакетного шифрування"
                });
            }
        }

        public override async Task<BatchDecryptResponse> DecryptBatch(BatchDecryptRequest request, ServerCallContext context)
        {
            try
            {
                var response = new BatchDecryptResponse { Success = true };

                var decryptionTasks = request.CipherTexts.Select(cipherText =>
                Task.Run(() => _encryptionService.Decrypt(cipherText))).ToArray();

                var results = await Task.WhenAll(decryptionTasks);

                response.PlainTexts.AddRange(results);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час пакетного дешифрування повідомлень");
                return await Task.FromResult(new BatchDecryptResponse
                {
                    Success = false,
                    ErrorMessage = "Сталася помилка під час пакетного дешифрування"
                });
            }
        }
    }
}
