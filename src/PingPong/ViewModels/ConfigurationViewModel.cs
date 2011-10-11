using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Caliburn.Micro;

namespace PingPong.ViewModels
{
    public class ConfigurationViewModel : Screen
    {
        private KeyValuePair<string, Style> _selectedTheme;

        public AppInfo AppInfo { get; private set; }

        public ObservableCollection<KeyValuePair<string, Style>> Themes { get; private set; }

        public KeyValuePair<string, Style> SelectedTheme
        {
            get { return _selectedTheme; }
            set
            {
                this.SetValue("SelectedTheme", value, ref _selectedTheme);
                AppInfo.StatusStyle = value.Value;
            }
        }

        public ConfigurationViewModel(AppInfo appInfo)
        {
            AppInfo = appInfo;
            Themes = new ObservableCollection<KeyValuePair<string, Style>>
            {
                new KeyValuePair<string, Style>("Metro", (Style)Application.Current.Resources["MetroTweetsPanel"]),
                new KeyValuePair<string, Style>("Dark", (Style)Application.Current.Resources["DarkTweetsPanel"]),
            };
            SelectedTheme = Themes.FirstOrDefault(x => x.Value == AppInfo.StatusStyle);
        }
    }
}