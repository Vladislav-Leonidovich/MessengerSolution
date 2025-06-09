using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Message;

namespace Shared.Exceptions
{
    public class ConflictException : AppException
    {

        public ConflictException(string message)
            : base(message)
        {
        }
    }
}
