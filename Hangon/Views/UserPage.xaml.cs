﻿using Hangon.Data;
using Hangon.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI.Composition;
using Windows.UI;
using Windows.ApplicationModel.Resources;
using System.Threading;
using Unsplasharp.Models;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;

namespace Hangon.Views {
    public sealed partial class UserPage : Page {
        #region variables
        private DataSource _PageDataSource { get; set; }

        private Photo _CurrentPhoto { get; set; }

        private User _CurrentUser { get; set; }

        public static Photo _LastSelectedPhoto { get; set; }

        private static Collection _LastCollectionSelected { get; set; }

        private static int _LastPivotIndexSelected { get; set; }

        private double _AnimationDelay { get; set; }

        private double _CollectionAnimationDelay { get; set; }

        private double _MiniCollectionAnimeDelay { get; set; }

        ResourceLoader _ResourcesLoader { get; set; }

        //Photo _LastSelectedPhoto { get; set; }

        private CoreDispatcher _UIDispatcher { get; set; }

        #endregion variables

        public UserPage() {
            InitializeComponent();
            InitializeVariables();
            InitializeTitleBar();
            ApplyCommandBarBarFrostedGlass();
        }
        

        private void RestoreLastSelectedPivotIndex() {
            PivotUserData.SelectedIndex = _LastPivotIndexSelected;
        }

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            if (e.NavigationMode == NavigationMode.Back) {
                _LastPivotIndexSelected = 0;
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", ImageBackground);
                PhotoPage._CurrentPhoto = _CurrentPhoto; // re-select last selected photo
            }
            
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            _CurrentPhoto = (Photo)e.Parameter;
            _CurrentUser = _CurrentPhoto.User;

            RestoreLastSelectedPivotIndex();

            base.OnNavigatedTo(e);
        }

