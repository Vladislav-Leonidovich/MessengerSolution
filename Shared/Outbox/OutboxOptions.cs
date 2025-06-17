using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Outbox
{
    public class OutboxOptions
    {
        public int BatchSize { get; set; } = 50;
        public int MaxRetryCount { get; set; } = 5;
        public TimeSpan[] RetryDelays { get; set; } = new[]
        {
        TimeSpan.FromSeconds(10),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1)
    };
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    }
}
