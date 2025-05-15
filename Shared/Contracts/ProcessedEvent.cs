namespace Shared.Contracts
{
    public class ProcessedEvent
    {
        public Guid EventId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string EventData { get; set; } = string.Empty;
    }
}
