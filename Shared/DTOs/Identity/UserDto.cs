using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Identity
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression("^@.*", ErrorMessage = "UserName must start with '@'")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "UserName має бути від 3 до 50 символів")]
        public string UserName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
    }
}
