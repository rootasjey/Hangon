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
using Windows.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI;
using Windows.UI.Composition;

namespace Hangon.Views {
    public sealed partial class HomePage : Page {
        #region variables
        private DataSource PageDataSource { get; set; }

        float _AnimationDelay { get; set; }

        static Photo _LastSelectedWallpaper { get; set; }

        bool _BlockLoadedAnimation { get; set; }
        #endregion variables

        public HomePage() {
            InitializeComponent();
            ApplyCommandBarBarFrostedGlass();
            StartNavigationToAnimation();
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

        void StartNavigationToAnimation() {
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PhotoImage");
            if (animation != null && _LastSelectedWallpaper != null) {
                _BlockLoadedAnimation = true;

                RecentView.Loaded += (s, e) => {
                    RecentView.ScrollIntoView(_LastSelectedWallpaper);
                    var item = (GridViewItem)RecentView.ContainerFromItem(_LastSelectedWallpaper);
                    if (item == null) return;

                    var control = (UserControl)item.ContentTemplateRoot;
                    var image = (Image)control.FindName("PhotoImage");
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

        #region data
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

        #endregion data 

        #region events
        private void PhotoItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var wallpaper = (Photo)item.DataContext;
            _LastSelectedWallpaper = wallpaper;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), wallpaper);
        }

        private void PhotoItem_Loaded(object sender, RoutedEventArgs e) {
            var photoItem = (StackPanel)sender;

            var data = (Photo)photoItem.DataContext;
            if (data == _LastSelectedWallpaper) {
                photoItem.Fade(1).Start();
                return;
            }

            _AnimationDelay += 100;

            photoItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, _AnimationDelay)
                    .Offset(0,0, 500, _AnimationDelay)
                    .Start();
        }
        #endregion events

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


            glassHost.Offset(0, 35).Start();

            AppBar.Opening += (s, e) => {
                glassHost.Offset(0, 0).Start();
            };
            AppBar.Closing += (s, e) => {
                glassHost.Offset(0, 35).Start();
            };
        }

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
