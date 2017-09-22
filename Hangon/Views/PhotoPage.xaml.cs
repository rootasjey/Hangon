using System;
using Hangon.Services;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Hangon.Data;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using Windows.Foundation;
using System.Threading;
using Windows.UI;
using Windows.ApplicationModel.Resources;
using Unsplasharp.Models;

namespace Hangon.Views {
    public sealed partial class PhotoPage : Page {
        #region variables
        public static Photo _CurrentPhoto { get; set; }

        private Image _CurrentPhotoImage { get; set; }

        private DataSource _PageDataSource { get; set; }

        private static PhotosList _PageItemsSource { get; set; }

        private Visual _BackgroundVisual { get; set; }

        private Compositor _BackgroundCompositor { get; set; }

        private ScrollViewer _ScrollViewer { get; set; }

        private CompositionPropertySet _ScrollerPropertySet { get; set; }

        private CoreDispatcher _UIDispatcher { get; set; }

        private double _PhotoCaptionTopMargin { get; set; }

        private bool _IsPhotoCaptionVisible { get; set; }

        private RowDefinition _RowSpacing { get; set; }

        private bool _ConnectedAnimationHandled { get; set; }

        private bool _IsStretched { get; set; }
        #endregion variables

        public PhotoPage() {
            InitializeComponent();
            InitializeVariables();
            ApplyCommandBarBarFrostedGlass();
        }

        private void InitializeVariables() {
            GetDeviceType();

            _PageDataSource = App.AppDataSource;
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            _IsPhotoCaptionVisible = true;
            _ConnectedAnimationHandled = false;
            _IsStretched = Settings.GetDefaultPhotoStretching() == Windows.UI.Xaml.Media.Stretch.UniformToFill;
            UpdateCmdStretchIcon();
        }

        void GetDeviceType() {
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                _PhotoCaptionTopMargin = 280;
            } else {
                _PhotoCaptionTopMargin = 180;
            }
        }

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;

