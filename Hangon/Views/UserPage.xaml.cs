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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI.Composition;
using Windows.UI;

namespace Hangon.Views {
    public sealed partial class UserPage : Page {
        #region variables
        private DataSource PageDataSource { get; set; }

        private Photo CurrentPhoto { get; set; }

        private User CurrentUser { get; set; }

        private static Photo LastPhotoSelected { get; set; }

        private static Collection LastCollectionSelected { get; set; }

        private static int LastPivotIndexSelected { get; set; }

        private double AnimationDelay { get; set; }

        private double CollectionAnimationDelay { get; set; }

        private double MiniCollectionAnimeDelay { get; set; }

        #endregion variables

        public UserPage() {
            InitializeComponent();
            ApplyCommandBarBarFrostedGlass();
            PageDataSource = App.AppDataSource;
        }

        private void RestoreLastSelectedPivotIndex() {
            PivotUserData.SelectedIndex = LastPivotIndexSelected;
        }

        #region navigation
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            if (e.NavigationMode == NavigationMode.Back) {
                LastPivotIndexSelected = 0;
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", ImageBackground);
            }
            
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            CurrentPhoto = (Photo)e.Parameter;
            CurrentUser = CurrentPhoto.User;

            RestoreLastSelectedPivotIndex();

            base.OnNavigatedTo(e);
        }
        #endregion navigation


        #region data
        private void ClearData() {
            PageDataSource.UserPhotos.Clear();
        }

        private void LoadData() {
            LoadUserData();
            LoadUserPhotos();
            LoadUserCollections();
        }
        
        private async void LoadUserData() {
            UserInfosPivotItem.FindName("UserView");
            PopulateCachedStats();

            CurrentUser = await PageDataSource.GetUser(CurrentPhoto.User.Username);

            PopulateStats();
            BindDataList();

            void PopulateCachedStats()
            {
                UserName.Text = CurrentPhoto.User.Name;
                UserLocation.Text = CurrentPhoto.User.Location ?? "";

                if (string.IsNullOrEmpty(UserLocation.Text)) {
                    UserLocationPanel.Visibility = Visibility.Collapsed;
                }
            }

            void PopulateStats()
            {
                PhotosCount.Text = CurrentUser.TotalPhotos.ToString();
                LikesCount.Text = CurrentUser.TotalLikes.ToString();
                CollectionsCount.Text = CurrentUser.TotalCollections.ToString();
                UserBioView.Text = CurrentUser.Bio ?? "";
            }

            void BindDataList()
            {
                UserPhotosListView.ItemsSource = PageDataSource.UserPhotos;
                UserCollectionsListView.ItemsSource = PageDataSource.UserCollections;

                if (PageDataSource.UserCollections.Count == 0) {
                    UserCollectionsListView.Visibility = Visibility.Collapsed;
                    UserCollectionsListViewHeader.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void LoadUserPhotos() {
            UserPhotosPivotItem.FindName("UserPhotosPivotItemContent");

            await PageDataSource.GetUserPhotos(CurrentPhoto.User.Username);
            UserPhotosGridView.ItemsSource = PageDataSource.UserPhotos;
        }

        private async void LoadUserCollections() {
            UserCollectionsPivotItem.FindName("UserCollectionsPivotItemContent");

            await PageDataSource.GetUserCollections(CurrentPhoto.User.Username);

            if (PageDataSource.UserCollections.Count > 0) {
                UserCollectionsGrid.ItemsSource = PageDataSource.UserCollections;

            } else {
                UserCollectionsGrid.Visibility = Visibility.Collapsed;
                CollectionEmptyView.Visibility = Visibility.Visible;
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

                UserImageSource.UriSource = new Uri(Unsplash.GetProfileImageLink(CurrentUser));
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

                if (photoAnimation == null || LastPhotoSelected == null) {
                    photoAnimation?.Cancel();
                    return;
                }

                if (LastPivotIndexSelected == 0) {
                    UserPhotosListView.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserPhotosListView, LastPhotoSelected, photoAnimation);
                    };

                } else if (LastPivotIndexSelected == 1) {
                    UserPhotosGridView.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserPhotosGridView, LastPhotoSelected, photoAnimation);
                    };
                }
            }

            void AnimateCollectionCover()
            {
                var collectionCoverAnimation = animationService.GetAnimation("CollectionCoverImage");

                if (collectionCoverAnimation == null || LastCollectionSelected == null) {
                    collectionCoverAnimation?.Cancel();
                    return;
                }

                if (LastPivotIndexSelected == 0) {
                    UserCollectionsListView.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserCollectionsListView, LastCollectionSelected, collectionCoverAnimation);
                    };

                } else if (LastPivotIndexSelected == 2) {
                    UserCollectionsGrid.Loaded += (s, e) => {
                        UI.AnimateBackItemToList(UserCollectionsGrid, LastCollectionSelected, collectionCoverAnimation);
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

            AppBar.Opening += (s, e) => {
                glassHost.Offset(0, 0).Start();
            };
            AppBar.Closing += (s, e) => {
                glassHost.Offset(0, 27).Start();
            };
        }

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

        #region events
        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        private void PhotoItem_Loaded(object sender, RoutedEventArgs e) {
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

        private void UserPhotosListViewHeader_Tapped(object sender, TappedRoutedEventArgs e) {
            PivotUserData.SelectedIndex = 1;
        }

        private void UserCollectionsListViewHeader_Tapped(object sender, TappedRoutedEventArgs e) {
            PivotUserData.SelectedIndex = 2;
        }

        private void CollectionItem_Loaded(object sender, RoutedEventArgs e) {
            var collectionItem = (Grid)sender;

            var data = (Collection)collectionItem.DataContext;
            if (data == LastCollectionSelected) {
                collectionItem.Fade(1).Start();
                LastCollectionSelected = null;
                return;
            }

            CollectionAnimationDelay += 100;

            collectionItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, CollectionAnimationDelay)
                    .Offset(0, 0, 500, CollectionAnimationDelay)
                    .Start();
        }

        private void PhotoItem_Tapped(object sender, TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var photo = (Photo)item.DataContext;

            LastPhotoSelected = photo;
            LastCollectionSelected = null;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), photo);
        }

        private void CollectionItem_Tapped(object sender, TappedRoutedEventArgs e) {
            var item = (Grid)sender;
            var collection = (Collection)item.DataContext;

            LastCollectionSelected = collection;
            LastPhotoSelected = null;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CollectionCoverImage", image);
            }

            Frame.Navigate(typeof(CollectionPage), collection);
        }

        private void MiniCollectionItem_Loaded(object sender, RoutedEventArgs e) {
            var collectionItem = (Grid)sender;

            var data = (Collection)collectionItem.DataContext;
            if (data == LastCollectionSelected) {
                collectionItem.Fade(1).Start();
                LastCollectionSelected = null;
                return;
            }

            MiniCollectionAnimeDelay += 100;

            collectionItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, MiniCollectionAnimeDelay)
                    .Offset(0, 0, 500, MiniCollectionAnimeDelay)
                    .Start();
        }

        private void PivotUserData_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            LastPivotIndexSelected = PivotUserData.SelectedIndex;

            switch (PivotUserData.SelectedIndex) {
                case 0:
                    LoadData();
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

            HandleConnectedAnimation(CurrentPhoto);
        }


        #endregion events

    }
}
