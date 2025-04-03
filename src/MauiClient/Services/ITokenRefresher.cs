using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiClient.Services
{
    public interface ITokenRefresher
    {
        Task<bool> RefreshTokenAsync(string refresh);
    }
}
