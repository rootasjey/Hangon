using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unsplasharp.Models;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Hangon.Models;

namespace Hangon.Services {
    public class Settings {
        #region keys

        private static string UseDefaultDownloadPathKey {
            get {
                return "UseDefaultDownloadPath";
            }
        }

        private static string DefaultDownloadPathKey {
            get {
                return "DefaultDownloadPath";
            }
        }

        private static string UseDefaultDownloadResolutionKey {
            get {
                return "UseDefaultDownloadResolution";
            }
        }

        private static string DefaultDownloadResolutionKey {
            get {
                return "DefaultDownloadResolution";
            }
        }

        private static string LanguageKey {
            get {
                return "Language";
            }
        }

        private static string ThemeKey {
            get {
                return "Theme";
            }
        }

        private static string AppVersionKey {
            get {
                return "AppVersion";
            }
        }

        private static string PhotoStretchingKey {
            get {
                return "PhotoStretching";
            }
        }

        private static string BestPhotoResolutionKey {
            get {
                return "BestPhotoResolution";
            }
        }

        private static string FavoritesListKey {
            get {
                return "FavoritesList";
            }
        }

        #endregion keys

        #region path
        public static void UpdateUseDefaultDownloadPath(bool whatever) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[UseDefaultDownloadPathKey] = whatever;
        }

        public static bool UseDefaultDownloadPath() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(UseDefaultDownloadPathKey) ?
                (bool)settingsValues[UseDefaultDownloadPathKey] : false;
        }

        public static void SaveDefaultDownloadPath(string path) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[DefaultDownloadPathKey] = path;
        }
        

        public static string GetDefaultDownloadPath() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(DefaultDownloadPathKey) ? 
                (string)settingsValues[DefaultDownloadPathKey] : null;
        }
        #endregion path

        #region resolution
        public static void UpdateUseDefaultResolution(bool whatever) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[UseDefaultDownloadResolutionKey] = whatever;
        }

        public static bool UseDefaultDownloadResolution() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(UseDefaultDownloadResolutionKey) ?
                (bool)settingsValues[UseDefaultDownloadResolutionKey] : false;
        }

        public static string GetDefaultDownloadResolution() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(DefaultDownloadResolutionKey) ?
                (string)settingsValues[DefaultDownloadResolutionKey] : "raw";
        }

        public static void SaveDefaultDownloadResolution(string resolution) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[DefaultDownloadResolutionKey] = resolution;
        }
        #endregion resolution

        #region lang
        public static void SaveAppCurrentLanguage(string language) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[LanguageKey] = language;
        }

        public static string GetAppCurrentLanguage() {
            string defaultLanguage = GlobalizationPreferences.Languages[0];

            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LanguageKey) ? (string)settingsValues[LanguageKey] : defaultLanguage;
        }
        
        #endregion lang

        #region theme
        public static bool IsApplicationThemeLight() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.TryGetValue(ThemeKey, out var previousTheme);
            return ApplicationTheme.Light.ToString() == (string)previousTheme;
        }

        public static void UpdateAppTheme(ApplicationTheme theme) {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.TryGetValue(ThemeKey, out var previousTheme);

            if ((string)previousTheme == theme.ToString()) return;

            localSettings.Values[ThemeKey] = theme.ToString();
            App.UpdateAppTheme();
        }
        #endregion theme

        #region photo settings

        public static Windows.UI.Xaml.Media.Stretch GetDefaultPhotoStretching() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.TryGetValue(PhotoStretchingKey, out var stretch);

            if ((string)stretch == nameof(Windows.UI.Xaml.Media.Stretch.UniformToFill)) {
                return Windows.UI.Xaml.Media.Stretch.UniformToFill;
            }

            return Windows.UI.Xaml.Media.Stretch.Uniform;
        }

        public static void SaveDefaultPhotoStretching(Windows.UI.Xaml.Media.Stretch stretch) {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[PhotoStretchingKey] = stretch.ToString();
        }

        public static void SaveBestPhotoResolution(string resolution) {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[BestPhotoResolutionKey] = resolution;
        }

        #endregion photo settings

        #region favorites

        public static async Task SaveFavorites(PhotosKeyedCollection photosList) {
            var json = JsonConvert.SerializeObject(photosList);

            var file = await ApplicationData
                .Current
                .LocalFolder
                .CreateFileAsync(FavoritesListKey, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, json);
        }

        public static async Task<PhotosKeyedCollection> GetFavorites() {
            var file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(FavoritesListKey);
            if (file == null) return null;

            string json = await FileIO.ReadTextAsync(file);
            return JsonConvert.DeserializeObject<PhotosKeyedCollection>(json);
        }

        #endregion favorites

        #region appversion
        public static bool IsNewUpdatedLaunch() {
            var isNewUpdatedLaunch = true;
            var currentVersion = GetAppVersion();
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(AppVersionKey)) {
                string savedVersion = (string)settingsValues[AppVersionKey];
                

                if (savedVersion == currentVersion) {
                    isNewUpdatedLaunch = false;
                }
                else { settingsValues[AppVersionKey] = currentVersion; }
            } else { settingsValues[AppVersionKey] = currentVersion; }

            return isNewUpdatedLaunch;
        }

        public static string GetAppVersion() {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

        }
        #endregion appversion
    }
}
