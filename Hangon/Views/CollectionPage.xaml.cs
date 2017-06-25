using Hangon.Data;
using Hangon.Models;
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

namespace Hangon.Views {
    public sealed partial class CollectionPage : Page {
        #region variables
        private DataSource PageDataSource { get; set; }

        private Collection CurrentCollection { get; set; }

        private double AnimationDelay { get; set; }

        private bool IsGoingFoward { get; set; }

        public static Photo LastPhotoSelected { get; set; }

        private static int LastPivotIndexSelected { get; set; }

        ResourceLoader _ResourcesLoader { get; set; }

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

            CurrentCollection = (Collection)e.Parameter;
            HandleConnectedAnimations(CurrentCollection);
            LoadData();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            if (!IsGoingFoward) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CollectionCoverImage", CollectionCoverImage);
            }

            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }
        #endregion navigation

        #region data
        private void InitializeVariables() {
            PageDataSource = App.AppDataSource;
            _ResourcesLoader = new ResourceLoader();
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void RestoreLastSelectedPivotIndex() {
            PivotCollection.SelectedIndex = LastPivotIndexSelected;
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
            TextTitle.Text = CurrentCollection.Title;
            TextDescription.Text = CurrentCollection.Description ?? "";
            TextPubDate.Text = DateTime
                .ParseExact(
                    CurrentCollection.PublishedAt, 
                    "MM/dd/yyyy HH:mm:ss", 
                    CultureInfo.InvariantCulture)
                .ToLocalTime()
                .ToString("dd MMMM yyyy");
        }

        private void LoadUserInfos() {
            UserName.Text = CurrentCollection.User.Name;
            UserLocation.Text = CurrentCollection.User.Location;
        }

        /// <summary>
        /// Fetch full collection's infos from the internet (INTERNET!)
        /// </summary>
        private async void LoadCompleteCollection() {
            CurrentCollection = await PageDataSource.GetCollection(CurrentCollection.Id);
        }

        private async void LoadCollectionPhotos() {
            await PageDataSource.GetCollectionPhotos(CurrentCollection.Id);

            if (PageDataSource.CollectionPhotos.Count > 0) { bindData(); } 
            else { showEmptyViews(); }

            void bindData()
            {
                PhotosListView.ItemsSource = PageDataSource.CollectionPhotos;
                PhotosGridView.ItemsSource = PageDataSource.CollectionPhotos;
                TextPhotosCount.Text = PageDataSource.CollectionPhotos.Count.ToString();
            }

            void showEmptyViews()
            {
                PhotosListViewHeader.Visibility = Visibility.Collapsed;
                PhotosListView.Visibility = Visibility.Collapsed;
                PhotosGridView.Visibility = Visibility.Collapsed;
                EmptyViewPhotos.Visibility = Visibility.Visible;
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

                CollectionCoverImage.Source = new BitmapImage(new Uri(CurrentCollection.CoverPhoto.Urls.Regular));
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
                
                UserImageSource.UriSource = new Uri(Unsplash.GetProfileImageLink(CurrentCollection.User));
            }

            void AnimatePhotoImage()
            {
                var photoAnimation = animationService.GetAnimation("PhotoImage");

                if (photoAnimation == null || LastPhotoSelected == null) return;

                if (LastPivotIndexSelected == 0) {
                    PhotosListView.Loaded += (s, e) => {
                        PhotosListView.ScrollIntoView(LastPhotoSelected);

                        var item = (ListViewItem)PhotosListView.ContainerFromItem(LastPhotoSelected);
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
                        PhotosGridView.ScrollIntoView(LastPhotoSelected);

                        var item = (GridViewItem)PhotosGridView.ContainerFromItem(LastPhotoSelected);
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
            if (data == LastPhotoSelected) {
                photoItem.Fade(1).Start();
                LastPhotoSelected = null;
                return;
            }

            AnimationDelay += 100;

            photoItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, AnimationDelay)
                    .Offset(0, 0, 500, AnimationDelay)
                    .Start();
        }

        private void PhotoItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var photo = (Photo)item.DataContext;
            LastPhotoSelected = photo;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                IsGoingFoward = true;
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), photo);
        }

        private void PhotosListViewHeader_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            PivotCollection.SelectedIndex = 1;
        }

        private void PivotCollection_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            LastPivotIndexSelected = PivotCollection.SelectedIndex;
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
            if (CurrentCollection?.Links == null) return; // get info on question mark

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Unsplash.ApplicationId;
            var userUri = new Uri(string.Format("{0}{1}", CurrentCollection.Links.Html, tracking));
            var success = await Windows.System.Launcher.LaunchUriAsync(userUri);
        }

        private void CmdCopyLink_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (CurrentCollection?.Links == null) return;

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Unsplash.ApplicationId;
            var userUri = string.Format("{0}{1}", CurrentCollection.Links.Html, tracking);
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

            LastPhotoSelected = photo;
            PhotoRightTappedFlyout.ShowAt(panel);
        }

        private void CmdCopyPhotoLink_Tapped(object sender, TappedRoutedEventArgs e) {
            var successMessage = _ResourcesLoader.GetString("CopyLinkSuccess");

            DataTransfer.Copy(LastPhotoSelected.Links.Html);
            Notify(successMessage);
        }

        private async void CmdSetAsWallpaper_Tapped(object sender, TappedRoutedEventArgs e) {
            var progressMessage = _ResourcesLoader.GetString("SettingWallpaper");
            var successMessage = _ResourcesLoader.GetString("WallpaperSetSuccess");
            var failedMessage = _ResourcesLoader.GetString("WallpaperSetFailed");

            ShowProgress(progressMessage);
            var success = await Wallpaper.SetAsWallpaper(LastPhotoSelected, HttpProgressCallback);
            HideProgress();

            if (success) Notify(successMessage);
            else Notify(failedMessage);
        }

        private async void CmdSetAsLockscreen_Tapped(object sender, TappedRoutedEventArgs e) {
            var progressMessage = _ResourcesLoader.GetString("SettingLockscreen");
            var successMessage = _ResourcesLoader.GetString("LockscreenSetSuccess");
            var failedMessage = _ResourcesLoader.GetString("LockscreenSetFailed");

            ShowProgress(progressMessage);
            var success = await Wallpaper.SetAsLockscreen(LastPhotoSelected, HttpProgressCallback);
            HideProgress();

            if (success) Notify(successMessage);
            else Notify(failedMessage);
        }

        private async void CmdOpenPhotoInBrowser_Tapped(object sender, TappedRoutedEventArgs e) {
            if (LastPhotoSelected == null || LastPhotoSelected.Links == null) return;

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Unsplash.ApplicationId;
            var userUri = new Uri(string.Format("{0}{1}", LastPhotoSelected.Links.Html, tracking));
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
                result = await Wallpaper.SaveToPicturesLibrary(LastPhotoSelected, HttpProgressCallback);

            } else {
                string url = getURL();
                result = await Wallpaper.SaveToPicturesLibrary(LastPhotoSelected, HttpProgressCallback, url);
            }

            HideProgress();

            var successMessage = _ResourcesLoader.GetString("SavePhotoSuccess");
            var failedMessage = _ResourcesLoader.GetString("SavePhotoFailed");

            if (result) Notify(successMessage);
            else Notify(failedMessage);

            string getURL()
            {
                switch (size) {
                    case "raw":
                        return LastPhotoSelected.Urls.Raw;
                    case "full":
                        return LastPhotoSelected.Urls.Full;
                    case "regular":
                        return LastPhotoSelected.Urls.Regular;
                    case "small":
                        return LastPhotoSelected.Urls.Small;
                    default:
                        return LastPhotoSelected.Urls.Regular;
                }
            }
        }

        #endregion rightTapped flyout
    }
}
