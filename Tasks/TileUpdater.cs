using System;
using Tasks.Data;
using Tasks.Services;
using Unsplasharp;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Tasks {
    public sealed class TileUpdater: IBackgroundTask {
        #region variables
        BackgroundTaskDeferral _Deferral { get; set; }

        private static string TaskActivity {
            get {
                return "TileActivity";
            }
        }
        #endregion variables

        public async void Run(IBackgroundTaskInstance taskInstance) {
            taskInstance.Canceled += OnCanceled;
            _Deferral = taskInstance.GetDeferral();

            SaveTime(taskInstance);

            var client = new UnsplasharpClient(Credentials.ApplicationId);
            var photos = await client.ListPhotos(page: 1, perPage: 6);

            TileDesigner.UpdatePrimary(photos);

            _Deferral.Complete();
        }

        private void SaveTime(IBackgroundTaskInstance instance) {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            ApplicationDataCompositeValue stats = new ApplicationDataCompositeValue {
                ["DateTime"] = DateTime.Now.ToString(),
                ["Exception"] = null
            };

            localSettings.Values[TaskActivity] = stats;
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
            // Indicate that the background task is canceled.
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            ApplicationDataCompositeValue activityError = new ApplicationDataCompositeValue {
                ["DateTime"] = DateTime.Now.ToLocalTime(),
                ["Exception"] = reason.ToString()
            };

            localSettings.Values[TaskActivity] = activityError;
        }
    }
}
