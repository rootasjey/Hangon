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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

namespace Hangon.Views {
    public sealed partial class UserPage : Page {
        #region variables
        private DataSource PageDataSource { get; set; }
        private Photo CurrentPhoto { get; set; }
        private User CurrentUser { get; set; }

        private static Photo LastPhotoSelected { get; set; }

        private static Collection LastCollectionSelected { get; set; }

        private double _AnimationDelay { get; set; }

        private double _CollectionAnimationDelay { get; set; }

        private bool UserStatsCollapsed { get; set; }

        #endregion variables

        public UserPage() {
            InitializeComponent();
            PageDataSource = App.AppDataSource;
        }

        #region navigation
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("WallpaperImage", ImageBackground);
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            var photo = (Photo)e.Parameter;
            CurrentPhoto = photo;

            HandleConnectedAnimation(CurrentPhoto);

            LoadData(); // always fires after constructor?

            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        private void PhotoItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var photo = (Photo)item.DataContext;
            LastPhotoSelected = photo;

            var image = (Image)item.FindName("WallpaperImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("WallpaperImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), photo);
        }

        private void CollectionItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            LastCollectionSelected = (Collection)((StackPanel)sender).DataContext;
        }
        #endregion navigation

        private void HandleConnectedAnimation(Photo photo) {
            if (photo == null) return;

            var animationService = ConnectedAnimationService.GetForCurrentView();

            UserName.Text = photo.User.Name;
            UserLocation.Text = photo.User.Location ?? "";

            AnimateProfileImage();
            AnimateBackground();

            // FUNCTION DEFINITIONS
            // --------------------
            void AnimateProfileImage() {
                var profileAnimation = animationService.GetAnimation("UserProfileImage");

                if (profileAnimation != null) {
                    UserProfileImage.Opacity = 0;
                    UserImageSource.ImageOpened += (s, e) => {
                        UserProfileImage.Opacity = 1;
                        profileAnimation.TryStart(UserProfileImage);
                    };
                }

                UserImageSource.UriSource = new Uri(photo.User.ProfileImage.Medium);

            }

            void AnimateBackground()
            {
                var backgroundAnimation = animationService.GetAnimation("WallpaperImage");
                if (backgroundAnimation != null) {
                    ImageBackground.Opacity = 0;
                    ImageBackground.ImageOpened += (s, e) => {
                        ImageBackground.Opacity = .6;
                        backgroundAnimation.TryStart(ImageBackground);
                        BackgroundBlurEffect.Blur(10, 1000, 1000).Start();
                    };
                }

                ImageBackground.Source = new BitmapImage(new Uri(photo.Urls.Regular));
            }
        }

        #region data
        private void ClearData() {
            PageDataSource.UserPhotos.Clear();
        }

        private void LoadData() {
            LoadStats();
            LoadPhotos();
            //LoadCollections();
        }

        private async void LoadStats() {
            CurrentUser = await PageDataSource.GetUser(CurrentPhoto.User.Username);

            PhotosCount.Text = CurrentUser.TotalPhotos.ToString();
            LikesCount.Text = CurrentUser.TotalLikes.ToString();
            CollectionsCount.Text = CurrentUser.TotalCollections.ToString();
            UserBioView.Text = CurrentUser.Bio;
        }

        private async void LoadPhotos() {
            await PageDataSource.GetUserPhotos(CurrentPhoto.User.Username);
            UserPhotosGrid.ItemsSource = PageDataSource.UserPhotos;
        }

        private async void LoadCollections() {
            if (PageDataSource.UserCollections != null &&
                PageDataSource.UserCollections.Count > 0) return;

            var results = await PageDataSource.GetUserCollections(CurrentPhoto.User.Username);

            if (results > 0) {
                UserCollectionsGrid.ItemsSource = PageDataSource.UserCollections;
            } else {
                UserCollectionsGrid.Visibility = Visibility.Collapsed;
                CollectionEmptyView.Visibility = Visibility.Visible;
            }
        }

        private void CollectionItem_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            var collectionItem = (StackPanel)sender;

            //var data = (Photo)photoItem.DataContext;
            //if (data == _LastPhotoSelected) {
            //    photoItem.Fade(1).Start();
            //    return;
            //}

            _AnimationDelay += 100;

            collectionItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, _CollectionAnimationDelay)
                    .Offset(0, 0, 500, _CollectionAnimationDelay)
                    .Start();
        }

        private void UserData_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var index = PivotUserData.SelectedIndex;

            switch (index) {
                case 0:
                    break;
                case 1:
                    LoadCollections();
                    break;
                case 2:
                    break;
                default:
                    break;
            }
        }
        #endregion data

        private void PhotoItem_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            var photoItem = (StackPanel)sender;

            var data = (Photo)photoItem.DataContext;
            if (data == LastPhotoSelected) {
                photoItem.Fade(1).Start();
                return;
            }

            _AnimationDelay += 100;

            photoItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, _AnimationDelay)
                    .Offset(0, 0, 500, _AnimationDelay)
                    .Start();
        }

        #region micro-interactions
        private void UserView_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            ellipse.Scale(1.1f, 1.1f).Start();
        }

        private void UserView_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var ellipse = (Ellipse)sender;
            ellipse.Scale(1f, 1f).Start();
        }

        private void Image_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var image = (Image)sender;
            image.Scale(1.1f, 1.1f).Start();
        }

        private void Image_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var image = (Image)sender;
            image.Scale(1f, 1f).Start();
        }

        private void ShadowPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {

        }

        private void ShadowPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {

        }
        #endregion micro-interactions

        #region CommandBar
        private async void CmdOpenInBrowser_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (CurrentUser?.Links == null) return; // get info on question mark

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Unsplash.ApplicationId;
            var userUri = new Uri(string.Format("{0}{1}", CurrentUser.Links.Html, tracking));
            var success = await Windows.System.Launcher.LaunchUriAsync(userUri);
        }

        private void CmdCopyLink_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (CurrentUser?.Links == null) return;

            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Unsplash.ApplicationId;
            var userUri = string.Format("{0}{1}", CurrentUser.Links.Html, tracking);
            DataTransfer.Copy(userUri);
        }

        private void CmdRefresh_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            ClearData();
            LoadData();
        }

        #endregion CommandBar


        private void ToggleUserViewVisibility_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            //ToggleUserViewVisibility.Rotate(180).Start();
        }
    }
}
