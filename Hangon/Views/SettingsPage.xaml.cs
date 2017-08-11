using Hangon.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Globalization;
using System.Threading;
using Windows.ApplicationModel.Email;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Hangon.Views {
    public sealed partial class SettingsPage : Page {
        CoreDispatcher UIDispatcher { get; set; }

        public SettingsPage() {
            InitializeComponent();
            SetUpPageAnimation();
            LoadData();

            UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            AnimatePersonalizationPivot();
        }

        #region animations
        private void SetUpPageAnimation() {
            TransitionCollection collection = new TransitionCollection();
            NavigationThemeTransition theme = new NavigationThemeTransition();

            var info = new ContinuumNavigationTransitionInfo();

            theme.DefaultNavigationTransitionInfo = info;
            collection.Add(theme);
            Transitions = collection;
        }
        

        private void AnimatePersonalizationPivot() {
            PersonalizationContentPanel.AnimateSlideIn();
        }

        #endregion animations

        private void LoadData() {
            WallSwitch.IsOn = BackgroundTasks.IsWallTaskActive();
            LockscreenSwitch.IsOn = BackgroundTasks.IsLockscreenTaskActive();
            TileSwitch.IsOn = BackgroundTasks.IsTileTaskActive();

            UpdateWallTaskActivityText();
            UpdateLockscreenTaskActivityText();
        }

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion navigation

        #region about

        private void FeedbackButton_Click(object sender, RoutedEventArgs e) {
            EmailMessage email = new EmailMessage() {
                Subject = "[Hangon] Feedback",
                Body = "send this email to jeremiecorpinot@outlook.com"
            };
            // TODO : add app infos
            EmailManager.ShowComposeNewEmailAsync(email);
        }

        private async void NoteButton_Click(object sender, RoutedEventArgs e) {
            string appID = "9PF0GQ81HNDF";
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=" + appID));
        }
        #endregion about

        #region tasks

        // ---------
        // WALL TASK
        // ---------
        #region wall task
        private void UpdateWallTaskActivityText() {
            var activity = BackgroundTasks.GetWallTaskActivity();
            if (activity == null) return;
            LastUpdatedTask.Text = "Wallpaper task last run on: " + activity["DateTime"];

            if (activity["Exception"] != null) {
                LastWallTaskError.Text = activity["Exception"].ToString();
            }
        }


        private void WallSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;
            if (toggle.IsOn) {
                ShowWallTaskActivity();

                if (BackgroundTasks.IsWallTaskActive()) {
                    return;
                }

                BackgroundTasks.RegisterWallTask(GetWallIntervalUpdate());

            } else {
                HideWallTaskActivity();
                BackgroundTasks.UnregisterWallTask();
            }
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

            BackgroundTasks.UnregisterWallTask();
            BackgroundTasks.RegisterWallTask(GetWallIntervalUpdate());
        }

        private uint GetWallIntervalUpdate() {
            var item = (ComboBoxItem)WallIntervalUpdates.SelectedItem;
            string value = (string)item.Tag;
            return uint.Parse(value);
        }

        private void RestartWallTask_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            BackgroundTasks.UnregisterWallTask();
            BackgroundTasks.RegisterWallTask(GetWallIntervalUpdate());
        }
        #endregion wall task


        // ---------------
        // LOCKSCREEN TASK
        // ---------------
        #region lockscreen task
        private void UpdateLockscreenTaskActivityText() {
            var activity = BackgroundTasks.GetLockscreenTaskActivity();
            if (activity == null) return;
            LastUpdatedLockscreenTask.Text = "Wallpaper task last run on: " + activity["DateTime"];

            if (activity["Exception"] != null) {
                LastLockscreenTaskError.Text = activity["Exception"].ToString();
            }
        }

        private void LockscreenSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) {
                ShowLockscreenTaskActivity();

                if (BackgroundTasks.IsLockscreenTaskActive()) {
                    return;
                }

                BackgroundTasks.RegisterLockscreenTask(GetLockscreenIntervalUpdates());

            } else {
                HideLockscreenTaskActivity();
                BackgroundTasks.UnregisterLockscreenTask();
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

        private void LockscreenIntervalUpdates_Loaded(object sender, RoutedEventArgs e) {
            var currentInterval = BackgroundTasks.GetLockscreenTaskInterval();


            for (int i = 0; i < LockscreenIntervalUpdates.Items.Count; i++) {
                var item = (ComboBoxItem)LockscreenIntervalUpdates.Items[i];
                var itemInterval = uint.Parse((string)item.Tag);

                if (itemInterval == currentInterval) {
                    LockscreenIntervalUpdates.SelectedIndex = i;
                    break;
                }
            }
        }

        private void LockscreenIntervalUpdate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!LockscreenSwitch.IsOn) {
                return;
            }

            BackgroundTasks.UnregisterLockscreenTask();
            BackgroundTasks.RegisterLockscreenTask(GetLockscreenIntervalUpdates());
        }

        private uint GetLockscreenIntervalUpdates() {
            var item = (ComboBoxItem)LockscreenIntervalUpdates.SelectedItem;
            string value = (string)item.Tag;
            return uint.Parse(value);
        }

        private void RestartLockscreenTask_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            BackgroundTasks.UnregisterLockscreenTask();
            BackgroundTasks.RegisterLockscreenTask(GetLockscreenIntervalUpdates());
        }

        #endregion lockscreen task


        // ---------
        // TILE TASK
        // ---------
        #region tile task
        private void TileSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) {
                ShowTileTaskActivity();

                if (BackgroundTasks.IsTileTaskActive()) {
                    return;
                }

                BackgroundTasks.RegisterTileTask(GetTileIntervalUpdates());

            } else {
                HideTileTaskActivity();
                BackgroundTasks.UnregisterTileTask();
            }
        }

        void ShowTileTaskActivity() {
            TileIntervalUpdates.Visibility = Visibility.Visible;
            TileTaskActivity.Visibility = Visibility.Visible;
        }

        void HideTileTaskActivity() {
            TileIntervalUpdates.Visibility = Visibility.Collapsed;
            TileTaskActivity.Visibility = Visibility.Collapsed;
        }

        private uint GetTileIntervalUpdates() {
            var item = (ComboBoxItem)TileIntervalUpdates.SelectedItem;
            string value = (string)item.Tag;
            return uint.Parse(value);
        }

        private void TileIntervalUpdates_Loaded(object sender, RoutedEventArgs e) {
            var currentInterval = BackgroundTasks.GetTileTaskInterval();

            for (int i = 0; i < TileIntervalUpdates.Items.Count; i++) {
                var item = (ComboBoxItem)TileIntervalUpdates.Items[i];
                var itemInterval = uint.Parse((string)item.Tag);

                if (itemInterval == currentInterval) {
                    TileIntervalUpdates.SelectedIndex = i;
                    break;
                }
            }
        }

        private void TileIntervalUpdate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!TileSwitch.IsOn) {
                return;
            }

            BackgroundTasks.UnregisterTileTask();
            BackgroundTasks.RegisterTileTask(GetTileIntervalUpdates());
        }

        private void RestartTileTask_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            BackgroundTasks.UnregisterTileTask();
            BackgroundTasks.RegisterTileTask(GetTileIntervalUpdates());
        }

        #endregion tile task

        #endregion tasks

        #region personalization
        #region auto save downloads location
        private void ToggleAutoSaveDownloads_Loaded(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;
            toggle.IsOn = Settings.UseDefaultDownloadPath();
        }

        private void ToggleAutoSaveDownloads_Toggled(object sender, RoutedEventArgs e) {
            Settings.UpdateUseDefaultDownloadPath(ToggleAutoSaveDownloads.IsOn);
        }

        #endregion auto save downloads location

        private void DefaultPhotoResolution_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var item = (ComboBoxItem)DefaultPhotoResolution.SelectedValue;
            var resolution = (string)item.Tag;

            if (!Settings.UseDefaultDownloadResolution()) return;
            if (resolution == Settings.GetDefaultDownloadResolution()) return;
            Settings.SaveDefaultDownloadResolution(resolution);
        }

        #region language
        private void LanguageSelection_Loaded(object sender, RoutedEventArgs e) {
            var language = Settings.GetAppCurrentLanguage();

            var culture = new CultureInfo(language);

            if (culture.CompareInfo.IndexOf(language, "en", CompareOptions.IgnoreCase) >= 0) {
                LanguageSelection.SelectedIndex = 0;
                return;
            }

            if (culture.CompareInfo.IndexOf(language, "fr", CompareOptions.IgnoreCase) >= 0) {
                LanguageSelection.SelectedIndex = 1;
                return;
            }
        }

        private void LanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var item = (ComboBoxItem)LanguageSelection.SelectedItem;
            var language = (string)item.Tag;
            var fullLang = (string)item.Content;

            if (language == Settings.GetAppCurrentLanguage()) return;
            Settings.SaveAppCurrentLanguage(language);

            App.UpdateLanguage();
            ToastLanguageUpdated();

            void ToastLanguageUpdated()
            {
                var toastMessage = fullLang + " language selected!";
                Notify(toastMessage);
            }
        }
        

        #endregion language

        private void DefaultPhotoResolution_Loaded(object sender, RoutedEventArgs e) {
            var resolutionChooser = (ComboBox)sender;
            var resolution = Settings.GetDefaultDownloadResolution();

            switch (resolution) {
                case "raw":
                    resolutionChooser.SelectedIndex = 0;
                    break;
                case "full":
                    resolutionChooser.SelectedIndex = 1;
                    break;
                case "regular":
                    resolutionChooser.SelectedIndex = 2;
                    break;
                case "small":
                    resolutionChooser.SelectedIndex = 3;
                    break;
                default:
                    resolutionChooser.SelectedIndex = 0;
                    break;
            }
        }

        #region auto download resolution
        private void ToggleAutoDownloadsResolution_Loaded(object sender, RoutedEventArgs e) {
            var active = Settings.UseDefaultDownloadResolution();
            var toggle = (ToggleSwitch)sender;
            toggle.IsOn = active;

            if (active) {
                DefaultPhotoResolution.Visibility = Visibility.Visible;
            }
        }

        private void ToggleAutoDownloadsResolution_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;
            var active = toggle.IsOn;

            if (active) {
                DefaultPhotoResolution.Visibility = Visibility.Visible;
            } else {
                DefaultPhotoResolution.Visibility = Visibility.Collapsed;
            }

            if (active == Settings.UseDefaultDownloadResolution()) return;
            Settings.UpdateUseDefaultResolution(active);

            
        }

        #endregion auto download resolution

        private void ThemeSwitch_Loaded(object sender, RoutedEventArgs e) {
            UpdateThemeSwitcher();

            void UpdateThemeSwitcher()
            {
                ThemeSwitch.IsOn = Settings.IsApplicationThemeLight();
            }
        }

        private void ThemeSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) ChangeTheme(ApplicationTheme.Light);

            else ChangeTheme(ApplicationTheme.Dark);

            void ChangeTheme(ApplicationTheme theme)
            {
                Settings.UpdateAppTheme(theme);
            }
        }


        #endregion personalization

        #region notifications
        private void FlyoutNotification_Dismiss(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            HideNotification();
        }

        void Notify(string message) {
            FlyoutText.Text = message;

            FlyoutNotification.Opacity = 0;
            FlyoutNotification.Visibility = Visibility.Visible;

            FlyoutNotification
                .Offset(0, -30, 0)
                .Then()
                .Fade(1)
                .Offset(0)
                .Start();

            var autoEvent = new AutoResetEvent(false);
            var timer = new Timer(async (object state) => {
                UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    HideNotification();
                });
            }, autoEvent, TimeSpan.FromSeconds(5), new TimeSpan());
        }

        async void HideNotification() {
            await FlyoutNotification
                .Fade(0)
                .Offset(0, -30)
                .StartAsync();

            FlyoutNotification.Visibility = Visibility.Collapsed;
        }
        #endregion notifications

    }
}
