using Hangon.Data;
using Hangon.Models;
using Hangon.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;

namespace Hangon.Views {
    public sealed partial class HomePage : Page {
        private DataSource PageDataSource { get; set; }

        float _AnimationDelay { get; set; }

        static Photo _LastSelectedWallpaper { get; set; }

        bool _BlockLoadedAnimation { get; set; }

        public HomePage() {
            InitializeComponent();

            StartNavigatinToAnimation();
            BindData();
            LoadData();
        }

        #region navigation

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            //_LastSelectedPivotItem = HomePivot.SelectedIndex;
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;
            //RestorePivotPosition();
            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion navigation

        void StartNavigatinToAnimation() {
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("WallpaperImage");
            if (animation != null && _LastSelectedWallpaper != null) {
                _BlockLoadedAnimation = true;

                RecentView.Loaded += (s, e) => {
                    RecentView.ScrollIntoView(_LastSelectedWallpaper);
                    var item = (GridViewItem)RecentView.ContainerFromItem(_LastSelectedWallpaper);
                    if (item == null) return;

                    var stack = (StackPanel)item.ContentTemplateRoot;
                    var image = (Image)stack.FindName("WallpaperImage");
                    if (image == null) return;

                    image.Opacity = 0;
                    image.Loaded += (_s, _e) => {
                        image.Opacity = 1;
                        animation.TryStart(image);
                    };                    
                };

                return;
            }
        }

        private  void BindData() {
            if (App.AppDataSource == null) {
                App.AppDataSource = new DataSource();
            }

            PageDataSource = App.AppDataSource;
        }

        private async void LoadData() {
            if (PageDataSource.NewPhotos?.Count > 0) {
                RecentView.ItemsSource = PageDataSource.NewPhotos;
                return;
            }

            ShowLoadingView();

            var added = await PageDataSource.FetchRecent();

            HideLoadingView();

            if (added>0) {
                RecentView.ItemsSource = PageDataSource.NewPhotos;
            } else {
                EmptyView.Visibility = Visibility.Visible;
            }

            void ShowLoadingView()
            {
                LoadingView.Visibility = Visibility.Visible;
            }

            void HideLoadingView()
            {
                LoadingView.Visibility = Visibility.Collapsed;
            }
        }

        private void PhotoItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var wallpaper = (Photo)item.DataContext;
            _LastSelectedWallpaper = wallpaper;

            var image = (Image)item.FindName("WallpaperImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("WallpaperImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), wallpaper);
        }

        private void PhotoItem_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            var wallItem = (StackPanel)sender;

            var data = (Photo)wallItem.DataContext;
            if (data == _LastSelectedWallpaper) {
                wallItem.Fade(1).Start();
                return;
            }

            _AnimationDelay += 100;

            wallItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, _AnimationDelay)
                    .Offset(0,0, 500, _AnimationDelay)
                    .Start();
        }

        #region micro-interactions
        private void Image_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var image = (Image)sender;
            image.Scale(1.1f, 1.1f).Start();
        }

        private void Image_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var image = (Image)sender;
            image.Scale(1f, 1f).Start();
        }

        private void ShadowPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var panel = (DropShadowPanel)sender;
            panel.ShadowOpacity = .1;
        }

        private void ShadowPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var panel = (DropShadowPanel)sender;
            panel.ShadowOpacity = 0;
        }

        #endregion micro-interactions

        #region commandbar
        private void CmdSettings_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(SettingsPage));
        }

        private void CmdRefresh_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            PageDataSource.NewPhotos.Clear();
            LoadData();
        }
        #endregion commandbar

    }
}
