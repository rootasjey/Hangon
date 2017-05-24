using Hangon.Services;
using System;
using Windows.ApplicationModel.Email;
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
            LockscreenSwitch.IsOn = BackgroundTask.IsLockscreenTaskActive();
            UpdateWallTaskActivityText();
            UpdateLockscreenTaskActivityText();
        }

        private void UpdateWallTaskActivityText() {
            var activity = BackgroundTask.GetWallTaskActivity();
            if (activity == null) return;
            LastUpdatedTask.Text = "Wallpaper task last run on: " + activity["DateTime"];

            if (activity["Exception"] != null) {
                LastWallTaskError.Text = activity["Exception"].ToString();
            }
        }

        private void UpdateLockscreenTaskActivityText() {
            var activity = BackgroundTask.GetLockscreenTaskActivity();
            if (activity == null) return;
            LastUpdatedLockscreenTask.Text = "Wallpaper task last run on: " + activity["DateTime"];

            if (activity["Exception"] != null) {
                LastLockscreenTaskError.Text = activity["Exception"].ToString();
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
                ShowWallTaskActivity();

                if (BackgroundTask.IsWallTaskActive()) {
                    return;
                }
                
                BackgroundTask.RegisterWallTask(GetWallIntervalUpdate());

            } else {
                HideWallTaskActivity();
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

        private void ShowWallTaskActivity() {
            WallIntervalUpdates.Visibility = Visibility.Visible;
            WallTaskActivity.Visibility = Visibility.Visible;
        }

        private void HideWallTaskActivity() {
            WallIntervalUpdates.Visibility = Visibility.Collapsed;
            WallTaskActivity.Visibility = Visibility.Collapsed;
        }

        private void WallIntervalUpdates_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!WallSwitch.IsOn) {
                return;
            }

            BackgroundTask.UnregisterWallTask();
            BackgroundTask.RegisterWallTask(GetWallIntervalUpdate());
        }

        private uint GetWallIntervalUpdate() {
            var item = (ComboBoxItem)WallIntervalUpdates.SelectedItem;
            string value = (string)item.Tag;
            return uint.Parse(value);
        }

        private void LockscreenSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) {
                ShowLockscreenTaskActivity();

                if (BackgroundTask.IsLockscreenTaskActive()) {
                    return;
                }

                BackgroundTask.RegisterLockscreenTask(GetLockscreenIntervalUpdates());

            } else {
                HideLockscreenTaskActivity();
                BackgroundTask.UnregisterLockscreenTask();
            }
        }

        private void ShowLockscreenTaskActivity() {
            LockscreenIntervalUpdates.Visibility = Visibility.Visible;
            LockscreenTaskActivity.Visibility = Visibility.Visible;
        }

        private void HideLockscreenTaskActivity() {
            LockscreenIntervalUpdates.Visibility = Visibility.Collapsed;
            LockscreenTaskActivity.Visibility = Visibility.Collapsed;
        }

        private void LockscreenIntervalUpdate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!LockscreenSwitch.IsOn) {
                return;
            }

            BackgroundTask.UnregisterLockscreenTask();
            BackgroundTask.RegisterLockscreenTask(GetLockscreenIntervalUpdates());
        }

        private uint GetLockscreenIntervalUpdates() {
            var item = (ComboBoxItem)LockscreenIntervalUpdates.SelectedItem;
            string value = (string)item.Tag;
            return uint.Parse(value);
        }
    }
}
