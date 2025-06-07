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
        public object? EntityId { get; }

        public EntityNotFoundException(string entityName)
            : base($"Сутність {entityName} не знайдена.")
        {
            EntityName = entityName;
        }
        public EntityNotFoundException(string entityName, object entityId)
            : base($"Сутність {entityName} з ідентифікатором {entityId} не знайдена.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }
        public EntityNotFoundException(string entityName, object entityId, Exception innerException)
            : base($"Сутність {entityName} з ідентифікатором {entityId} не знайдена.", innerException)
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
