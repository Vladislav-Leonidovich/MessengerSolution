using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Message;

namespace MauiClient.ViewModels
{
    public class ChatRoomViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public MessageDto LastMessagePreview { get; set; } = new MessageDto();

        public string FormattedLastMessageDate
        {
            get
            {
                if (LastMessagePreview == null || LastMessagePreview.CreatedAt == default)
                    return string.Empty;

                var now = DateTime.Now;
                var date = LastMessagePreview.CreatedAt;

                if (date.Date == now.Date)
                {
                    return date.ToString("HH:mm");
                }
                else if (date.Year == now.Year)
                {
                    return date.ToString("dd.MM");
                }
                else
                {
                    return date.ToString("dd.MM.yyyy");
                }
            }
        }
    }
}
