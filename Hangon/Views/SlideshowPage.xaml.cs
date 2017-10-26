using Hangon.Services;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unsplasharp.Models;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Email;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Hangon.Views {
    public sealed partial class SlideshowPage : Page {

        #region variables

        private List<Photo> _PhotosList { get; set; }

        private string AddonSlideshowId {
            get {
                return "9NHVSTRNZ6PL";
            }
        }

        private Timer _TimerBackground { get; set; }

        private CoreDispatcher _UIDispatcher { get; set; }

        int _SlideshowDuration {
            get {
                return 10000;
            }
        }

        private int _BackgroundChangeCount { get; set; }

        private bool _IsCollectionLocked { get; set; }

        private Photo _CurrentPhoto { get; set; }

        #endregion variables

        public SlideshowPage() {
            InitializeComponent();
            InitializeVariables();
            InitializeTitleBar();
            InitializeData();
        }

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            StopSlideShow();
            App.UpdateTitleBarTheme();
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

        private void GoToPhotoPage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            StopSlideShow();
            Frame.Navigate(typeof(PhotoPage), new object[] { _CurrentPhoto, _PhotosList, this.GetType() });
        }

        private void GoToUser_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(UserPage), _CurrentPhoto);
        }

        private void CmdGoHome_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(HomePage));
        }

        #endregion navigation

        private void InitializeVariables() {
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        #region titlebar

        private void InitializeTitleBar() {
            App.DeviceType = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;

            if (App.DeviceType == "Windows.Mobile") {
                TitleBar.Visibility = Visibility.Collapsed;
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.HideAsync();
                return;
            }

            Window.Current.Activated += Current_Activated;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            TitleBar.Height = coreTitleBar.Height;
            Window.Current.SetTitleBar(TitleBarMainContent);

            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            App.SetTitleBarTheme(ElementTheme.Light);
        }

        void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar titleBar, object args) {
            TitleBar.Visibility = titleBar.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) {
            TitleBar.Height = sender.Height;
            RightMask.Width = sender.SystemOverlayRightInset;
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e) {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated) {
                TitleBarMainContent.Opacity = 1;
                return;
            }

            TitleBarMainContent.Opacity = 0.5;
        }

        #endregion titlebar

        private async void InitializeData() {
            var isAllowed = await InAppPurchases.DoesUserHaveAddon(AddonSlideshowId);

            if (isAllowed) {
                StartSlideshow();
                return;
            }

            SlideshowLoadingView.Visibility = Visibility.Collapsed;
            SlideshowNoAccessView.Visibility = Visibility.Visible;
        }

        private void InitializeCaptionsFozenGlass() {
            ApplyFrostedGlass(UsernameFrozenHost, UserName, 20, 20);
            ApplyFrostedGlass(LikesIconFrozenHost, LikesIcon, 10, 10);
            ApplyFrostedGlass(LikesCountFrozenHost, LikesCount, 10, 15);
            ApplyFrostedGlass(DownloadIconFrozenHost, DownloadIcon, 10, 10);
            ApplyFrostedGlass(DownloadTextFrozenHost, DownloadsCount, 10, 15);
            ApplyFrostedGlass(LocationIconFrozenHost, LocationIcon, 10, 10);
            ApplyFrostedGlass(LocationTextFrozenHost, LocationText, 10, 10);
        }

        #region slideshow management

        private async void StartSlideshow() {
            HidePhotoStats();
            InitializeCaptionsFozenGlass();

            PhotoImage.Visibility = Visibility.Visible;
            PhotoCaption.Visibility = Visibility.Visible;

            _PhotosList = await App.DataSource.GetRandomPhotos();

            SlideshowLoadingView.Visibility = Visibility.Collapsed;

            if (_PhotosList == null || _PhotosList.Count == 0) {
                SlideshowErrorView.Visibility = Visibility.Visible;
                return;
            }
            
            var random = new Random();
            var autoEvent = new AutoResetEvent(false);

            _TimerBackground = new Timer(async (object state) => {
                await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    if (_IsCollectionLocked) {
                        return;
                    }

                    HideFavoriteIcon();

                    var index = random.Next(_PhotosList.Count);

                    UpdateBackground(index);
                    UpdateCaption(index);

                    ShowFavoriteIcon();

                    _BackgroundChangeCount++;

                    if (_BackgroundChangeCount >= 20) {
                        AddNewRandomPhotosToList();
                        _BackgroundChangeCount = 0;
                    }
                });
            }, autoEvent, 0, _SlideshowDuration);
        }

        private async void AddNewRandomPhotosToList() {
            var newPhotos = await App.DataSource.GetRandomPhotos();

            if (newPhotos == null) return;

            _IsCollectionLocked = true;

            _PhotosList.Clear();
            _PhotosList.AddRange(newPhotos);

            _IsCollectionLocked = false;
        }

        private async void UpdateBackground(int index) {
            _CurrentPhoto = _PhotosList[index];
            var photoPath = _CurrentPhoto.Urls.Regular;

            await PhotoImage.Fade(0).StartAsync();
            await PhotoImage.Scale(1.2f, 1.2f, 0, 0, 0).StartAsync();

            PhotoImage.Source = new BitmapImage(new Uri(photoPath));
            PhotoImage.Fade(1, _SlideshowDuration).Scale(1f, 1f, 0, 0, _SlideshowDuration).Start();
        }

        private void UpdateCaption(int index) {
            HidePhotoStats();
            PopulateStats(index);
            AnimatePhotoStats();
        }

        private void PopulateStats(int index) {
            var photo = _PhotosList[index];
            var userProfileUri = App.DataSource.GetProfileImageLink(photo.User);

            UserImageSource.UriSource = new Uri(userProfileUri);
            UserName.Text = photo.User.Name ?? photo.User.Username ??
                string.Format("{0} {1}", photo.User.FirstName, photo.User.LastName);

            LocationText.Text = photo.Location?.Title ?? "";

            if (string.IsNullOrEmpty(LocationText.Text)) {
                LocationPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            } else {
                LocationPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            LikesCount.Text = photo.Likes.ToString();
            DownloadsCount.Text = photo.Downloads.ToString();
        }

        private void HidePhotoStats() {
            UserPanel.Opacity = 0;
            LocationPanel.Opacity = 0;
            StatsCountPanel.Opacity = 0;
        }

        private void AnimatePhotoStats() {
            UserPanel
                .Offset(0, 100).Fade(0)
                .Then()
                .Offset(0, 0).Fade(1)
                .Start();

            StatsCountPanel
                .Offset(0, 100).Fade(0)
                .Then()
                .Offset(0, 0).Fade(1)
                .SetDelay(100)
                .Start();

            LocationPanel
                .Offset(0, 100).Fade(0)
                .Then()
                .Offset(0, 0).Fade(1)
                .SetDelay(200)
                .Start();
        }

        private void StopSlideShow() {
            _TimerBackground?.Dispose();
            _TimerBackground = null;
        }

        private void TryAgainStartingSlideshow_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            SlideshowNoAccessView.Visibility = Visibility.Collapsed;
            SlideshowErrorView.Visibility = Visibility.Collapsed;
            SlideshowLoadingView.Visibility = Visibility.Visible;

            InitializeData();
        }

        private async void ShowFavoriteIcon() {
            var isFavorite = await IsCurrentPhotoInFavorites();

            if (isFavorite) {
                ToggleFavoritesIcon.Glyph = "\uE00B";

            } else { ToggleFavoritesIcon.Glyph = "\uE006"; }

            FloatingToggleFavoritesPanel.Visibility = Visibility.Visible;

            var centerX = (float)ToggleFavoritesIcon.ActualWidth / 2;
            var centerY = (float)ToggleFavoritesIcon.ActualWidth / 2;

            ToggleFavoritesIcon
                .Fade(1)
                .Scale(1.2f, 1.2f, centerX, centerY)
                .Then()
                .Scale(1f, 1f, centerX, centerY)
                .Start();
        }

        private void HideFavoriteIcon() {
            var centerX = (float)ToggleFavoritesIcon.ActualWidth / 2;
            var centerY = (float)ToggleFavoritesIcon.ActualWidth / 2;

            ToggleFavoritesIcon
                .Fade(1)
                .Scale(.9f, .9f, centerX, centerY)
                .Start();

            FloatingToggleFavoritesPanel.Visibility = Visibility.Collapsed;
        }

        private async Task<bool> IsCurrentPhotoInFavorites() {
            await App.DataSource.LoadLocalFavorites();
            return App.DataSource.LocalFavorites.Contains(_CurrentPhoto.Id);
        }
        
        #endregion slideshow management

        #region micro animations

        private void UserProfileImage_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            var centerX = (float)ellipse.ActualWidth / 2;
            var centerY = (float)ellipse.ActualHeight / 2;

            ellipse
                .Scale(1.1f, 1.1f, centerX, centerY)
                .Start();
        }

        private void UserProfileImage_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            var centerX = (float)ellipse.ActualWidth / 2;
            var centerY = (float)ellipse.ActualHeight / 2;

            ellipse
                .Scale(1f, 1f, centerX, centerY)
                .Start();
        }

        private void AddFavoritesIcon_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var icon = (FontIcon)sender;
            var centerX = (float)icon.ActualWidth / 2;
            var centerY = (float)icon.ActualHeight / 2;

            icon
                .Scale(1.1f, 1.1f, centerX, centerY)
                .Start();
        }

        private void AddFavoritesIcon_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var icon = (FontIcon)sender;
            var centerX = (float)icon.ActualWidth / 2;
            var centerY = (float)icon.ActualHeight / 2;

            icon
                .Scale(1f, 1f, centerX, centerY)
                .Start();
        }

        #endregion micro animations

        #region events

        private async void ToggleFavorite_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var isFavorite = await IsCurrentPhotoInFavorites();

            if (isFavorite) {
                await App.DataSource.RemoveFromFavorites(_CurrentPhoto);
            } else {
                await App.DataSource.AddToFavorites(_CurrentPhoto);
            }
            
            ShowFavoriteIcon();
        }

        private async void BuySlideshowAddon_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var result = await InAppPurchases.PurchaseAddon(AddonSlideshowId);
            var messageResult = InAppPurchases.GetMessagePurchaseResult(result);

            DataTransfer.ShowLocalToast(messageResult);
        }

        private void ContactMe_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            EmailMessage email = new EmailMessage() {
                Subject = "[Hangon] Feedback",
                Body = "send this email to jeremiecorpinot@outlook.com"
            };
            
            var task = EmailManager.ShowComposeNewEmailAsync(email);
        }

        #endregion events

        private void ApplyFrostedGlass(FrameworkElement host, FrameworkElement parent, 
            double additionalHeight = 0, double additionalWidth = 0) {

            parent.SizeChanged += (s, e) => {
                host.Height = parent.ActualHeight + additionalHeight;
                host.Width = parent.ActualWidth + additionalWidth;
            };

            var glassHost = host;
            var visual = ElementCompositionPreview.GetElementVisual(glassHost);
            var compositor = visual.Compositor;

            // Create a glass effect, requires Win2D NuGet package
            var glassEffect = new GaussianBlurEffect {
                BlurAmount = 10.0f,
                BorderMode = EffectBorderMode.Hard,
                Source = new ArithmeticCompositeEffect {
                    MultiplyAmount = 0,
                    Source1Amount = 0.5f,
                    Source2Amount = 0.5f,
                    Source1 = new CompositionEffectSourceParameter("backdropBrush"),
                    Source2 = new ColorSourceEffect {
                        Color = Color.FromArgb(255, 245, 245, 245)
                    }
                }
            };

            //  Create an instance of the effect and set its source to a CompositionBackdropBrush
            var effectFactory = compositor.CreateEffectFactory(glassEffect);
            var backdropBrush = compositor.CreateBackdropBrush();
            var effectBrush = effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("backdropBrush", backdropBrush);

            // Create a Visual to contain the frosted glass effect
            var glassVisual = compositor.CreateSpriteVisual();
            glassVisual.Brush = effectBrush;

            // Add the blur as a child of the host in the visual tree
            ElementCompositionPreview.SetElementChildVisual(glassHost, glassVisual);

            // Make sure size of glass host and glass visual always stay in sync
            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", visual);

            glassVisual.StartAnimation("Size", bindSizeAnimation);
        }

    }
}
