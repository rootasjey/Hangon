using System;
using Hangon.Data;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Resources;
using Unsplasharp.Models;

namespace Hangon.Views {
    public sealed partial class HomePage : Page {

        #region variables
        private DataSource PageDataSource { get; set; }

        private float _RecentAnimationDelay { get; set; }

        private float _CuratedAnimationDelay { get; set; }

        private float _SearchAnimationDelay { get; set; }

        private static Photo _LastSelectedPhoto { get; set; }

        private bool _BlockLoadedAnimation { get; set; }

        private static int _LastSelectedPivotIndex { get; set; }

        private CoreDispatcher _UIDispatcher { get; set; }

        private Timer _TimerWordSuggestion { get; set; }

        private Timer _TimerSearchBackground { get; set; }

        private float _CmdBarOpenedOffset { get; set; }

        private static bool _AreSearchResultsActivated { get; set; }

        ResourceLoader _ResourcesLoader { get; set; }
        #endregion variables

        public HomePage() {
            InitializeComponent();
            InitializeVariables();
            
            ApplyCommandBarBarFrostedGlass();
            BindAppDataSource();
            RestorePivotPosition();

            ShowUpdateChangelogIfIsNewLaunch();
        }

        #region navigation

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            FreeMemory();

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
        }

        void StartNavigationToAnimation() {
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PhotoImageBack");

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
                    UI.AnimateBackItemToList(RecentView, _LastSelectedPhoto, animation);
                };
            }

            void animateCurated()
            {
                CuratedView.Loaded += (s, e) => {
                    UI.AnimateBackItemToList(CuratedView, _LastSelectedPhoto, animation);
                };
            }

            void animateSearchResults()
            {
                SearchPhotosView.Loaded += (s, e) => {
                    UI.AnimateBackItemToList(SearchPhotosView, _LastSelectedPhoto, animation);
                };
            }
        }

        #region data
        private void InitializeVariables() {
            _CmdBarOpenedOffset = 15;
            _ResourcesLoader = new ResourceLoader();
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void FreeMemory() {
            StopSearchBackgroundSlideShow();
            StopWordsSuggestion();
        }

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

            ShowRecentLoadingView();

            var added = await PageDataSource.FetchRecentPhotos();

            HideRecentLoadingView();

            if (added > 0) {
                RecentView.ItemsSource = PageDataSource.RecentPhotos;
                TileDesigner.UpdatePrimary();

            } else {
                ShowRecentEmptyView();
                RecentView.Visibility = Visibility.Collapsed;
            }
        }

        void ShowRecentLoadingView() {
            RecentLoadingView.Visibility = Visibility.Visible;
        }

        void HideRecentLoadingView() {
            RecentLoadingView.Visibility = Visibility.Collapsed;
        }

        void ShowRecentEmptyView() {
            RecentEmptyView.Visibility = Visibility.Visible;
        }

        private async void LoadCuratedData() {
            if (PageDataSource.CuratedPhotos?.Count > 0) {
                CuratedView.ItemsSource = PageDataSource.CuratedPhotos;
                return;
            }

            ShowCuratedLoadingView();

            var added = await PageDataSource.FetchCuratedPhotos();

            HideCuratedLoadingView();

            if (added>0) {
                CuratedView.ItemsSource = PageDataSource.CuratedPhotos;

            } else {
                ShowCuratedEmptyView();
                CuratedView.Visibility = Visibility.Collapsed;
            }
        }

        void ShowCuratedLoadingView() {
            CuratedLoadingView.Visibility = Visibility.Visible;
        }

        void HideCuratedLoadingView() {
            CuratedLoadingView.Visibility = Visibility.Collapsed;
        }

        void ShowCuratedEmptyView() {
            CuratedEmptyView.Visibility = Visibility.Visible;
        }

        #endregion data 

        #region events
        private void PagePivot_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UseCmdBarMinimalMode();
            _LastSelectedPivotIndex = PagePivot.SelectedIndex;

            switch (PagePivot.SelectedIndex) {
                case 0:
                    FindName("RecentPhotosPivotItemContent");
                    LoadRecentData();
                    StartNavigationToAnimation();

                    ShowResfreshCmd();
                    HideShowSearchCmd();
                    StopWordsSuggestion();
                    StopSearchBackgroundSlideShow();
                    break;

                case 1:
                    FindName("CuratedPhotosPivotItemContent");
                    LoadCuratedData();
                    StartNavigationToAnimation();

                    ShowResfreshCmd();
                    HideShowSearchCmd();
                    StopWordsSuggestion();
                    StopSearchBackgroundSlideShow();
                    break;

                case 2:
                    FindName("SearchPivotItemContent");
                    StartNavigationToAnimation();
                    HideResfreshCmd();

                    if (_AreSearchResultsActivated) {
                        HideSearchPanel();
                        ShowSearchResults();

                        UpdateCmdBarOpenedOffset(2);
                        UseCmdBarCompactMode();
                        ShowShowSearchCmd();
                        return;
                    }

                    FocusSearchBox();
                    HideSearchResults();
                    UpdateCmdBarOpenedOffset(0);
                    StartWordsSuggestions();
                    StartSearchBackgroundSlideShow();
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

            // EVENTS
            // ------
            AppBar.Opening += (s, e) => {
                glassHost.Offset(0, _CmdBarOpenedOffset).Start();
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

            async void ReloadRecentData()
            {
                ShowRecentLoadingView();
                await PageDataSource.ReloadRecentPhotos();
                HideRecentLoadingView();

                if (PageDataSource.RecentPhotos.Count == 0) {
                    ShowRecentEmptyView();
                }
            }

            async void ReloadCuratedData()
            {
                ShowCuratedLoadingView();
                await PageDataSource.ReloadCuratedPhotos();
                HideCuratedLoadingView();

                if (PageDataSource.CuratedPhotos.Count == 0) {
                    ShowCuratedEmptyView();
                }
            }
            
        }

        private void CmdShowSearch_Tapped(object sender, TappedRoutedEventArgs e) {
            HideSearchResults();
            HideSearchEmptyView();
            ShowSearchPanel();

            HideShowSearchCmd();
            UseCmdBarMinimalMode();
            UpdateCmdBarOpenedOffset(0);
            
            StartSearchBackgroundSlideShow();
            StartWordsSuggestions();
        }

        void UseCmdBarMinimalMode() {
            AppBarFrozenHost.Offset(0, 50).Start();
            AppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
        }

        void UseCmdBarCompactMode() {
            AppBarFrozenHost.Offset(0, 27).Start();
            AppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
        }

        void ShowShowSearchCmd() {
            CmdShowSearch.Visibility = Visibility.Visible;
            UpdateCmdBarOpenedOffset(2);
        }

        void HideShowSearchCmd() {
            CmdShowSearch.Visibility = Visibility.Collapsed;
            UpdateCmdBarOpenedOffset(1);
        }

        /// <summary>
        /// Update the CommandBar opened offset
        /// It changes according to icons' label length
        /// </summary>
        /// <param name="lines">The icon's label max lines</param>
        void UpdateCmdBarOpenedOffset(int lines) {
            switch (lines) {
                case 0:
                    _CmdBarOpenedOffset = 28;
                    break;
                case 1:
                    _CmdBarOpenedOffset = 15;
                    break;
                case 2:
                    _CmdBarOpenedOffset = 0;
                    break;
                default:
                    break;
            }
        }
        #endregion commandbar

        #region search
        private void SearchBox_KeyUp(object sender, KeyRoutedEventArgs e) {
            if (e.Key != Windows.System.VirtualKey.Enter) { return; }

            var query = SearchBox.Text;
            Search(query);
        }

        private async void Search(string query) {
            if (string.IsNullOrEmpty(query) || 
                string.IsNullOrWhiteSpace(query) || 
                query.Length < 3) {

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
                UseCmdBarCompactMode();
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

        async void ShowSearchResults() {
            _AreSearchResultsActivated = true;

            SearchPhotosView.ItemsSource = PageDataSource.PhotosSearchResults;

            await SearchPhotosView.Fade(0, 0).Offset(0, 20, 0).StartAsync();

            SearchPhotosView.Visibility = Visibility.Visible;
            SearchPhotosView.Fade(1).Offset(0, 0).Start();
        }

        async void HideSearchResults() {
            await SearchPhotosView.Fade().Offset(0, 20).StartAsync();
            SearchPhotosView.Visibility = Visibility.Collapsed;
        }

        void ShowSearchPanel() {
            _AreSearchResultsActivated = false;

            var asyncExec = SearchPanel.AnimateSlideIn();
        }

        void HideSearchPanel() {
            SearchPanel.Visibility = Visibility.Collapsed;

            StopWordsSuggestion();
            StopSearchBackgroundSlideShow();
        }

        void HideSearchEmptyView() {
            SearchEmptyView.Visibility = Visibility.Collapsed;
        }

        async void FocusSearchBox() {
            await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                SearchBox.Focus(FocusState.Programmatic);
                SearchBox.Focus(FocusState.Programmatic);
            });
        }

        void StartWordsSuggestions() {
            string[] words = {
                "nature", "landscape", "desk", "oceans",
                "city", "road", "people", "love", "sky",
                "mountains", "man", "woman", "nasa",
                "summer", "home", "food", "happy"
            };

            var random = new Random();
            var autoEvent = new AutoResetEvent(false);

            _TimerWordSuggestion = new Timer(async (object state) => {
                await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    UpdateTermSuggestion();
                });
            }, autoEvent, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            async void UpdateTermSuggestion()
            {
                await WordSuggestion.Fade(0).StartAsync();
                WordSuggestion.Text = string.Format("...{0}", words[random.Next(words.Length)]);
                WordSuggestion.Fade(1).Start();
            }
        }

        void StopWordsSuggestion() {
            _TimerWordSuggestion?.Dispose();
        }

        private void StartSearchBackgroundSlideShow() {
            var recents = PageDataSource.RecentPhotos;
            if (recents?.Count == 0) return;

            var duration = 10000;
            var random = new Random();
            var autoEvent = new AutoResetEvent(false);

            ShowPageBackground();

            _TimerSearchBackground = new Timer(async (object state) => {
                await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    UpdateBackground();
                });
            }, autoEvent, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(duration));

            async void UpdateBackground()
            {
                var index = random.Next(recents.Count);
                var photoPath = recents[index].Urls.Regular;

                await BackgroundImage.Fade(0).StartAsync();
                await BackgroundImage.Scale(1f, 1f, 0, 0, 0).StartAsync();
                BackgroundImage.Source = new BitmapImage(new Uri(photoPath));
                BackgroundImage.Fade(1, duration).Scale(1.2f, 1.2f, 0, 0, duration).Start();
            }
        }

        private void StopSearchBackgroundSlideShow() {
            HidePageBackground();
            _TimerSearchBackground?.Dispose();
        }

        private void ShowPageBackground() {
            BackgroundContainer.Visibility = Visibility.Visible;
        }

        private async void HidePageBackground() {
            await BackgroundImage.Fade().StartAsync();
            BackgroundContainer.Visibility = Visibility.Collapsed;
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

        private void CmdCopyLink_Tapped(object sender, TappedRoutedEventArgs e) {
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

        private async void CmdOpenInBrowser_Tapped(object sender, TappedRoutedEventArgs e) {
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

        #endregion rightTapped flyout

        #region update changelog
        private void ShowUpdateChangelogIfIsNewLaunch() {
            if (Settings.IsNewUpdatedLaunch()) {
                ShowLastUpdateChangelog();
            }            
        }

        private async void ShowLastUpdateChangelog() {
            PagePivot.IsEnabled = false;
            await UpdateChangeLogFlyout.Scale(.9f, .9f, 0, 0, 0).Fade(0).StartAsync();
            UpdateChangeLogFlyout.Visibility = Visibility.Visible;

            var x = (float)UpdateChangeLogFlyout.ActualWidth / 2;
            var y = (float)UpdateChangeLogFlyout.ActualHeight / 2;

            await UpdateChangeLogFlyout.Scale(1f, 1f, x, y).Fade(1).StartAsync();
            PagePivot.Blur(10, 500, 500).Start();
        }

        private async void ChangelogDismissButton_Tapped(object sender, TappedRoutedEventArgs e) {
            var x = (float)UpdateChangeLogFlyout.ActualWidth / 2;
            var y = (float)UpdateChangeLogFlyout.ActualHeight / 2;

            await UpdateChangeLogFlyout.Scale(.9f, .9f, x, y).Fade(0).StartAsync();
            UpdateChangeLogFlyout.Visibility = Visibility.Collapsed;
            PagePivot.Blur(0).Start();
            PagePivot.IsEnabled = true;
        }

        #endregion update changelog
    }
}
