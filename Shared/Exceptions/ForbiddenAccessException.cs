using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Exceptions
{
    public class ForbiddenAccessException : AppException
    {
        public ForbiddenAccessException(string message = "У вас немає прав для цієї операції.")
            : base(message) { }
    }
}
