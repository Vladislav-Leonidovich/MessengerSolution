using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiClient.Models.Folder;

namespace MauiClient.Services
{
    public interface IFolderService
    {
        Task<IEnumerable<FolderDto>> GetFoldersAsync(int userId);
    }
}
