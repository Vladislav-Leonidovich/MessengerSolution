using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Exceptions
{
    public class DatabaseException : AppException
    {
        public DatabaseException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
