using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Exceptions
{
    public class EntityNotFoundException : AppException
    {
        public string EntityName { get; }
        public object EntityId { get; }

        public EntityNotFoundException(string entityName, object entityId)
            : base($"Сутність {entityName} з ідентифікатором {entityId} не знайдена.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
