using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiClient.Models.Chat
{
    public class CreateChatRoomDto
    {
        public string Name { get; set; } = string.Empty;
        public List<int> UserIds { get; set; } = new List<int>();
    }
}
