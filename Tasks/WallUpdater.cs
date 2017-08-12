using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.System.UserProfile;
using Unsplasharp.Models;
using Tasks.Data;
using Unsplasharp;

namespace Tasks {
    public sealed class WallUpdater : IBackgroundTask {
        #region variables
        BackgroundTaskDeferral _Deferral;

        private static string WallTaskName {
            get {
                return "WallUpdaterTask";
            }
        }

        private static string LockscreenTaskName {
            get {
                return "LockscreenUpdaterTask";
            }
        }

        #endregion variables

        /// <summary>
        /// Task's Entry Point
        /// </summary>
        /// <param name="taskInstance">Task starting the method</param>
        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance) {
            taskInstance.Canceled += OnCanceled;
            _Deferral = taskInstance.GetDeferral();

            SaveTime(taskInstance);

            Photo photo = await GetRandom();

            StorageFile file = await DownloadImagefromServer(photo.Urls.Regular, photo.Id);

            if (taskInstance.Task.Name == WallTaskName) {
                await SetWallpaperAsync(file);
            } else {
                await SetLockscreenAsync(file);
            }

            //SaveLockscreenBackgroundName(lockImage.Name);
            //SaveAppBackground(lockImage);
            _Deferral.Complete();
        }

        string GetActivityKey(IBackgroundTaskInstance instance) {
            string key = instance.Task.Name == WallTaskName ? WallTaskName : LockscreenTaskName;
            key += "Activity";
            return key;
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
            // Indicate that the background task is canceled.
            string key = GetActivityKey(sender);

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            ApplicationDataCompositeValue activityError = new ApplicationDataCompositeValue {
                ["DateTime"] = DateTime.Now.ToLocalTime(),
                ["Exception"] = reason.ToString()
            };

            localSettings.Values[key] = activityError;
        }

        private void SaveTime(IBackgroundTaskInstance instance) {
            var key = GetActivityKey(instance);

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            ApplicationDataCompositeValue stats = new ApplicationDataCompositeValue {
                ["DateTime"] = DateTime.Now.ToString(),
                ["Exception"] = null
            };

            localSettings.Values[key] = stats;
        }

        private async Task<Photo> GetRandom() {
            var client = new UnsplasharpClient(Credentials.ApplicationId);
            return await client.GetRandomPhoto();
        }

        private async Task<StorageFile> DownloadImagefromServer(string URI, string filename) {
            filename += ".png";
            var rootFolder = ApplicationData.Current.LocalFolder;
            var coverpic = await rootFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            try {
                HttpClient client = new HttpClient();
                byte[] buffer = await client.GetByteArrayAsync(URI); // Download file
                using (Stream stream = await coverpic.OpenStreamForWriteAsync())
                    stream.Write(buffer, 0, buffer.Length); // Save

                return coverpic;
            } catch {
                return null;
            }
        }

        // Pass in a relative path to a file inside the local appdata folder 
        private async Task<bool> SetLockscreenAsync(StorageFile file) {
            bool success = false;

            if (UserProfilePersonalizationSettings.IsSupported()) {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetLockScreenImageAsync(file);
            }
            return success;
        }

        private async Task<bool> SetWallpaperAsync(StorageFile file) {
            bool success = false;

            if (UserProfilePersonalizationSettings.IsSupported()) {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetWallpaperImageAsync(file);
            }
            return success;
        }
    }
}