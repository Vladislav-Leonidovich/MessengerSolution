using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Authorization.Permissions
{
    public enum MessagePermission
    {
        SendMessage,
        EditMessage,
        DeleteMessage,
        ViewMessage,
        ReactToMessage,
        ReplayMessage,
        ReportMessage,
        PinMessage,
        UnpinMessage,
        CleanChat
    }
}
