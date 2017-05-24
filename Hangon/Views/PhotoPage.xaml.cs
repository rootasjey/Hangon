using System;
using Hangon.Models;
using Hangon.Services;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Animations;

namespace Hangon.Views {
    public sealed partial class PhotoPage : Page {
        public Photo CurrentPhoto { get; set; }

        public PhotoPage() {
            InitializeComponent();
        }

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("WallpaperImage", PhotoView);
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            var photo = (Photo)e.Parameter;
            CurrentPhoto = photo;

            HandleConnectedAnimation(photo);
            FetchData(photo);
            Populate();

            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion navigation

        private void HandleConnectedAnimation(Photo photo) {
            if (photo == null) return;

            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("WallpaperImage");
            if (animation != null) {
                PhotoView.Opacity = 0;
                PhotoView.ImageOpened += (s, e) => {
                    PhotoView.Opacity = 1;
                    animation.TryStart(PhotoView);
                };                
            }

            PhotoView.Source = new BitmapImage(new Uri(photo.Urls.Regular));
        }

        private void FetchData(Photo wall) {

        }

        private void Populate() {

        }


        void ShowProgress(string message = "") {
            ProgressDeterminate.Value = 0;
            FlyoutNotification.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(message)) return;
            FlyoutText.Text = message;
        }

        void HideProgress() {
            FlyoutNotification.Visibility = Visibility.Collapsed;
        }

        public void HttpProgressCallback(Windows.Web.Http.HttpProgress progress) {
            if (progress.TotalBytesToReceive == null) return;

            ProgressDeterminate.Minimum = 0;
            ProgressDeterminate.Maximum = (double)progress.TotalBytesToReceive;
            ProgressDeterminate.Value = progress.BytesReceived;
        }

        async void Download(string size = "") {
            ShowProgress(); // show progress flyout

            if (string.IsNullOrEmpty(size)) {
                await Wallpaper.SaveToPicturesLibrary(CurrentPhoto, HttpProgressCallback);

            } else {
                string url = getURL();
                await Wallpaper.SaveToPicturesLibrary(CurrentPhoto, HttpProgressCallback, url);
            }

            HideProgress(); // hide show progress flyout

            string getURL()
            {
                switch (size) {
                    case "raw":
                        return CurrentPhoto.Urls.Raw;
                    case "full":
                        return CurrentPhoto.Urls.Full;
                    case "regular":
                        return CurrentPhoto.Urls.Regular;
                    case "small":
                        return CurrentPhoto.Urls.Small;
                    default:
                        return CurrentPhoto.Urls.Regular;
                }
            }
        }

        #region commandbar
        private async void CmdSetWallpaper(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            ShowProgress("Setting wallpaper");
            var success = await Wallpaper.SetAsWallpaper(CurrentPhoto, HttpProgressCallback);
            HideProgress();

            if (!success) {
                DataTransfer.ShowLocalToast("Ops. There I couldn't set your wellpaper. Try again or contact the developer.");
            }
        }

        private async void CmdSetLockscreen(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            ShowProgress("Setting lockscreen background");
            var success = await Wallpaper.SetAsLockscreen(CurrentPhoto, HttpProgressCallback);
            HideProgress();

            if (!success) {
                DataTransfer.ShowLocalToast("Ops. There I couldn't set your lockscreen background. Try again or contact the developer.");
            }
        }

        private void CmdDownload_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (Settings.UseDefaultDownloadResolution()) {
                var resolution = Settings.GetDefaultDownloadResolution();
                Download(resolution);
                return;
            }

            var cmd = (AppBarButton)sender;
            FlyoutDownload.ShowAt(cmd);
        }

        private void CmdDownloadResolution_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var cmd = (MenuFlyoutItem)sender;
            var resolution = cmd.Text;
            Download(resolution);
        }

        private void CmdCrop_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {

        }

        #endregion commandbar
        
    }
}
