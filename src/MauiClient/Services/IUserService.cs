using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Identity;

namespace MauiClient.Services
{
    public interface IUserService
    {
        Task<UserDto?> SearchUserByUsernameAsync(string username);
    }
}
