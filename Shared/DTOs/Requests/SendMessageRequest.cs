using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Requests
{
    public class SendMessageRequest : ApiRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
