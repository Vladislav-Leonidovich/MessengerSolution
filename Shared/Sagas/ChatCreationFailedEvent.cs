using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Sagas
{
    public class ChatCreationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
