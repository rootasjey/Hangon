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

            PhotoView.Source = new BitmapImage(new System.Uri(photo.Urls.Regular));
        }

        private void FetchData(Photo wall) {

        }

        private void Populate() {

        }


        void ShowProgress(string message = "") {
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

        #region commandbar
        private void CmdSetWallpaper(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Wallpaper.SetAsWallpaper(CurrentPhoto);
        }

        private void CmdSetLockscreen(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Wallpaper.SetAsLockscreen(CurrentPhoto);
        }

        private async void CmdDownload_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            ShowProgress(); // show progress flyout
            await Wallpaper.SaveToPicturesLibrary(CurrentPhoto, HttpProgressCallback);
            HideProgress(); // hide show progress flyout
        }

        private void CmdCrop_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {

        }

        #endregion commandbar
    }
}
