using System.Threading.Tasks;
using Hangon.Models;

namespace Hangon.Data {
    public class DataSource {
        public PhotosCollection NewPhotos { get; set; }

        public PhotosCollection UserPhotos { get; set; }

        public CollectionsCollection UserCollections { get; set; }

        public PhotosCollection CollectionPhotos { get; set; }

        public async Task<int> FetchRecent() {
            if (NewPhotos == null) NewPhotos = new PhotosCollection();

            var url = string.Format(Unsplash.GetUrl("photos"));
            if (NewPhotos.Url == url) return 0;

            NewPhotos.Clear();
            NewPhotos.Page = 0;
            NewPhotos.Url = url;
            return await NewPhotos.Fetch();
        }

        public async Task<Photo> GetPhoto(string id) {
            return await Unsplash.GetPhoto(id);
        }

        public async Task<User> GetUser(string username) {
            return await Unsplash.GetUser(username);
        }

        public async Task<int> GetUserPhotos(string username) {
            if (UserPhotos == null) UserPhotos = new PhotosCollection();

            var url = string.Format("{0}/{1}/photos", Unsplash.GetUrl("users"), username);
            if (UserPhotos.Url == url) return 0;

            UserPhotos.Clear();
            UserPhotos.Page = 0;
            UserPhotos.Url = url;
            return await UserPhotos.Fetch();
        }

        public async Task<int> GetUserCollections(string username) {
            if (UserCollections == null) UserCollections = new CollectionsCollection();

            var url = string.Format("{0}/{1}/collections", Unsplash.GetUrl("users"), username);
            if (UserCollections.Url == url) return 0;

            UserCollections.Clear();
            UserCollections.Page = 0;
            UserCollections.Url = url;
            return await UserCollections.Fetch();
        }

        public async Task<Collection> GetCollection(string id) {
            return await Unsplash.GetCollection(id);
        }

        public async Task<int> GetCollectionPhotos(string collectionId) {
            if (CollectionPhotos == null) CollectionPhotos = new PhotosCollection();

            var url = string.Format("{0}/{1}/photos", Unsplash.GetUrl("collections"), collectionId);
            if (CollectionPhotos.Url == url) return 0;

            CollectionPhotos.Clear();
            CollectionPhotos.Page = 0;
            CollectionPhotos.Url = url;
            return await CollectionPhotos.Fetch();
        }
    }
}
