using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Outbox
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventData { get; set; } = string.Empty;
        public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? NextRetryAt { get; set; } = null;
        public DateTime? ProcessedAt { get; set; } = null;
        public int RetryCount { get; set; } = 0;
        public string? Error { get; set; } = string.Empty;
    }
}
