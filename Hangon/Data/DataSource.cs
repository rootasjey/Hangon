using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Hangon.Services;

namespace Hangon.Data {
    public class DataSource {
        //public ObservableCollection<Wallpaper> Recent { get; set; }
        public RecentWallpapers Recent { get; set; }

        public async Task<int> FetchRecent() {
            Recent = new RecentWallpapers();
            return await Recent.FetchRecent();
        }

        public async Task<StorageFile> DownloadImagefromServer(string URI, string filename) {
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

        public async void SavePicture(BitmapImage bitmap) {
            try {
                await Storage<BitmapImage>.SaveObjectsAsync(bitmap, "wall.png");
            } catch {
                return;
            }
        }

        public async Task<BitmapImage> GetPicture() {
            try {
                BitmapImage image = await Storage<BitmapImage>.RestoreObjectsAsync("wall.png");
                return image;
            } catch {
                return null;
            }
        }
    }
}
