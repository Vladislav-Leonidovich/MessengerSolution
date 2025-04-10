using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Authorization
{
    public interface IPermissionService<TPermission> where TPermission : Enum
    {
        Task<bool> HasPermissionAsync(int userId, TPermission permission, int? resourceId = null);
        Task CheckPermissionAsync(int userId, TPermission permission, int? resourceId = null);
    }
}
