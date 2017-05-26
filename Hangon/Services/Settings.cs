using Windows.Storage;

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
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LanguageKey) ? (string)settingsValues[LanguageKey] : null;
        }
        #endregion lang
        
    }
}
