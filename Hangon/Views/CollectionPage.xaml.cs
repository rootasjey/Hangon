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

namespace Hangon.Views {
    public sealed partial class CollectionPage : Page {
        #region variables
        private DataSource PageDataSource { get; set; }

        private Collection CurrentCollection { get; set; }

        private double AnimationDelay { get; set; }

        private bool IsGoingFoward { get; set; }

        public static Photo LastPhotoSelected { get; set; }

        private static int LastPivotIndexSelected { get; set; }
        #endregion variables

        public CollectionPage() {
            InitializeComponent();
            PageDataSource = App.AppDataSource;
            RestoreLastSelectedPivotIndex();
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

    }
}
