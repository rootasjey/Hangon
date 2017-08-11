using System;
using Tasks.Data;
using Tasks.Services;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Tasks {
    public sealed class TileUpdater: IBackgroundTask {
        #region variables
        BackgroundTaskDeferral _deferral;
        volatile bool _cancelRequested = false;

        private static string TaskActivity {
            get {
                return "TileActivity";
            }
        }
        #endregion variables

        public async void Run(IBackgroundTaskInstance taskInstance) {
            taskInstance.Canceled += OnCanceled;
            var deferral = taskInstance.GetDeferral();

            SaveTime(taskInstance);

            var client = new Unsplasharp.Client(Credentials.ApplicationId);
            var photos = await client.ListPhotos(page: 1, perPage: 6);

            TileDesigner.UpdatePrimary(photos);

            deferral.Complete();
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
            _cancelRequested = true;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            ApplicationDataCompositeValue activityError = new ApplicationDataCompositeValue {
                ["DateTime"] = DateTime.Now.ToLocalTime(),
                ["Exception"] = reason.ToString()
            };

            localSettings.Values[TaskActivity] = activityError;
        }
    }
}
