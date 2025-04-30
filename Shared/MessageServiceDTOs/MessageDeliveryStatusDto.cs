using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.MessageServiceDTOs
{
    public class MessageDeliveryStatusDto
    {
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public int DeliveredCount { get; set; }
        public int TotalRecipientsCount { get; set; }
        public bool IsDeliveredToAll { get; set; }
        public List<int> DeliveredToUserIds { get; set; } = new List<int>();
        public DateTime LastCheckedAt { get; set; }
    }
}
