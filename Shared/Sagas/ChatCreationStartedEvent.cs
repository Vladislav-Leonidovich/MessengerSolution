using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Sagas
{
    public class ChatCreationStartedEvent
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public int ChatRoomId { get; set; }
        public int CreatorUserId { get; set; }
        public List<int> MemberIds { get; set; } = new List<int>();
    }
}
