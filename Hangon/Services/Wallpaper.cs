using System;
using Hangon.Models;
using Windows.System.UserProfile;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Windows.Web.Http;
using Windows.Storage.Streams;

namespace Hangon.Services {
    public class Wallpaper {

        public static async Task<bool> SetAsWallpaper(Photo photo) {
            bool success = false;

            var file = await DownloadImage(photo.Urls.Regular, photo.Id);

            if (UserProfilePersonalizationSettings.IsSupported()) {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetWallpaperImageAsync(file);
            }
            return success;
        }

        public static async Task<bool> SetAsLockscreen(Photo photo) {
            bool success = false;

            var file = await DownloadImage(photo.Urls.Regular, photo.Id);

            if (UserProfilePersonalizationSettings.IsSupported()) {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetLockScreenImageAsync(file);
            }
            return success;
        }

        private static async Task<StorageFile> DownloadImage(string URI, string filename) {
            filename += ".png";
            var rootFolder = ApplicationData.Current.LocalFolder;
            var coverpic = await rootFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            try {
                var client = new System.Net.Http.HttpClient();
                byte[] buffer = await client.GetByteArrayAsync(URI); // Download file
                using (Stream stream = await coverpic.OpenStreamForWriteAsync())
                    stream.Write(buffer, 0, buffer.Length); // Save

                return coverpic;
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Take a photo object and save it Pictures Library
        /// </summary>
        /// <param name="photo">Pepresents my image model, you can replace it by a string</param>
        /// <param name="httpProgressCallback">A function callback which will be fired when the HTTP progression is updated</param>
        /// <returns></returns>
        public static async Task SaveToPicturesLibrary(Photo photo, Action<HttpProgress> httpProgressCallback) {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker() {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
            };

            savePicker.FileTypeChoices.Add("Picture", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = photo.Id;
            
            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null) {
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                
                var client = new HttpClient();
                Progress<HttpProgress> progressCallback = new Progress<HttpProgress>(httpProgressCallback);
                var tokenSource = new CancellationTokenSource();

                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(photo.Urls.Raw));
                HttpResponseMessage response = await client.SendRequestAsync(request)
                    .AsTask(tokenSource.Token, progressCallback);

                IInputStream inputStream = await response.Content.ReadAsInputStreamAsync();
                IOutputStream outputStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                await RandomAccessStream.CopyAndCloseAsync(inputStream, outputStream);

                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                Windows.Storage.Provider.FileUpdateStatus status =
                    await CachedFileManager.CompleteUpdatesAsync(file);

                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete) {
                    DataTransfer.ShowLocalToast("The photo was saved.");
                } else {
                    DataTransfer.ShowLocalToast("The photo couldn't be saved.");
                }

                // Free resources
                client.Dispose();
                inputStream.Dispose();
                outputStream.Dispose();
            }
        }
    }
}
