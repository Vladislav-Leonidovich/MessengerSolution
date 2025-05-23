﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Authorization.Permissions
{
    public enum ChatPermission
    {
        // Дозволи для чатів
        CreateChat,
        ViewChat,
        DeleteChat,
        AddUserToChat,
        RemoveUserFromChat,
        ManageChatSettings,

        // Дозволи для папок
        ViewFolder,
        CreateFolder,
        UpdateFolder,
        DeleteFolder,
        AssignChatToFolder,
        UnassignChatToFolder
    }
}
