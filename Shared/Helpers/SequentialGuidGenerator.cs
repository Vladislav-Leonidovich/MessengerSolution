using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Shared.Helpers
{
    public class SequentialGuidGenerator : ValueGenerator<Guid>
    {
        public override bool GeneratesTemporaryValues => false;

        public override Guid Next(EntityEntry entry)
        {
            return SequentialGuid.Create();
        }
    }

    public static class SequentialGuid
    {
        public static Guid Create() => RT.Comb.Provider.Sql.Create();
    }
}
