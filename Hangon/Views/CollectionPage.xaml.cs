using Hangon.Data;
using Hangon.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml;
using System.Globalization;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Composition;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI;
using Windows.ApplicationModel.Resources;
using System.Threading;
using Windows.UI.Xaml.Input;
using Unsplasharp.Models;

namespace Hangon.Views {
    public sealed partial class CollectionPage : Page {
        #region variables
        private DataSource _PageDataSource { get; set; }

        private Collection _CurrentCollection { get; set; }

        private double _AnimationDelay { get; set; }

        private bool _IsGoingFoward { get; set; }

        public static Photo _LastSelectedPhoto { get; set; }

        private static int _LastSelectedPivotIndex { get; set; }

        private CoreDispatcher _UIDispatcher { get; set; }
        #endregion variables

        public CollectionPage() {
            InitializeComponent();
            InitializeVariables();
            RestoreLastSelectedPivotIndex();
            ApplyCommandBarBarFrostedGlass();
        }

        #region navigation
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            _CurrentCollection = (Collection)e.Parameter;
            HandleConnectedAnimations(_CurrentCollection);
            LoadData();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            if (!_IsGoingFoward) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CollectionCoverImage", CollectionCoverImage);
            }

            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }
        #endregion navigation

        #region data
        private void InitializeVariables() {
            _PageDataSource = App.AppDataSource;
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void RestoreLastSelectedPivotIndex() {
            PivotCollection.SelectedIndex = _LastSelectedPivotIndex;
        }

        private void LoadData() {
            LoadUserInfos();
            LoadPartialCollection();
            LoadCompleteCollection();
            LoadCollectionPhotos();
        }

        /// <summary>
        /// Populate UI with cached (already downloaded) infos
        /// </summary>
        private void LoadPartialCollection() {
            TextTitle.Text = _CurrentCollection.Title;
            TextDescription.Text = _CurrentCollection.Description ?? "";
            TextPubDate.Text = DateTime
                .ParseExact(
                    _CurrentCollection.PublishedAt, 
                    "MM/dd/yyyy HH:mm:ss", 
                    CultureInfo.InvariantCulture)
                .ToLocalTime()
                .ToString("dd MMMM yyyy");
        }

        private void LoadUserInfos() {
            UserName.Text = _PageDataSource.GetUsernameFormated(_CurrentCollection.User);
            
            if (string.IsNullOrEmpty(_CurrentCollection.User.Location)) {
                PanelUserLocation.Visibility = Visibility.Collapsed;

            } else { UserLocation.Text = _CurrentCollection.User.Location; }
        }

        /// <summary>
        /// Fetch full collection's infos from the internet (INTERNET!)
        /// </summary>
        private async void LoadCompleteCollection() {
            _CurrentCollection = await _PageDataSource.GetCollection(_CurrentCollection.Id);
        }

        private async void LoadCollectionPhotos() {
            await _PageDataSource.GetCollectionPhotos(_CurrentCollection.Id);

            if (_PageDataSource.CollectionPhotos.Count > 0) { bindData(); } 
            else { showEmptyViews(); }

            void bindData()
            {
                PhotosListView.ItemsSource = _PageDataSource.CollectionPhotos;
                PhotosGridView.ItemsSource = _PageDataSource.CollectionPhotos;
                TextPhotosCount.Text = _PageDataSource.CollectionPhotos.Count.ToString();
            }

            void showEmptyViews()
            {
                PhotosListViewHeader.Visibility = Visibility.Collapsed;
                PhotosListView.Visibility = Visibility.Collapsed;
                PhotosGridView.Visibility = Visibility.Collapsed;
                EmptyViewPhotos.Visibility = Visibility.Visible;
                EmptyViewPhotosListView.Visibility = Visibility.Visible;
            }
        }

        #endregion data

        #region micro-interactions
        private void HandleConnectedAnimations(Collection collection) {
            if (collection == null) return;

            var animationService = ConnectedAnimationService.GetForCurrentView();

            AnimateCollectionCover();
            AnimateUserProfileImage();
            AnimatePhotoImage();

            void AnimateCollectionCover()
            {
                var coverAnimation = animationService.GetAnimation("CollectionCoverImage");

                if (_CurrentCollection.CoverPhoto == null) {
                    coverAnimation.Cancel();
                    return;
                }

                if (coverAnimation != null) {
                    CollectionCoverImage.Opacity = 0;
                    CollectionCoverImage.ImageOpened += (s, e) => {
                        CollectionCoverImage.Opacity = .6;
                        coverAnimation.TryStart(CollectionCoverImage);
                        BackgroundBlurEffect.Blur(10, 1000, 1000).Start();
                    };

                } else {
                    CollectionCoverImage.Fade(.6f).Start();
                    BackgroundBlurEffect.Blur(10, 1000, 1000).Start();
                }

                CollectionCoverImage.Source = new BitmapImage(new Uri(_CurrentCollection.CoverPhoto.Urls.Regular));
            }

            void AnimateUserProfileImage()
            {
                var profileAnimation = animationService.GetAnimation("UserProfileImage");

                if (profileAnimation != null) {
                    UserProfileImage.Opacity = 0; // TODO: check opacity effect on animation
                    UserImageSource.ImageOpened += (s, e) => {
                        UserProfileImage.Opacity = 1;
                        profileAnimation.TryStart(UserProfileImage);
                    };
                }
                
                UserImageSource.UriSource = new Uri(_PageDataSource.GetProfileImageLink(_CurrentCollection.User));
            }

            void AnimatePhotoImage()
            {
                var photoAnimation = animationService.GetAnimation("PhotoImageBack");

                if (photoAnimation == null || _LastSelectedPhoto == null) return;

                if (_LastSelectedPivotIndex == 0) {
                    PhotosListView.Loaded += (s, e) => {
                        PhotosListView.ScrollIntoView(_LastSelectedPhoto);

                        var item = (ListViewItem)PhotosListView.ContainerFromItem(_LastSelectedPhoto);
                        if (item == null) { photoAnimation.Cancel(); return; }

                        var stack = (StackPanel)item.ContentTemplateRoot;
                        var image = (Image)stack.FindName("PhotoImage");
                        if (image == null) { photoAnimation.Cancel(); return; }

                        image.Opacity = 0;
                        image.Loaded += (_s, _e) => {
                            image.Opacity = 1;
                            photoAnimation.TryStart(image);
                        };
                    };

                } else {
                    PhotosGridView.Loaded += (s, e) => {
                        PhotosGridView.ScrollIntoView(_LastSelectedPhoto);

                        var item = (GridViewItem)PhotosGridView.ContainerFromItem(_LastSelectedPhoto);
                        if (item == null) { photoAnimation.Cancel(); return; }

                        var stack = (StackPanel)item.ContentTemplateRoot;
                        var image = (Image)stack.FindName("PhotoImage");
                        if (image == null) { photoAnimation.Cancel(); return; }

                        image.Opacity = 0;
                        image.Loaded += (_s, _e) => {
                            image.Opacity = 1;
                            photoAnimation.TryStart(image);
                        };
                    };
                }
            }
        }
        #endregion micro-interactions

        #region events
        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        private void PhotoItem_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            var photoItem = (StackPanel)sender;

            var data = (Photo)photoItem.DataContext;
            if (data == _LastSelectedPhoto) {
                photoItem.Fade(1).Start();
                _LastSelectedPhoto = null;
                return;
            }

            _AnimationDelay += 100;

            photoItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, _AnimationDelay)
                    .Offset(0, 0, 500, _AnimationDelay)
                    .Start();
        }

        private void PhotoItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var photo = (Photo)item.DataContext;
            _LastSelectedPhoto = photo;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                _IsGoingFoward = true;
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), new object[] { photo, _PageDataSource.CollectionPhotos });
        }

        private void PhotosListViewHeader_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            PivotCollection.SelectedIndex = 1;
        }

        private void PivotCollection_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            _LastSelectedPivotIndex = PivotCollection.SelectedIndex;
        }
        #endregion events

        #region micro-interactions

        private void UserView_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            ellipse.Scale(1.1f, 1.1f).Start();
        }

        private void UserView_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            ellipse.Scale(1f, 1f).Start();
        }

        #endregion micro-interactions

        #region commandbar
        void ApplyCommandBarBarFrostedGlass() {
            var glassHost = AppBarFrozenHost;
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


            glassHost.Offset(0, 27).Start();

            AppBar.Opening += (s, e) => {
                glassHost.Offset(0, 0).Start();
            };
            AppBar.Closing += (s, e) => {
                glassHost.Offset(0, 27).Start();
            };
        }

        private async void CmdOpenInBrowser_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (_CurrentCollection?.Links == null) return; // get info on question mark

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Credentials.ApplicationId;
            var userUri = new Uri(string.Format("{0}{1}", _CurrentCollection.Links.Html, tracking));
            var success = await Windows.System.Launcher.LaunchUriAsync(userUri);
        }

        private void CmdCopyLink_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (_CurrentCollection?.Links == null) return;

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Credentials.ApplicationId;
            var userUri = string.Format("{0}{1}", _CurrentCollection.Links.Html, tracking);
            DataTransfer.Copy(userUri);
        }

        #endregion commandbar

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
                _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
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

        #region rightTapped flyout
        void ShowProgress(string message = "") {
            ProgressDeterminate.Value = 0;
            FlyoutNotification.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(message)) return;
            FlyoutText.Text = message;
        }

        void HideProgress() {
            FlyoutNotification.Visibility = Visibility.Collapsed;
        }

        private void HttpProgressCallback(Windows.Web.Http.HttpProgress progress) {
            if (progress.TotalBytesToReceive == null) return;

            ProgressDeterminate.Minimum = 0;
            ProgressDeterminate.Maximum = (double)progress.TotalBytesToReceive;
            ProgressDeterminate.Value = progress.BytesReceived;
        }

        private void PhotoItem_RightTapped(object sender, RightTappedRoutedEventArgs e) {
            var panel = (StackPanel)sender;
            var photo = (Photo)panel.DataContext;

            _LastSelectedPhoto = photo;
            PhotoRightTappedFlyout.ShowAt(panel);
        }

        private void CmdCopyPhotoLink_Tapped(object sender, TappedRoutedEventArgs e) {
            var successMessage = App.ResourceLoader.GetString("CopyLinkSuccess");

            DataTransfer.Copy(_LastSelectedPhoto.Links.Html);
            Notify(successMessage);
        }

        private async void CmdSetAsWallpaper_Tapped(object sender, TappedRoutedEventArgs e) {
            var progressMessage = App.ResourceLoader.GetString("SettingWallpaper");
            var successMessage = App.ResourceLoader.GetString("WallpaperSetSuccess");
            var failedMessage = App.ResourceLoader.GetString("WallpaperSetFailed");

            ShowProgress(progressMessage);
            var success = await Wallpaper.SetAsWallpaper(_LastSelectedPhoto, HttpProgressCallback);
            HideProgress();

            if (success) Notify(successMessage);
            else Notify(failedMessage);
        }

        private async void CmdSetAsLockscreen_Tapped(object sender, TappedRoutedEventArgs e) {
            var progressMessage = App.ResourceLoader.GetString("SettingLockscreen");
            var successMessage = App.ResourceLoader.GetString("LockscreenSetSuccess");
            var failedMessage = App.ResourceLoader.GetString("LockscreenSetFailed");

            ShowProgress(progressMessage);
            var success = await Wallpaper.SetAsLockscreen(_LastSelectedPhoto, HttpProgressCallback);
            HideProgress();

            if (success) Notify(successMessage);
            else Notify(failedMessage);
        }

        private async void CmdOpenPhotoInBrowser_Tapped(object sender, TappedRoutedEventArgs e) {
            if (_LastSelectedPhoto == null || _LastSelectedPhoto.Links == null) return;

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Credentials.ApplicationId;
            var userUri = new Uri(string.Format("{0}{1}", _LastSelectedPhoto.Links.Html, tracking));
            var success = await Windows.System.Launcher.LaunchUriAsync(userUri);
        }

        private void CmdDownloadResolution_Tapped(object sender, TappedRoutedEventArgs e) {
            var cmd = (MenuFlyoutItem)sender;
            var resolution = (string)cmd.Tag;
            Download(resolution);
        }

        async void Download(string size = "") {
            ShowProgress();
            var result = false;

            if (string.IsNullOrEmpty(size)) {
                result = await Wallpaper.SaveToPicturesLibrary(_LastSelectedPhoto, HttpProgressCallback);

            } else {
                string url = getURL();
                result = await Wallpaper.SaveToPicturesLibrary(_LastSelectedPhoto, HttpProgressCallback, url);
            }

            HideProgress();

            var successMessage = App.ResourceLoader.GetString("SavePhotoSuccess");
            var failedMessage = App.ResourceLoader.GetString("SavePhotoFailed");

            if (result) Notify(successMessage);
            else Notify(failedMessage);

            string getURL()
            {
                switch (size) {
                    case "raw":
                        return _LastSelectedPhoto.Urls.Raw;
                    case "full":
                        return _LastSelectedPhoto.Urls.Full;
                    case "regular":
                        return _LastSelectedPhoto.Urls.Regular;
                    case "small":
                        return _LastSelectedPhoto.Urls.Small;
                    default:
                        return _LastSelectedPhoto.Urls.Regular;
                }
            }
        }

        #endregion rightTapped flyout
    }
}