        private void CmdGoHome_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(HomePage));
        }

        #endregion navigation

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

        #region data
        private void InitializeVariables() {
            _PageDataSource = App.DataSource;
            _ResourcesLoader = new ResourceLoader();
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void ClearData() {
            _PageDataSource.UserPhotos.Clear();
        }

        private void InitializeData() {
            LoadUserData();
            LoadUserPhotos();
            LoadUserCollections();
        }
        
        private void LoadUserData() {
            UserInfosPivotItem.FindName("UserView");
            PopulateCachedStats();

            PopulateStats();

            void PopulateCachedStats()
            {
                UserName.Text = _PageDataSource.GetUsernameFormated(_CurrentPhoto.User);
                UserLocation.Text = _CurrentPhoto.User.Location ?? "";

                if (string.IsNullOrEmpty(UserLocation.Text)) {
                    UserLocationPanel.Visibility = Visibility.Collapsed;
                }
            }

            void PopulateStats()
            {
                PhotosCount.Text = _CurrentUser.TotalPhotos.ToString();
                LikesCount.Text = _CurrentUser.TotalLikes.ToString();
                CollectionsCount.Text = _CurrentUser.TotalCollections.ToString();
                UserBioView.Text = _CurrentUser.Bio ?? "";
            }
            
        }

        private async void LoadUserPhotos() {
            UserPhotosPivotItem.FindName("UserPhotosPivotItemContent");

            await _PageDataSource.GetUserPhotos(_PageDataSource.GetUsername(_CurrentPhoto.User));
            UserPhotosGridView.ItemsSource = _PageDataSource.UserPhotos;

            BindPhotosListView();

            void BindPhotosListView()
            {
                if (UserPhotosListView != null) {
                    UserPhotosListView.ItemsSource = _PageDataSource.UserPhotos;
                }
            }
        }

        private async void LoadUserCollections() {
            UserCollectionsPivotItem.FindName("UserCollectionsPivotItemContent");

            await _PageDataSource.GetUserCollections(_PageDataSource.GetUsername(_CurrentPhoto.User));

            if (_PageDataSource.UserCollections.Count > 0) {
                UserCollectionsGrid.ItemsSource = _PageDataSource.UserCollections;

            } else {
                UserCollectionsGrid.Visibility = Visibility.Collapsed;
                CollectionEmptyView.Visibility = Visibility.Visible;
            }

            BindCollectionListView();

            void BindCollectionListView()
            {
                if (UserCollectionsListView != null) {
                    UserCollectionsListView.ItemsSource = _PageDataSource.UserCollections;

                    if (_PageDataSource.UserCollections.Count == 0) {
                        UserCollectionsListView.Visibility = Visibility.Collapsed;
                        UserCollectionsListViewHeader.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        #endregion data
        
        #region micro-interactions

        private void HandleConnectedAnimation(Photo photo) {
            if (photo == null) return;

            var animationService = ConnectedAnimationService.GetForCurrentView();

            AnimateProfileImage();
            AnimateBackground();
            AnimateCollectionCover();
            AnimatePhotoCover();


            void AnimateProfileImage()
            {
                var profileAnimation = animationService.GetAnimation("UserProfileImage");

                if (UserImageSource == null || PivotUserData.SelectedIndex != 0) {
                    profileAnimation?.Cancel();
                    return;
                }

                if (profileAnimation != null) {
                    UserProfileImage.Opacity = 0; // TODO: check opacity effect on animation
                    UserImageSource.ImageOpened += (s, e) => {
                        UserProfileImage.Opacity = 1;
                        profileAnimation.TryStart(UserProfileImage);
                    };
                }

                UserImageSource.UriSource = new Uri(_PageDataSource.GetProfileImageLink(_CurrentUser));
            }

            void AnimateBackground()
            {
                var backgroundAnimation = animationService.GetAnimation("PhotoImage");

                if (backgroundAnimation != null) {
                    ImageBackground.Opacity = 0;
                    ImageBackground.ImageOpened += (s, e) => {
                        ImageBackground.Opacity = .6;
                        backgroundAnimation.TryStart(ImageBackground);
                        BackgroundBlurEffect.Blur(10, 1000, 1000).Start();
                    };

                } else {
                    ImageBackground.Fade(.6f).Start();
                    BackgroundBlurEffect.Blur(10, 1000, 1000).Start();
                }

                ImageBackground.Source = new BitmapImage(new Uri(photo.Urls.Regular));
            }

            void AnimatePhotoCover()
            {
                var photoAnimation = animationService.GetAnimation("PhotoImageBack");

                if (photoAnimation == null || _LastSelectedPhoto == null) {
                    photoAnimation?.Cancel();
                    return;
                }

                if (_LastPivotIndexSelected == 0) {
                    UserPhotosListView.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserPhotosListView, _LastSelectedPhoto, photoAnimation);
                    };

                } else if (_LastPivotIndexSelected == 1) {
                    UserPhotosGridView.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserPhotosGridView, _LastSelectedPhoto, photoAnimation);
                    };
                }
            }

            void AnimateCollectionCover()
            {
                var collectionCoverAnimation = animationService.GetAnimation("CollectionCoverImage");

                if (collectionCoverAnimation == null || _LastCollectionSelected == null) {
                    collectionCoverAnimation?.Cancel();
                    return;
                }

                if (_LastPivotIndexSelected == 0) {
                    UserCollectionsListView.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserCollectionsListView, _LastCollectionSelected, collectionCoverAnimation);
                    };

                } else if (_LastPivotIndexSelected == 2) {
                    UserCollectionsGrid.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserCollectionsGrid, _LastCollectionSelected, collectionCoverAnimation);
                    };
                }
            }
        }

        private void UserView_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            ellipse.Scale(1.1f, 1.1f).Start();
        }

        private void UserView_PointerExited(object sender, PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            ellipse.Scale(1f, 1f).Start();
        }

        private void PhotoItem_PointerEntered(object sender, PointerRoutedEventArgs e) {
            var panel = (StackPanel)sender;
            var image = (Image)panel.FindName("PhotoImage");

            if (image == null) return;

            image.Scale(1f, 1f).Start();
        }

        private void PhotoItem_PointerExited(object sender, PointerRoutedEventArgs e) {
            var panel = (StackPanel)sender;
            var image = (Image)panel.FindName("PhotoImage");

            if (image == null) return;

            image.Scale(1f, 1f).Start();
        }

        private void CollectionItem_PointerEntered(object sender, PointerRoutedEventArgs e) {
            var panel = (Grid)sender;
            var image = (Image)panel.FindName("PhotoImage");

            if (image == null) return;

            image.Scale(1.1f, 1.1f).Start();
        }

        private void CollectionItem_PointerExited(object sender, PointerRoutedEventArgs e) {
            var panel = (Grid)sender;
            var image = (Image)panel.FindName("PhotoImage");

            if (image == null) return;

            image.Scale(1f, 1f).Start();
        }

        #endregion micro-interactions

        #region CommandBar
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

            PageCommandBar.Opening += (s, e) => {
                glassHost.Offset(0, 0).Start();
            };

            PageCommandBar.Closing += (s, e) => {
                glassHost.Offset(0, 27).Start();
            };
        }

        private async void CmdOpenInBrowser_Tapped(object sender, TappedRoutedEventArgs e) {
            if (_CurrentUser == null || _CurrentUser.Links == null) return;

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Credentials.ApplicationId;
            var userUri = new Uri(string.Format("{0}{1}", _CurrentUser.Links.Html, tracking));
            var success = await Windows.System.Launcher.LaunchUriAsync(userUri);
        }

        private void CmdCopyLink_Tapped(object sender, TappedRoutedEventArgs e) {
            if (_CurrentUser?.Links == null) return;

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Credentials.ApplicationId;
            var userUri = string.Format("{0}{1}", _CurrentUser.Links.Html, tracking);
            DataTransfer.Copy(userUri);

            var successMessage = _ResourcesLoader.GetString("CopyLinkSuccess");
            Notify(successMessage);
        }

        private void CmdRefresh_Tapped(object sender, TappedRoutedEventArgs e) {
            ClearData();
            InitializeData();
        }

        #endregion CommandBar

        #region events
        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        private void PhotoItem_Loaded(object sender, RoutedEventArgs e) {
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

        private void UserPhotosListViewHeader_Tapped(object sender, TappedRoutedEventArgs e) {
            PivotUserData.SelectedIndex = 1;
        }

        private void UserCollectionsListViewHeader_Tapped(object sender, TappedRoutedEventArgs e) {
            PivotUserData.SelectedIndex = 2;
        }

        private void CollectionItem_Loaded(object sender, RoutedEventArgs e) {
            var collectionItem = (Grid)sender;

            var data = (Collection)collectionItem.DataContext;
            if (data == _LastCollectionSelected) {
                collectionItem.Fade(1).Start();
                _LastCollectionSelected = null;
                return;
            }

            _CollectionAnimationDelay += 100;

            collectionItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, _CollectionAnimationDelay)
                    .Offset(0, 0, 500, _CollectionAnimationDelay)
                    .Start();
        }

        private void PhotoItem_Tapped(object sender, TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var photo = (Photo)item.DataContext;

            _LastSelectedPhoto = photo;
            _LastCollectionSelected = null;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), new object[] { photo, _PageDataSource.UserPhotos, this.GetType() });
        }

        private void CollectionItem_Tapped(object sender, TappedRoutedEventArgs e) {
            var item = (Grid)sender;
            var collection = (Collection)item.DataContext;

            _LastCollectionSelected = collection;
            _LastSelectedPhoto = null;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CollectionCoverImage", image);
            }

            Frame.Navigate(typeof(CollectionPage), collection);
        }

        private void MiniCollectionItem_Loaded(object sender, RoutedEventArgs e) {
            var collectionItem = (Grid)sender;

            var data = (Collection)collectionItem.DataContext;
            if (data == _LastCollectionSelected) {
                collectionItem.Fade(1).Start();
                _LastCollectionSelected = null;
                return;
            }

            _MiniCollectionAnimeDelay += 100;

            collectionItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, _MiniCollectionAnimeDelay)
                    .Offset(0, 0, 500, _MiniCollectionAnimeDelay)
                    .Start();
        }

        private void PivotUserData_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            _LastPivotIndexSelected = PivotUserData.SelectedIndex;

            var task = App.DataSource.LoadLocalFavorites();

            switch (PivotUserData.SelectedIndex) {
                case 0:
                    InitializeData();
                    break;
                case 1:
                    LoadUserPhotos();
                    break;
                case 2:
                    LoadUserCollections();
                    break;
                default:
                    break;
            }

            HandleConnectedAnimation(_CurrentPhoto);
        }


        #endregion events

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
                await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
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

            CheckIfPhotoInFavorites(photo);

            PhotoRightTappedFlyout.ShowAt(panel);
        }

        private void CheckIfPhotoInFavorites(Photo photo) {
            if (App.DataSource.LocalFavorites == null) {
                RightCmdRemoveFavorites.Visibility = Visibility.Collapsed;
                RightCmdAddToFavorites.Visibility = Visibility.Collapsed;
                return;
            }

            if (App.DataSource.LocalFavorites.Contains(photo.Id)) {
                RightCmdRemoveFavorites.Visibility = Visibility.Visible;
                RightCmdAddToFavorites.Visibility = Visibility.Collapsed;
                return;
            }

            RightCmdRemoveFavorites.Visibility = Visibility.Collapsed;
            RightCmdAddToFavorites.Visibility = Visibility.Visible;
        }


        private void CmdCopyPhotoLink_Tapped(object sender, TappedRoutedEventArgs e) {
            var successMessage = _ResourcesLoader.GetString("CopyLinkSuccess");

            DataTransfer.Copy(_LastSelectedPhoto.Links.Html);
            Notify(successMessage);
        }

        private async void CmdSetAsWallpaper_Tapped(object sender, TappedRoutedEventArgs e) {
            var progressMessage = _ResourcesLoader.GetString("SettingWallpaper");
            var successMessage = _ResourcesLoader.GetString("WallpaperSetSuccess");
            var failedMessage = _ResourcesLoader.GetString("WallpaperSetFailed");

            ShowProgress(progressMessage);
            var success = await Wallpaper.SetAsWallpaper(_LastSelectedPhoto, HttpProgressCallback);
            HideProgress();

            if (success) Notify(successMessage);
            else Notify(failedMessage);
        }

        private async void CmdSetAsLockscreen_Tapped(object sender, TappedRoutedEventArgs e) {
            var progressMessage = _ResourcesLoader.GetString("SettingLockscreen");
            var successMessage = _ResourcesLoader.GetString("LockscreenSetSuccess");
            var failedMessage = _ResourcesLoader.GetString("LockscreenSetFailed");

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

            var successMessage = _ResourcesLoader.GetString("SavePhotoSuccess");
            var failedMessage = _ResourcesLoader.GetString("SavePhotoFailed");

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

        private void RightCmdAddToFavorites_Tapped(object sender, TappedRoutedEventArgs e) {
            var localFavorites = App.DataSource.LocalFavorites;
            if (localFavorites == null) return;

            var cmd = (MenuFlyoutItem)sender;
            var photo = (Photo)cmd.DataContext;

            if (photo == null || string.IsNullOrEmpty(photo.Id)) {
                DataTransfer.ShowLocalToast(App.ResourceLoader.GetString("PhotoNotFound"));
                return;
            }

            if (localFavorites.Contains(photo.Id)) {
                DataTransfer.ShowLocalToast(App.ResourceLoader.GetString("PhotoAlreadyInFavorites"));
                return;
            }

            var task = App.DataSource.AddToFavorites(photo);

            // TODO: Notify add
            var message = App.ResourceLoader.GetString("PhotoSuccessfulAddedToFavorites");
            Notify(message);
        }

        private void RightCmdRemoveFavorites_Tapped(object sender, TappedRoutedEventArgs e) {
            var localFavorites = App.DataSource.LocalFavorites;
            if (localFavorites == null) return;

            var cmd = (MenuFlyoutItem)sender;
            var photo = (Photo)cmd.DataContext;

            if (photo == null || string.IsNullOrEmpty(photo.Id)) {
                DataTransfer.ShowLocalToast(App.ResourceLoader.GetString("PhotoNotFound"));
                return;
            }

            var task = App.DataSource.RemoveFromFavorites(photo);

            // TODO: Notify removed
            var message = App.ResourceLoader.GetString("PhotoSuccessfulRemovedFromFavorites");
            Notify(message);
        }

        #endregion rightTapped flyout
        
    }
}
