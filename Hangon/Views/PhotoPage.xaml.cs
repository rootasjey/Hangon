﻿using System;
using Hangon.Models;
using Hangon.Services;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Hangon.Data;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using Windows.Foundation;
using System.Globalization;
using System.Threading;

namespace Hangon.Views {
    public sealed partial class PhotoPage : Page {
        #region variables
        public Photo CurrentPhoto { get; set; }

        private DataSource PageDataSource { get; set; }

        private Visual _backgroundVisual { get; set; }
        private Compositor _backgroundCompositor { get; set; }
        private ScrollViewer _ScrollViewer { get; set; }
        private CompositionPropertySet _ScrollerPropertySet { get; set; }

        CoreDispatcher UIDispatcher { get; set; }
        #endregion variables

        public PhotoPage() {
            InitializeComponent();
            PageDataSource = App.AppDataSource;
            UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", PhotoView);
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            var photo = (Photo)e.Parameter;
            CurrentPhoto = photo;

            HandleConnectedAnimation(photo);
            FetchData(photo);

            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }
        
        #endregion navigation

        #region data
        private async void FetchData(Photo photo) {
            CurrentPhoto = await PageDataSource.GetPhoto(photo.Id);
            Populate();
        }

        private void Populate() {
            PopulateUSer();
            PopulateStats();
            PopulateExif();
            AnimateEntrance();

            void PopulateUSer()
            {
                UserImageSource.UriSource = new Uri(Unsplash.GetProfileImageLink(CurrentPhoto.User));
                UserName.Text = CurrentPhoto.User.Name;
                UserLocation.Text = CurrentPhoto.User.Location ?? "";
            }

            void PopulateStats()
            {
                DownloadsCount.Text = CurrentPhoto.Downloads.ToString();
                LikesCount.Text = CurrentPhoto.Likes.ToString();
            }

            void PopulateExif()
            {
                ExifMake.Text = CurrentPhoto.Exif.Make ?? "";
                ExifModel.Text = CurrentPhoto.Exif.Model ?? "";

                if (string.IsNullOrEmpty(ExifMake.Text) && 
                    string.IsNullOrEmpty(ExifModel.Text)) {
                    IconCamera.Visibility = Visibility.Collapsed;
                }

                PublishedOn.Text = DateTime
                    .ParseExact(CurrentPhoto.CreatedAt, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                    .ToLocalTime()
                    .ToString("dd MMMM yyyy");
            }

            void AnimateEntrance()
            {
                PhotoCaption.Offset(0, 30, 0)
                    .Then()
                    .Fade(1, 500, 500)
                    .Offset(0, 0, 500, 500)
                    .Start();
            }
        }
        
        #endregion data

        #region download

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
            ShowProgress();
            var result = false;

            if (string.IsNullOrEmpty(size)) {
                result = await Wallpaper.SaveToPicturesLibrary(CurrentPhoto, HttpProgressCallback);

            } else {
                string url = getURL();
                result = await Wallpaper.SaveToPicturesLibrary(CurrentPhoto, HttpProgressCallback, url);
            }

            HideProgress();

            if (result) Notify("The photo was saved."); 
            else Notify("The photo couldn't be saved :(");

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

        #endregion download

        #region commandbar
        private async void CmdSetWallpaper(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            ShowProgress("Setting wallpaper");
            var success = await Wallpaper.SetAsWallpaper(CurrentPhoto, HttpProgressCallback);
            HideProgress();

            if (success) Notify("The photo has been correctly set has your wallpaper!");
            else Notify("Ops. There I couldn't set your wallpaper :( Try again or contact the developer.");
        }

        private async void CmdSetLockscreen(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            ShowProgress("Setting lockscreen background");
            var success = await Wallpaper.SetAsLockscreen(CurrentPhoto, HttpProgressCallback);
            HideProgress();

            if (success) Notify("The photo has been correctly set has your locksreen background!");
            else Notify("Ops. There I couldn't set your lockscreen background :( Try again or contact the developer.");
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

        #region micro-interactions
        private void UserView_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var panel = (Ellipse)sender;
            panel.Scale(1.1f, 1.1f).Start();
        }

        private void UserView_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var panel = (Ellipse)sender;
            panel.Scale(1f, 1f).Start();
        }

        #endregion micro-interactions

