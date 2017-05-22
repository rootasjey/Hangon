using Hangon.Services;
using System;
using Windows.ApplicationModel.Email;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Hangon.Views {
    public sealed partial class SettingsPage : Page {
        public SettingsPage() {
            InitializeComponent();
            LoadData();
        }

        private void LoadData() {
            WallSwitch.IsOn = BackgroundTask.IsWallTaskActive();
            GetLastUpdatedTask();
            GetLastError();
        }

        private void GetLastUpdatedTask() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("wallstats")) {
                ApplicationDataCompositeValue stats = (ApplicationDataCompositeValue)localSettings.Values["wallstats"];

                if (stats["date"] == null) {
                    return;
                }

                LastUpdatedTask.Text = "Task last run on: " + stats["date"];
            }
        }

        private void GetLastError() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("wallerror")) {
                ApplicationDataCompositeValue stats = (ApplicationDataCompositeValue)localSettings.Values["wallerror"];

                if (stats["date"] == null) {
                    return;
                }

                LastError.Text = "Last error on: " + stats["date"] + " " + " - due to: " + stats["error"];
            }
        }

        /// <summary>
        /// Add or remove background task when the toggle changes state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WallSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;
            if (toggle.IsOn) {
                ShowWallTaskConfig();

                if (BackgroundTask.IsWallTaskActive()) {
                    return;
                }
                
                BackgroundTask.RegisterWallTask(GetTimeWallInterval());

            } else {
                HideWallTaskConfig();
                BackgroundTask.UnregisterWallTask();
            }
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e) {
            EmailMessage email = new EmailMessage() {
                Subject = "[splashpaper] Feedback",
                Body = "send this email to metrodevapp@outlook.com"
            };
            // TODO : add app infos
            EmailManager.ShowComposeNewEmailAsync(email);
        }

        private async void NoteButton_Click(object sender, RoutedEventArgs e) {
            string appID = "9wzdncrcwfqr";
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=" + appID));
        }

        private void ShowWallTaskConfig() {
            ComboboxTimeWall.Visibility = Visibility.Visible;
            WallTaskInfos.Visibility = Visibility.Visible;
        }

        private void HideWallTaskConfig() {
            ComboboxTimeWall.Visibility = Visibility.Collapsed;
            WallTaskInfos.Visibility = Visibility.Collapsed;
        }

        private void ComboboxTimeWall_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!WallSwitch.IsOn) {
                return;
            }

            BackgroundTask.UnregisterWallTask();
            BackgroundTask.RegisterWallTask(GetTimeWallInterval());
        }

        private uint GetTimeWallInterval() {
            var item = (ComboBoxItem)ComboboxTimeWall.SelectedItem;
            string value = (string)item.Content;
            return uint.Parse(value);
        }

    }
}
