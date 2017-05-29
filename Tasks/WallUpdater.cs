using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Tasks.Models;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Newtonsoft.Json.Linq;
using Windows.System.UserProfile;
using System.Net.Http.Headers;

namespace Tasks {
    public sealed class WallUpdater : IBackgroundTask {
        BackgroundTaskDeferral _deferral;
        volatile bool _cancelRequested = false;

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

        /// <summary>
        /// Task's Entry Point
        /// </summary>
        /// <param name="taskInstance">Task starting the method</param>
        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance) {
            taskInstance.Canceled += OnCanceled;
            var deferral = taskInstance.GetDeferral();

            SaveTime(taskInstance);

            Photo photo = await GetRandom();

            StorageFile file = await DownloadImagefromServer(photo.URLRegular, photo.Id);

            if (taskInstance.Task.Name == WallTaskName) {
                await SetWallpaperAsync(file);
            } else {
                await SetLockscreenAsync(file);
            }
            
            //SaveLockscreenBackgroundName(lockImage.Name);
            //SaveAppBackground(lockImage);
            deferral.Complete();
        }

        string GetActivityKey(IBackgroundTaskInstance instance) {
            string key = instance.Task.Name == WallTaskName ? WallTaskName : LockscreenTaskName;
            key += "Activity";
            return key;
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
            // Indicate that the background task is canceled.
            _cancelRequested = true;

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
            Photo paper = new Photo();

            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", "16246a4d58baa698a0a720106aab4ecedfe241c72205586da6ab9393424894a8");
            HttpResponseMessage response = null;

            try {
                response = await http.GetAsync("https://api.unsplash.com/photos/random");
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(responseBodyAsText);

                paper.Id = (string)json.GetValue("id");
                paper.Likes = (int)json.GetValue("likes");
                paper.URLRaw = (string)json["urls"]["raw"];
                paper.URLRegular = (string)json["urls"]["regular"];
                paper.Thumbnail = (string)json["urls"]["thumb"];

                return paper;
            } catch/* (HttpRequestException hre)*/ {
                return paper;
            }
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