using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiClient.Services
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Resources;
    public class LocalizationResourceManager : INotifyPropertyChanged
    {
        // Сінглтон (опційно, ви можете реєструвати його через DI як singleton)
        private static LocalizationResourceManager? _instance;
        public static LocalizationResourceManager Instance => _instance ??= new LocalizationResourceManager();

        // Ресурс-менеджер для файлу Strings.resx (переконайтеся, що namespace і базова назва відповідають)
        public ResourceManager ResourceManager { get; } = new ResourceManager("MauiClient.Resources.Localization.Strings", typeof(LocalizationResourceManager).Assembly);

        private CultureInfo _culture = CultureInfo.CurrentUICulture;
        public CultureInfo Culture
        {
            get => _culture;
            set
            {
                if (_culture != value)
                {
                    _culture = value;
                    OnPropertyChanged(nameof(Culture));
                    // Повідомляємо, що всі рядки могли змінитися
                    OnPropertyChanged(string.Empty);
                }
            }
        }

        // Індексатор для зручного отримання рядків за ключем
        public string this[string key] => ResourceManager.GetString(key, Culture) ?? $"[{key}]";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Метод для зміни культури (наприклад, при виборі мови користувачем)
        public void SetCulture(CultureInfo culture)
        {
            Culture = culture;
        }
    }

}