        #region image composition
        private void HandleConnectedAnimation(Photo photo) {
            if (photo == null) return;

            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PhotoImage");
            if (animation != null) {
                //PhotoView.Opacity = 0;
                PhotoView.ImageOpened += (s, e) => {
                    PhotoView.Opacity = 1;
                    animation.TryStart(PhotoView);
                };
            }

            PhotoView.Source = new BitmapImage(new Uri(photo.Urls.Regular));
        }

        private void PhotoViewContainer_Loaded(object sender, RoutedEventArgs ev) {
            GetPhotoCompositorVariables();

            PhotoCaption.Loaded += (s, e) => {
                GetScrollViewerProps();
                AttachBlurOpacityEffects();
            };
        }

        void GetPhotoCompositorVariables() {
            _backgroundVisual = ElementCompositionPreview.GetElementVisual(PhotoViewContainer);
            _backgroundCompositor = _backgroundVisual.Compositor;
        }
        
        void GetScrollViewerProps() {
            _ScrollViewer = PhotoCaption;
            _ScrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(_ScrollViewer);
        }

        void AttachBlurOpacityEffects() {
            // -----
            // Blur
            // -----
            var blurEffect = new GaussianBlurEffect() {
                Name = "Blur",
                BlurAmount = 20.0f,
                BorderMode = EffectBorderMode.Hard,
                Optimization = EffectOptimization.Speed,
                Source = new CompositionEffectSourceParameter("Backdrop")
            };

            var effectFactory = _backgroundCompositor.CreateEffectFactory(blurEffect, new[] { "Blur.BlurAmount" });
            var effectBrush = effectFactory.CreateBrush();

            var destinationBrush = _backgroundCompositor.CreateBackdropBrush();
            effectBrush.SetSourceParameter("Backdrop", destinationBrush);

            var _bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var _scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var _size = new Size(_bounds.Width * _scaleFactor, _bounds.Height * _scaleFactor);

            var blurVisual = _backgroundCompositor.CreateSpriteVisual();
            blurVisual.Size = new Vector2((float)_size.Width + 300, (float)_size.Height);
            blurVisual.Brush = effectBrush;
            ElementCompositionPreview.SetElementChildVisual(PhotoViewContainer, blurVisual);

            var blurExpression = _backgroundCompositor.CreateExpressionAnimation(
                "Clamp(-scroller.Translation.Y / 10,0,20)");
            blurExpression.SetReferenceParameter("scroller", _ScrollerPropertySet);

            blurVisual.Brush.Properties.StartAnimation("Blur.BlurAmount", blurExpression);

            // --------
            // Opacity
            // --------
            var opacityVisual = _backgroundCompositor.CreateSpriteVisual();
            opacityVisual.Brush = _backgroundCompositor.CreateColorBrush(Windows.UI.Colors.Black);
            opacityVisual.Size = new Vector2((float)_size.Width + 300, (float)_size.Height);
            ElementCompositionPreview.SetElementChildVisual(PhotoDimmer, opacityVisual);

            var opacityExpression = _backgroundCompositor.CreateExpressionAnimation(
                "Clamp((-scroller.Translation.Y) / 100, 0, 0.6)");
            opacityExpression.SetReferenceParameter("scroller", _ScrollerPropertySet);
            opacityVisual.StartAnimation("Opacity", opacityExpression);
        }


        #endregion image composition

        #region events

        private void PhotoCaptionContent_Loaded(object sender, RoutedEventArgs e) {
            UpdatePhotoCaptionPosition();
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e) {
            UpdatePhotoCaptionPosition();
        }

        private void PhotoCaptionContent_Unloaded(object sender, RoutedEventArgs e) {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        private void UserView_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("UserProfileImage", UserProfileImage);
            Frame.Navigate(typeof(UserPage), CurrentPhoto);
        }

        private async void OpenPhotoInBrowser_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Unsplash.ApplicationId;
            var photoUri = new Uri(string.Format("{0}{1}", CurrentPhoto.Links.Html, tracking));
            var success = await Windows.System.Launcher.LaunchUriAsync(photoUri);
        }
        #endregion events

        private void UpdatePhotoCaptionPosition() {
            var height = Window.Current.Bounds.Height - 180;
            RowSpacing.Height = new GridLength(height);
        }
        
        private void FlyoutNotification_Dismiss(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            HideNotification();
        }

        void Notify(string message) {
            ProgressGrid.Visibility = Visibility.Collapsed;
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
    }
}
