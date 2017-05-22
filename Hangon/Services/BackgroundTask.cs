using System;
using Windows.ApplicationModel.Background;

namespace Hangon.Services {
    public class BackgroundTask {        
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

        public static bool IsLockscreenTaskActive() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == LockscreenTaskName) {
                    return true;
                }
            }
            return false;
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

        public static async void RegisterBackgroundTask(string taskName, string entryPoint, uint interval) {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == taskName) {
                    return;
                }
            }

            BackgroundExecutionManager.RemoveAccess();
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.Denied) {
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