            if (e.NavigationMode == NavigationMode.New) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImage", _CurrentPhotoImage);

            } else if (e.NavigationMode == NavigationMode.Back) {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhotoImageBack", _CurrentPhotoImage);

                // Update last selected photo on previous Page
                // which may has been changed due to FlipView
                if (e.SourcePageType == typeof(HomePage)) {
                    HomePage._LastSelectedPhoto = _CurrentPhoto;

                } else if (e.SourcePageType == typeof(UserPage)) {
                    UserPage._LastSelectedPhoto = _CurrentPhoto;

                } else if (e.SourcePageType == typeof(CollectionPage)) {
                    CollectionPage._LastSelectedPhoto = _CurrentPhoto;
                }
            }            

            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            var parameters = (object[])e.Parameter;

            if (e.NavigationMode != NavigationMode.Back) {
                _CurrentPhoto = (Photo)parameters[0];
            }

            _PageItemsSource = (PhotosList)parameters[1];

            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion navigation

        #region download

        void ShowProgress(string message = "") {
            ProgressDeterminate.Value = 0;
            FlyoutNotification.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(message)) return;
            FlyoutText.Text = message;
        }

        void HideProgress() {
            FlyoutNotification.Visibility = Visibility.Collapsed;
        }

        public void HttpProgressCallback(Windows.Web.Http.HttpProgress progress) {
            if (progress.TotalBytesToReceive == null) return;

            ProgressDeterminate.Minimum = 0;
            ProgressDeterminate.Maximum = (double)progress.TotalBytesToReceive;
            ProgressDeterminate.Value = progress.BytesReceived;
        }

        async void Download(string size = "") {
            ShowProgress();
            var result = false;

            if (string.IsNullOrEmpty(size)) {
                result = await Wallpaper.SaveToPicturesLibrary(_CurrentPhoto, HttpProgressCallback);

            } else {
                string url = getURL();
                result = await Wallpaper.SaveToPicturesLibrary(_CurrentPhoto, HttpProgressCallback, url);
            }

            HideProgress();

            var successMessage = App.ResourceLoader.GetString("SavePhotoSuccess");
            var failedMessage = App.ResourceLoader.GetString("SavePhotoFailed");

            if (result) Notify(successMessage);
            else Notify(failedMessage);

            string getURL() {
                switch (size) {
                    case "raw":
                        return _CurrentPhoto.Urls.Raw;
                    case "full":
                        return _CurrentPhoto.Urls.Full;
                    case "regular":
                        return _CurrentPhoto.Urls.Regular;
                    case "small":
                        return _CurrentPhoto.Urls.Small;
                    default:
                        return _CurrentPhoto.Urls.Regular;
                }
            }
        }

        #endregion download

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

        private async void CmdSetAsWallpaper(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var progressMessage = App.ResourceLoader.GetString("SettingWallpaper");
            var successMessage = App.ResourceLoader.GetString("WallpaperSetSuccess");
            var failedMessage = App.ResourceLoader.GetString("WallpaperSetFailed");

            ShowProgress(progressMessage);
            var success = await Wallpaper.SetAsWallpaper(_CurrentPhoto, HttpProgressCallback);
            HideProgress();

            if (success) Notify(successMessage);
            else Notify(failedMessage);
        }

        private async void CmdSetAsLockscreen(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var progressMessage = App.ResourceLoader.GetString("SettingLockscreen");
            var successMessage = App.ResourceLoader.GetString("LockscreenSetSuccess");
            var failedMessage = App.ResourceLoader.GetString("LockscreenSetFailed");

            ShowProgress(progressMessage);
            var success = await Wallpaper.SetAsLockscreen(_CurrentPhoto, HttpProgressCallback);
            HideProgress();

            if (success) Notify(successMessage);
            else Notify(failedMessage);
        }

        private void CmdDownload_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (Settings.UseDefaultDownloadResolution()) {
                var resolution = Settings.GetDefaultDownloadResolution();
                Download(resolution);
                return;
            }

            var cmd = (AppBarButton)sender;
            FlyoutDownload.ShowAt(cmd);
        }

        private void CmdDownloadResolution_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var cmd = (MenuFlyoutItem)sender;
            var resolution = (string)cmd.Tag;
            Download(resolution);
        }

        private void CmdToggleCaption_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            if (_IsPhotoCaptionVisible) {
                HideCaption();
                _IsPhotoCaptionVisible = false;
                return;
            }

            ShowCaption();
            _IsPhotoCaptionVisible = true;
        }

        private void CmdCrop_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            
        }

        private void CmdStretch_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var root = GetCurrentItemTemplateRoot();
            var image = (Image)root.FindName("PhotoImage");

            var stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
            var glyph = "\uE1D9";

            if (!_IsStretched) {
                stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
                glyph = "\uE1D8";
                _IsStretched = true;

            } else { _IsStretched = false; }

            image.Stretch = stretch;
            CmdStretchIcon.Glyph = glyph;

            Settings.SaveDefaultPhotoStretching(stretch);
        }

        private void UpdateCmdStretchIcon() {
            if (_IsStretched) { CmdStretchIcon.Glyph = "\uE1D8"; } 
            else { CmdStretchIcon.Glyph = "\uE1D9"; }
        }

        #endregion commandbar

        #region micro-interactions
        private void UserView_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var panel = (Ellipse)sender;
            panel.Scale(1.1f, 1.1f).Start();
        }

        private void UserView_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var panel = (Ellipse)sender;
            panel.Scale(1f, 1f).Start();
        }

        #endregion micro-interactions

        #region image composition
        private void HandleConnectedAnimation(Image photoImage) {
            if (_CurrentPhoto == null || photoImage == null) return;

            _ConnectedAnimationHandled = true;

            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PhotoImage");

            if (animation != null) {
                //photoImage.Opacity = 0;
                photoImage.ImageOpened += (s, e) => {
                    photoImage.Opacity = 1;
                    animation.TryStart(photoImage);
                };
            }

            //photoImage.Source = new BitmapImage(new Uri(_CurrentPhoto.Urls.Regular));
        }

        private void PhotoViewContainer_Loaded(object sender, RoutedEventArgs ev) {
            var PhotoViewContainer = (Grid)sender;
            var PhotoDimmer = (Grid)PhotoViewContainer.FindName("PhotoDimmer");
            var PhotoView = (Image)PhotoViewContainer.FindName("PhotoView");
            var PhotoCaption = (ScrollViewer)PhotoViewContainer.FindName("PhotoCaption");

            if (PhotosFlipView.SelectedIndex == 0) {
                var image = (Image)PhotoViewContainer.FindName("PhotoImage");
                RefreshPhotoStretching(image);
            }
            
            InitializeCompositorVariables(PhotoViewContainer);

            PhotoCaption.Loaded += (s, e) => {
                InitializeScrollViewerProps(PhotoCaption);
                AttachBlurOpacityEffects(PhotoViewContainer, PhotoDimmer);
            };
        }

        void InitializeCompositorVariables(Grid PhotoViewContainer) {
            _BackgroundVisual = ElementCompositionPreview.GetElementVisual(PhotoViewContainer);
            _BackgroundCompositor = _BackgroundVisual.Compositor;
        }

        void InitializeScrollViewerProps(ScrollViewer PhotoCaption) {
            _ScrollViewer = PhotoCaption;
            _ScrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(_ScrollViewer);
        }

        void AttachBlurOpacityEffects(Grid PhotoViewContainer, Grid PhotoDimmer) {
            // -----
            // Blur
            // -----
            var blurEffect = new GaussianBlurEffect() {
                Name = "Blur",
                BlurAmount = 20.0f,
                BorderMode = EffectBorderMode.Hard,
                Optimization = EffectOptimization.Speed,
                Source = new CompositionEffectSourceParameter("Backdrop")
            };

            var effectFactory = _BackgroundCompositor.CreateEffectFactory(blurEffect, new[] { "Blur.BlurAmount" });
            var effectBrush = effectFactory.CreateBrush();

            var destinationBrush = _BackgroundCompositor.CreateBackdropBrush();
            effectBrush.SetSourceParameter("Backdrop", destinationBrush);

            var _bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var _scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var _size = new Size(_bounds.Width * _scaleFactor, _bounds.Height * _scaleFactor);

            var blurVisual = _BackgroundCompositor.CreateSpriteVisual();
            blurVisual.Size = new Vector2((float)_size.Width + 300, (float)_size.Height);
            blurVisual.Brush = effectBrush;
            ElementCompositionPreview.SetElementChildVisual(PhotoViewContainer, blurVisual);

            var blurExpression = _BackgroundCompositor.CreateExpressionAnimation(
                "Clamp(-scroller.Translation.Y / 10,0,20)");
            blurExpression.SetReferenceParameter("scroller", _ScrollerPropertySet);

            blurVisual.Brush.Properties.StartAnimation("Blur.BlurAmount", blurExpression);

            // --------
            // Opacity
            // --------
            var opacityVisual = _BackgroundCompositor.CreateSpriteVisual();
            opacityVisual.Brush = _BackgroundCompositor.CreateColorBrush(Windows.UI.Colors.Black);
            opacityVisual.Size = new Vector2((float)_size.Width + 300, (float)_size.Height);
            ElementCompositionPreview.SetElementChildVisual(PhotoDimmer, opacityVisual);

            var opacityExpression = _BackgroundCompositor.CreateExpressionAnimation(
                "Clamp((-scroller.Translation.Y) / 100, 0, 0.6)");
            opacityExpression.SetReferenceParameter("scroller", _ScrollerPropertySet);
            opacityVisual.StartAnimation("Opacity", opacityExpression);
        }

        #endregion image composition

        #region events

        private void PhotoCaptionContent_Loaded(object sender, RoutedEventArgs e) {
            var photoCaption = (Grid)sender;
            _RowSpacing = (RowDefinition)photoCaption.FindName("RowSpacing");

            UpdatePhotoCaptionPosition();
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e) {
            UpdatePhotoCaptionPosition();
        }

        private void PhotoCaptionContent_Unloaded(object sender, RoutedEventArgs e) {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        private void UserView_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var UserView = (StackPanel)sender;
            var UserProfileImage = (Ellipse)UserView.FindName("UserProfileImage");

            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("UserProfileImage", UserProfileImage);
            Frame.Navigate(typeof(UserPage), _CurrentPhoto);
        }

        private async void OpenPhotoInBrowser_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var tracking = "?utm_source=Hangon&utm_medium=referral&utm_campaign=" + Credentials.ApplicationId;
            var photoUri = new Uri(string.Format("{0}{1}", _CurrentPhoto.Links.Html, tracking));
            var success = await Windows.System.Launcher.LaunchUriAsync(photoUri);
        }

        private void PhotosFlipView_Loaded(object sender, RoutedEventArgs e) {
            PhotosFlipView.ItemsSource = _PageItemsSource;
            var index = _PageItemsSource.IndexOf(_CurrentPhoto);

            PhotosFlipView.SelectionChanged -= PhotosFlipView_SelectionChanged;
            PhotosFlipView.SelectionChanged += PhotosFlipView_SelectionChanged;

            PhotosFlipView.SelectedIndex = index;

            ForceFetchOnFirstItemIfSelected();

            void ForceFetchOnFirstItemIfSelected() {
                if (index == 0) {
                    FillPhotoProperties();
                }
            }
        }

        private void PhotoCaption_Loaded(object sender, RoutedEventArgs ev) {
            var PhotoCaption = (ScrollViewer)sender;
            InitializeScrollViewerProps(PhotoCaption);
        }

        private void PhotoImage_Loaded(object sender, RoutedEventArgs e) {
            if (_ConnectedAnimationHandled) return;

            var photoImage = (Image)sender;
            HandleConnectedAnimation(photoImage);

            _CurrentPhotoImage = photoImage;
        }

        private void PhotosFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            _CurrentPhoto = (Photo)PhotosFlipView.SelectedItem;

            FillPhotoProperties();
            PhotosFlipView.UpdateLayout();

            var item = (FlipViewItem)PhotosFlipView.ContainerFromItem(_CurrentPhoto);
            if (item == null) { return; }

            var root = (Grid)item.ContentTemplateRoot;
            var image = (Image)root.FindName("PhotoImage");

            _CurrentPhotoImage = image;

            RefreshCaptionVisibility(root);
            RefreshPhotoStretching(image);
        }

        private void PhotoPullBox_RefreshInvoked(DependencyObject sender, object args) {
            if (Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion events

        #region others methods

        void ShowCaption(Grid caption = null) {
            Grid _caption;

            if (caption != null) _caption = caption;
            else _caption = GetCurrentCaptionItem();

            if (_caption.Opacity == 1) return;

            _caption
                .Offset(0, 0)
                .Fade(1)
                .Start();

            CmdToggleCaptionIcon.UriSource = new Uri("ms-appx:///Assets/Icons/hide.png");

            var label = App.ResourceLoader.GetString("HideCaption");
            CmdToggleCaption.Label = label;
        }

        void HideCaption(Grid caption = null) {
            Grid _caption;

            if (caption != null) _caption = caption;
            else _caption = GetCurrentCaptionItem();

            if (_caption.Opacity == 0) return;

            _caption
                .Offset(0, 300)
                .Fade(0)
                .Start();

            CmdToggleCaptionIcon.UriSource = new Uri("ms-appx:///Assets/Icons/show.png");

            var label = App.ResourceLoader.GetString("ShowCaption");
            CmdToggleCaption.Label = label;

            // Completly hide caption
            //var root = GetCurrentItemTemplateRoot();
            //var photoPullBox = (UIElement)root.FindName("PhotoPullBox");
            //photoPullBox.Visibility = Visibility.Collapsed;
        }

        private Grid GetCurrentCaptionItem() {
            var root = GetCurrentItemTemplateRoot();
            var caption = (Grid)root.FindName("PhotoCaptionContent");
            return caption;
        }

        private Grid GetCurrentItemTemplateRoot() {
            var photo = (Photo)PhotosFlipView.SelectedItem;
            var flipViewItem = (FlipViewItem)PhotosFlipView.ContainerFromItem(photo);
            return (Grid)flipViewItem.ContentTemplateRoot;
        }

        private void RefreshPhotoStretching(Image image) {
            if (_IsStretched) {
                image.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
                return;
            }

            image.Stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
        }

        private void UpdatePhotoCaptionPosition() {
            var height = Window.Current.Bounds.Height - _PhotoCaptionTopMargin;
            _RowSpacing.Height = new GridLength(height);
        }

        private void FlyoutNotification_Dismiss(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            HideNotification();
        }

        void Notify(string message) {
            ProgressGrid.Visibility = Visibility.Collapsed;
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

        private async void FillPhotoProperties() {
            var fullPhoto = await _PageDataSource.GetPhoto(_CurrentPhoto.Id);
            MergePhotoData(_CurrentPhoto, fullPhoto);
        }

        private void MergePhotoData(Photo target, Photo source) {
            if (target == null || source == null) return;

            target.Downloads = source.Downloads;
            target.Exif = source.Exif;
            target.Location = source.Location;
        }

        void RefreshCaptionVisibility(Grid root) {
            var caption = (Grid)root.FindName("PhotoCaptionContent");

            if (!_IsPhotoCaptionVisible) { HideCaption(caption); } else { ShowCaption(caption); }
        }

        #endregion others

    }
}
