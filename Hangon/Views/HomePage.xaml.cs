using System;
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
using Windows.UI.Xaml.Input;
using System.Threading;

namespace Hangon.Views {
    public sealed partial class HomePage : Page {
        #region variables
        private DataSource PageDataSource { get; set; }

        float _RecentAnimationDelay { get; set; }

        float _CuratedAnimationDelay { get; set; }

        float _SearchAnimationDelay { get; set; }

        static Photo _LastSelectedPhoto { get; set; }

        bool _BlockLoadedAnimation { get; set; }

        private static int _LastSelectedPivotIndex { get; set; }

        CoreDispatcher UIDispatcher { get; set; }

        Timer _TimerWordSuggestion { get; set; }
        Timer _TimerSearchBackground { get; set; }
        #endregion variables

        public HomePage() {
            InitializeComponent();
            UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            ApplyCommandBarBarFrostedGlass();
            BindAppDataSource();

            RestorePivotPosition();
            StartNavigationToAnimation();
            LoadRecentData();
        }

        #region navigation

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatedFrom(e);
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

        #endregion navigation

        void RestorePivotPosition() {
            PagePivot.SelectedIndex = _LastSelectedPivotIndex;
            AutoShowSearchResults();

            void AutoShowSearchResults()
            {
                if (_LastSelectedPivotIndex != 2) {
                    return;
                }

                if (PageDataSource.PhotosSearchResults?.Count > 0) {
                    HideSearchPanel();
                    ShowSearchResults();
                }
            }
        }

