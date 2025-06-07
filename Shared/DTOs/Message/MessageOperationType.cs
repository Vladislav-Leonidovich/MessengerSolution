using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Message
{
    public enum MessageOperationType
    {
        SendMessage = 0,
        EditMessage = 1,
        DeleteMessage = 2,
        DeleteAllMessages = 3,
        ForwardMessage = 4,
        EncryptMessage = 5,
        DecryptMessage = 6,
        BulkProcessing = 7
    }
}
