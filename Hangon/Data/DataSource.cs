using System.Threading.Tasks;
using Hangon.Models;

namespace Hangon.Data {
    public class DataSource {
        public RecentWallpapers Recent { get; set; }

        public async Task<int> FetchRecent() {
            Recent = new RecentWallpapers();
            return await Recent.FetchRecent();
        }

        public async Task<Photo> GetPhoto(string id) {
            var result = await Unsplash.GetPhoto(id);
            return result;
        }
    }
}
