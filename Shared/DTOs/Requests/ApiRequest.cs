using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Requests
{
    public abstract class ApiRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? ClientInfo { get; set; }
        public string? CorrelationId { get; set; }
    }
}