        void StartNavigationToAnimation() {
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PhotoImage");

            if (animation == null || _LastSelectedPhoto == null) {
                return;
            }

            _BlockLoadedAnimation = true;

            if (PagePivot.SelectedIndex == 0) {
                animateRecent();

            } else if (PagePivot.SelectedIndex == 1) {
                animateCurated();

            } else if (PagePivot.SelectedIndex == 2) {
                animateSearchResults();
            }

            void animateRecent()
            {

                RecentView.Loaded += (s, e) => {
                    RecentView.ScrollIntoView(_LastSelectedPhoto);
                    var item = (GridViewItem)RecentView.ContainerFromItem(_LastSelectedPhoto);
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
            }

            void animateCurated()
            {
                CuratedView.Loaded += (s, e) => {
                    CuratedView.ScrollIntoView(_LastSelectedPhoto);
                    var item = (GridViewItem)CuratedView.ContainerFromItem(_LastSelectedPhoto);
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
            }

            void animateSearchResults()
            {
                SearchPhotosView.Loaded += (s, e) => {
                    SearchPhotosView.ScrollIntoView(_LastSelectedPhoto);
                    var item = (GridViewItem)SearchPhotosView.ContainerFromItem(_LastSelectedPhoto);
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
            }
        }

        #region data
        private void BindAppDataSource() {
            if (App.AppDataSource == null) {
                App.AppDataSource = new DataSource();
            }

            PageDataSource = App.AppDataSource;
        }

        private async void LoadRecentData() {
            if (PageDataSource.RecentPhotos?.Count > 0) {
                RecentView.ItemsSource = PageDataSource.RecentPhotos;
                return;
            }

            ShowLoadingView();

            var added = await PageDataSource.FetchRecentPhotos();

            HideLoadingView();

            if (added > 0) {
                RecentView.ItemsSource = PageDataSource.RecentPhotos;

            } else {
                ShowEmptyView();
                RecentView.Visibility = Visibility.Collapsed;
            }

            void ShowLoadingView()
            {
                RecentLoadingView.Visibility = Visibility.Visible;
            }

            void HideLoadingView()
            {
                RecentLoadingView.Visibility = Visibility.Collapsed;
            }

            void ShowEmptyView()
            {
                RecentEmptyView.Visibility = Visibility.Visible;
            }
        }

        private async void LoadCuratedData() {
            FindName("CuratedPhotosPivotItemContent");

            if (PageDataSource.CuratedPhotos?.Count > 0) {
                CuratedView.ItemsSource = PageDataSource.CuratedPhotos;
                return;
            }

            ShowLoadingView();

            var added = await PageDataSource.FetchCuratedPhotos();

            HideLoadingView();

            if (added>0) {
                CuratedView.ItemsSource = PageDataSource.CuratedPhotos;

            } else {
                ShowEmptyView();
                CuratedView.Visibility = Visibility.Collapsed;
            }


            void ShowLoadingView()
            {
                CuratedLoadingView.Visibility = Visibility.Visible;
            }

            void HideLoadingView()
            {
                CuratedLoadingView.Visibility = Visibility.Collapsed;
            }

            void ShowEmptyView()
            {
                CuratedEmptyView.Visibility = Visibility.Visible;
            }
        }

        #endregion data 

        #region events
        private void PagePivot_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UseAppBarMinimalMode();
            _LastSelectedPivotIndex = PagePivot.SelectedIndex;

            switch (PagePivot.SelectedIndex) {
                case 0:
                    ShowResfreshCmd();
                    HideShowSearchCmd();
                    StopWordsSuggestion();
                    break;

                case 1:
                    LoadCuratedData();
                    ShowResfreshCmd();
                    HideShowSearchCmd();
                    StopWordsSuggestion();
                    break;

                case 2:
                    HideResfreshCmd();

                    if (SearchPanel.Visibility == Visibility.Collapsed) {
                        UseAppBarCompactMode();
                        ShowShowSearchCmd();
                        return;
                    }

                    FocusSearchBox();
                    StartWordsSuggestions();
                    break;

                default:
                    break;
            }
            

            void HideResfreshCmd()
            {
                CmdRefresh.Visibility = Visibility.Collapsed;
            }

            void ShowResfreshCmd()
            {
                CmdRefresh.Visibility = Visibility.Visible;
            }
        }

        private void PhotoItem_Tapped(object sender, TappedRoutedEventArgs e) {
            var item = (StackPanel)sender;
            var wallpaper = (Photo)item.DataContext;
            _LastSelectedPhoto = wallpaper;

            var image = (Image)item.FindName("PhotoImage");

            if (image != null) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", image);
            }

            Frame.Navigate(typeof(PhotoPage), wallpaper);
        }

        private void PhotoItem_Loaded(object sender, RoutedEventArgs e) {
            var photoItem = (StackPanel)sender;

            var data = (Photo)photoItem.DataContext;

            if (data == _LastSelectedPhoto) {
                photoItem.Fade(1).Start();
                return;
            }

            float delay = GetAnimationDelayPivotIndex();

            photoItem.Offset(0, 100, 0)
                    .Then()
                    .Fade(1, 500, delay)
                    .Offset(0,0, 500, delay)
                    .Start();
        }

        float GetAnimationDelayPivotIndex() {
            var step = 100;

            switch (PagePivot.SelectedIndex) {
                case 0:
                    return _RecentAnimationDelay += step;
                case 1:
                    return _CuratedAnimationDelay += step;
                case 2:
                    return _SearchAnimationDelay += step;
                default:
                    return 0;
            }
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


            glassHost.Offset(0, 50).Start();

            AppBar.Opening += (s, e) => {
                glassHost.Offset(0, 0).Start();
            };

            AppBar.Closing += (s, e) => {
                if (AppBar.ClosedDisplayMode == AppBarClosedDisplayMode.Compact) {
                    glassHost.Offset(0, 27).Start();

                } else if (AppBar.ClosedDisplayMode == AppBarClosedDisplayMode.Minimal) {
                    glassHost.Offset(0, 50).Start();
                }
                
            };
        }

        private void CmdSettings_Tapped(object sender, TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(SettingsPage));
        }

        private void CmdRefresh_Tapped(object sender, TappedRoutedEventArgs e) {
            switch (PagePivot.SelectedIndex) {
                case 0:
                    ReloadRecentData();
                    break;
                case 1:
                    ReloadCuratedData();
                    break;
                default:
                    break;
            }

            void ReloadRecentData()
            {
                PageDataSource.RecentPhotos.Clear();
                LoadRecentData();
            }

            void ReloadCuratedData()
            {
                PageDataSource.CuratedPhotos.Clear();
                LoadCuratedData();
            }
            
        }

