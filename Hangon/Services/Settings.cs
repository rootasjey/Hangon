using System;
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
        #endregion keys

        #region path
        public static void UpdateMustUseDefaultDownloadPath(bool whatever) {
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
        public static void UpdateMustUseDefaultResolution(bool whatever) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[UseDefaultDownloadResolutionKey] = whatever;
        }

        public static bool UseDefaultDownloadResolution() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(UseDefaultDownloadResolutionKey) ?
                (bool)settingsValues[UseDefaultDownloadResolutionKey] : false;
        }

        #endregion resolution
    }
}
