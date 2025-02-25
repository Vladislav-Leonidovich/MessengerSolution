using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiClient.Models.Auth
{
    // Модель користувача, яку можна використовувати в UI та ViewModel
    public class UserDto
    {
        // Ідентифікатор користувача
        public int Id { get; set; }
        // Логін або ім'я користувача
        public string UserName { get; set; } = string.Empty;
        // Email користувача
        public string Email { get; set; } = string.Empty;
    }
}
