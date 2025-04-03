using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts
{
    public class ChatDeletedEvent
    {
        public int ChatRoomId { get; set; }
    }
}
