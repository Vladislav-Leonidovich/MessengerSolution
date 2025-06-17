using ChatService.Data;
using ChatService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Outbox;

namespace ChatService.Controllers
{
    [Authorize(Roles = "Admin")] // Тільки для адміністраторів
    [ApiController]
    [Route("api/[controller]")]
    public class OutboxAdminController : ControllerBase
    {
        private readonly ChatDbContext _dbContext;
        private readonly ILogger<OutboxAdminController> _logger;

        public OutboxAdminController(
            ChatDbContext dbContext,
            ILogger<OutboxAdminController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: api/outboxadmin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = new
            {
                TotalMessages = await _dbContext.OutboxMessages.CountAsync(),
                PendingMessages = await _dbContext.OutboxMessages.CountAsync(m => m.Status == OutboxMessageStatus.Pending),
                ProcessingMessages = await _dbContext.OutboxMessages.CountAsync(m => m.Status == OutboxMessageStatus.Processing),
                ProcessedMessages = await _dbContext.OutboxMessages.CountAsync(m => m.Status == OutboxMessageStatus.Processed),
                FailedMessages = await _dbContext.OutboxMessages.CountAsync(m => m.Status == OutboxMessageStatus.Failed),
                CancelledMessages = await _dbContext.OutboxMessages.CountAsync(m => m.Status == OutboxMessageStatus.Cancelled)
            };

            return Ok(stats);
        }

        // GET: api/outboxadmin/failed
        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedMessages()
        {
            var failedMessages = await _dbContext.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Failed)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.EventType,
                    m.CreatedAt,
                    m.RetryCount,
                    m.Error
                })
                .ToListAsync();

            return Ok(failedMessages);
        }

        // POST: api/outboxadmin/retry/{id}
        [HttpPost("retry/{id}")]
        public async Task<IActionResult> RetryMessage(Guid id)
        {
            var message = await _dbContext.OutboxMessages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            if (message.Status != OutboxMessageStatus.Failed)
            {
                return BadRequest("Можна повторити спробу тільки для повідомлень зі статусом 'Failed'");
            }

            message.Status = OutboxMessageStatus.Pending;
            message.RetryCount = 0;
            message.NextRetryAt = null;
            message.Error = $"Повторна спроба вручну: {message.Error}";

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Повідомлення Outbox {MessageId} відправлено на повторну обробку", id);

            return Ok(new { message = "Повідомлення відправлено на повторну обробку" });
        }

        // POST: api/outboxadmin/cancel/{id}
        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelMessage(Guid id)
        {
            var message = await _dbContext.OutboxMessages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            if (message.Status == OutboxMessageStatus.Processed)
            {
                return BadRequest("Не можна скасувати вже оброблене повідомлення");
            }

            message.Status = OutboxMessageStatus.Cancelled;
            message.ProcessedAt = DateTime.UtcNow;
            message.Error = $"Скасовано вручну: {message.Error}";

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Повідомлення Outbox {MessageId} скасовано", id);

            return Ok(new { message = "Повідомлення скасовано" });
        }

        // POST: api/outboxadmin/retryall
        [HttpPost("retryall")]
        public async Task<IActionResult> RetryAllFailedMessages()
        {
            var failedMessages = await _dbContext.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Failed)
                .ToListAsync();

            foreach (var message in failedMessages)
            {
                message.Status = OutboxMessageStatus.Pending;
                message.RetryCount = 0;
                message.NextRetryAt = null;
                message.Error = $"Масова повторна обробка: {message.Error}";
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Відправлено на повторну обробку {Count} повідомлень", failedMessages.Count);

            return Ok(new { message = $"Відправлено на повторну обробку {failedMessages.Count} повідомлень" });
        }
    }
}
