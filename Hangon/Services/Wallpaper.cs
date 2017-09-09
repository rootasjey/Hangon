using System;
using Windows.System.UserProfile;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.Generic;
using System.Threading;
using Windows.Web.Http;
using Windows.Storage.Streams;
using Unsplasharp.Models;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using Windows.Foundation;
using System.Numerics;

namespace Hangon.Services {
    public class Wallpaper {
        public static async Task<bool> SetAsWallpaper(Photo photo, Action<HttpProgress> httpProgressCallback) {
            bool success = false;

            var urlFormat = ChooseBestPhotoFormat(photo);

            var file = await DownloadImage(urlFormat, photo.Id, httpProgressCallback);
            if (file == null) return success;

            if (UserProfilePersonalizationSettings.IsSupported()) {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetWallpaperImageAsync(file);
            }
            return success;
        }

        public static async Task<bool> SetAsLockscreen(Photo photo, Action<HttpProgress> httpProgressCallback) {
            bool success = false;

            var urlFormat = ChooseBestPhotoFormat(photo);

            var file = await DownloadImage(urlFormat, photo.Id, httpProgressCallback);
            if (file == null) return success;

            if (UserProfilePersonalizationSettings.IsSupported()) {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetLockScreenImageAsync(file);
            }
            return success;
        }

        private static async Task<StorageFile> DownloadImage(string URI, string filename, Action<HttpProgress> httpProgressCallback) {
            filename += ".png";
            var rootFolder = ApplicationData.Current.LocalFolder;
            var coverpic = await rootFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            //try {
            //    var client = new System.Net.Http.HttpClient();
            //    byte[] buffer = await client.GetByteArrayAsync(URI); // Download file
            //    using (Stream stream = await coverpic.OpenStreamForWriteAsync())
            //        stream.Write(buffer, 0, buffer.Length); // Save

            //    return coverpic;
            //} catch {
            //    return null;
            //}
            try {
                var client = new HttpClient();
                Progress<HttpProgress> progressCallback = new Progress<HttpProgress>(httpProgressCallback);
                var tokenSource = new CancellationTokenSource();

                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(URI));
                HttpResponseMessage response = await client.SendRequestAsync(request)
                    .AsTask(tokenSource.Token, progressCallback);

                IInputStream inputStream = await response.Content.ReadAsInputStreamAsync();
                IOutputStream outputStream = await coverpic.OpenAsync(FileAccessMode.ReadWrite);
                await RandomAccessStream.CopyAndCloseAsync(inputStream, outputStream);
                return coverpic;

            } catch {
                await coverpic.DeleteAsync();
                return null;
            }
            
        }

        /// <summary>
        /// Take a photo object and save it Pictures Library
        /// </summary>
        /// <param name="photo">Pepresents my image model, you can replace it by a string</param>
        /// <param name="httpProgressCallback">A function callback which will be fired when the HTTP progression is updated</param>
        /// <returns></returns>
        public static async Task<bool> SaveToPicturesLibrary(Photo photo, Action<HttpProgress> httpProgressCallback, string url = "") {
            StorageFile file;

            if (Settings.UseDefaultDownloadPath()) {
                file = await GetFileFromDefaultLocation();
            } else {
                file = await GetFileFromPicker();
            }


            if (file == null) {
                //DataTransfer.ShowLocalToast("The photo couldn't be saved.");
                return false;
            }

            // Prevent updates to the remote version of the file until
            // we finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(file);

            var _url = string.IsNullOrEmpty(url) ? photo.Urls.Raw : url;

            var client = new HttpClient();
            Progress<HttpProgress> progressCallback = new Progress<HttpProgress>(httpProgressCallback);
            var tokenSource = new CancellationTokenSource();

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_url));
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

            // Free resources
            client.Dispose();
            inputStream.Dispose();
            outputStream.Dispose();

            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete) {
                //DataTransfer.ShowLocalToast("The photo was saved.");
                return true;
            } else {
                //DataTransfer.ShowLocalToast("The photo couldn't be saved.");
                return false;
            }
                       

            async Task<StorageFile> GetFileFromDefaultLocation()
            {
                StorageFolder storageFolder = KnownFolders.SavedPictures;
                return await storageFolder.CreateFileAsync(string.Format("{0}.jpg", photo.Id), CreationCollisionOption.ReplaceExisting);
            }

            async Task<StorageFile> GetFileFromPicker()
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker() {
                    SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
                };

                savePicker.FileTypeChoices.Add("Picture", new List<string>() { ".jpg" });
                savePicker.SuggestedFileName = photo.Id;

                return await savePicker.PickSaveFileAsync();
            }
        }
        
        private static string ChooseBestPhotoFormat(Photo photo) {
            var url = photo.Urls.Raw;

            var format = GetBestPhotoFormat();

            switch (format) {
                case "small":
                    url = photo.Urls.Small;
                    break;
                case "regular":
                    url = photo.Urls.Regular;
                    break;
                case "full":
                    url = photo.Urls.Full;
                    break;
                default:
                    break;
            }

            return url;
        }

        public static string GetBestPhotoFormat() {
            // 1-Get the device screen's resolution/size
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);

            // 2-Setup levels
            var sizeSmall = new Vector2(400, 600);
            var sizeRegular = new Vector2(1080, 720);

            if (size.Width < sizeSmall.X || size.Height < sizeSmall.Y) {
                return "small";
            }
            if (size.Width < sizeRegular.X || size.Height < sizeRegular.Y) {
                return "regular";
            }
            if (size.Width > sizeRegular.X || size.Height > sizeRegular.Y) {
                return "full";
            }

            return "regular";
        }
    }
}
