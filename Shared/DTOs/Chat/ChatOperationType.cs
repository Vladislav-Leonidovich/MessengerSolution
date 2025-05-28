using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public enum ChatOperationType
    {
        Create,
        Update,
        Delete,
        AddMember,
        RemoveMember,
        ChangeOwner,
        UpdateSettings,
        Archive,
        Restore
    }
}
