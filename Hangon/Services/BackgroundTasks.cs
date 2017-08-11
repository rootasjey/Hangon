using System;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Hangon.Services {
    public class BackgroundTasks {
        #region variables
        private static string WallTaskName {
            get {
                return "WallUpdaterTask";
            }
        }

        private static string WallTaskEntryPoint {
            get {
                return "Tasks.WallUpdater";
            }
        }

        private static string WallTaskInterval {
            get {
                return "WallTaskInterval";
            }
        }

        private static string LockscreenTaskName {
            get {
                return "LockscreenUpdaterTask";
            }
        }

        private static string LockscreenTaskEntryPoint {
            get {
                return "Tasks.WallUpdater";
            }
        }

        private static string LockscreenTaskInterval {
            get {
                return "LockscreenTaskInterval";
            }
        }

        private static string TileTaskName {
            get {
                return "TileUpdaterTask";
            }
        }

        private static string TileTaskEntryPoint {
            get {
                return "Tasks.TileUpdater";
            }
        }

        private static string TileTaskInterval {
            get {
                return "TileTaskInterval";
            }
        }
        #endregion variables

        #region tile task
        public static bool IsTileTaskActive() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == TileTaskName) {
                    return true;
                }
            }
            return false;
        }

        public static void RegisterTileTask(uint interval) {
            RegisterBackgroundTask(
                TileTaskName,
                TileTaskEntryPoint,
                interval
            );
        }

        public static void UnregisterTileTask() {
            UnregisterBackgroundTask(TileTaskName);
        }

        public static void SaveTileTaskInterval(uint interval) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[TileTaskInterval] = interval;
        }

        public static uint GetTileTaskInterval() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(TileTaskInterval) ? (uint)settingsValues[TileTaskInterval] : 60;
        }

        public static ApplicationDataCompositeValue GetTileTaskActivity() {
            var key = "TileUpdaterTask" + "Activity";
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(key) ? (ApplicationDataCompositeValue)settingsValues[key] : null;
        }
        #endregion tile task

        #region wall task
        public static bool IsWallTaskActive() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == WallTaskName) {
                    return true;
                }
            }
            return false;
        }

        public static void RegisterWallTask(uint interval) {
            RegisterBackgroundTask(
                WallTaskName,
                WallTaskEntryPoint,
                interval
            );
        }

        public static void UnregisterWallTask() {
            UnregisterBackgroundTask(WallTaskName);
        }

        public static void SaveWallTaskInterval(uint interval) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[WallTaskInterval] = interval;
        }

        public static uint GetWallTaskInterval() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(WallTaskInterval) ? (uint)settingsValues[WallTaskInterval] : 60;
        }

        public static ApplicationDataCompositeValue GetWallTaskActivity() {
            var key = "WallUpdaterTask" + "Activity";
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(key) ? (ApplicationDataCompositeValue)settingsValues[key] : null;
        }
        #endregion wall task

        #region lockscreen task
        public static bool IsLockscreenTaskActive() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == LockscreenTaskName) {
                    return true;
                }
            }
            return false;
        }

        public static void SaveLockscreenTaskInterval(uint interval) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[LockscreenTaskInterval] = interval;
        }

        public static uint GetLockscreenTaskInterval() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LockscreenTaskInterval) ? (uint)settingsValues[LockscreenTaskInterval] : 60;
        }

        public static void RegisterLockscreenTask(uint interval) {
            RegisterBackgroundTask(
                LockscreenTaskName,
                LockscreenTaskEntryPoint,
                interval
            );
        }

        public static void UnregisterLockscreenTask() {
            UnregisterBackgroundTask(LockscreenTaskName);
        }

        public static ApplicationDataCompositeValue GetLockscreenTaskActivity() {
            var key = "LockscreenUpdaterTask" + "Activity";
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(key) ? (ApplicationDataCompositeValue)settingsValues[key] : null;
        }

        #endregion lockscreen task

        private static async void RegisterBackgroundTask(string taskName, string entryPoint, uint interval) {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == taskName) {
                    return;
                }
            }

            BackgroundExecutionManager.RemoveAccess();
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.DeniedByUser ||
                status == BackgroundAccessStatus.DeniedBySystemPolicy) {
                return;
            }

            SystemCondition internetCondition = new SystemCondition(SystemConditionType.InternetAvailable);

            var builder = new BackgroundTaskBuilder() {
                Name = taskName,
                TaskEntryPoint = entryPoint
            };

            var timeTrigger = new TimeTrigger(interval, false);
            builder.SetTrigger(timeTrigger);
            builder.AddCondition(internetCondition);
            BackgroundTaskRegistration taskRegistered = builder.Register();
        }
        
        public static void UnregisterBackgroundTask(string taskName) {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == taskName) {
                    BackgroundExecutionManager.RemoveAccess();
                    task.Value.Unregister(false);
                    break;
                }
            }
        }

    }
}