        private void CmdShowSearch_Tapped(object sender, TappedRoutedEventArgs e) {
            HideSearchResults();
            HideSearchEmptyView();
            ShowSearchPanel();
        }

        void UseAppBarMinimalMode() {
            AppBarFrozenHost.Offset(0, 50).Start();
            AppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
        }

        void UseAppBarCompactMode() {
            AppBarFrozenHost.Offset(0, 27).Start();
            AppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
        }

        void ShowShowSearchCmd() {
            CmdShowSearch.Visibility = Visibility.Visible;
        }

        void HideShowSearchCmd() {
            CmdShowSearch.Visibility = Visibility.Collapsed;
        }
        #endregion commandbar

        #region search
        private void SearchBox_KeyUp(object sender, KeyRoutedEventArgs e) {
            if (e.Key != Windows.System.VirtualKey.Enter) { return; }

            var query = SearchBox.Text;
            Search(query);
        }

        private async void Search(string query) {
            if (string.IsNullOrEmpty(query) || string.IsNullOrWhiteSpace(query)
                || query.Length < 3) {

                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                var message = loader.GetString("SearchQueryMinimum");
                Notify(message);
                return;
            }

            HideSearchPanel();
            ShowLoadingSearch();

            var results = await PageDataSource.SearchPhotos(query);

            HideLoadingSearch();

            if (PageDataSource.PhotosSearchResults.Count > 0) {
                ShowSearchResults();
                UseAppBarCompactMode();
                ShowShowSearchCmd();

            } else {
                ShowEmptyView();
            }

            void ShowLoadingSearch()
            {
                SearchLoadingView.Visibility = Visibility.Visible;
            }

            void HideLoadingSearch()
            {
                SearchLoadingView.Visibility = Visibility.Collapsed;
            }

            void ShowEmptyView()
            {
                SearchEmptyView.Visibility = Visibility.Visible;
            }
        }

        void ShowSearchResults() {
            SearchPhotosView.ItemsSource = PageDataSource.PhotosSearchResults;
            SearchPhotosView.Visibility = Visibility.Visible;
        }

        void HideSearchPanel() {
            SearchPanel.Visibility = Visibility.Collapsed;
        }

        void HideSearchEmptyView() {
            SearchEmptyView.Visibility = Visibility.Collapsed;
        }

        void HideSearchResults() {
            SearchPhotosView.Visibility = Visibility.Collapsed;
        }

        void ShowSearchPanel() {
            SearchPanel.Visibility = Visibility.Visible;
        }

        async void FocusSearchBox() {
            await UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                SearchBox.Focus(FocusState.Programmatic);
                SearchBox.Focus(FocusState.Programmatic);
            });
        }

        void StartWordsSuggestions() {
            string[] terms = { "nature", "space", "desk", "oceans" };

            var cursor = 0;
            var random = new Random();

            var autoEvent = new AutoResetEvent(false);
            _TimerWordSuggestion = new Timer(async (object state) => {
                await UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    UpdateTermSuggestion();
                });
            }, autoEvent, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            async void UpdateTermSuggestion()
            {
                await WordSuggestion.Fade(0).StartAsync();
                WordSuggestion.Text = string.Format("...{0}", terms[cursor]);
                WordSuggestion.Fade(1).Start();

                cursor = random.Next(terms.Length);
            }
        }

        void StopWordsSuggestion() {
            _TimerWordSuggestion?.Dispose();
        }

        private async void StartSearchBackgroundSlideShow() {

        }

        private void StopSearchBackgroundSlideShow() {

        }

        private void WordSuggestion_Tapped(object sender, TappedRoutedEventArgs e) {
            var query = WordSuggestion.Text.Substring(3);
            Search(query);
        }

        #endregion search

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
        #endregion notifications
    }
}
