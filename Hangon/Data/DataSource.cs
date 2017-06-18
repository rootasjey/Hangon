using System.Threading.Tasks;
using Hangon.Models;

namespace Hangon.Data {
    public class DataSource {
        #region variables
        public PhotosCollection RecentPhotos { get; set; }

        public PhotosCollection CuratedPhotos { get; set; }

        public PhotosCollection PhotosSearchResults { get; set; }

        public PhotosCollection UserPhotos { get; set; }

        public CollectionsCollection UserCollections { get; set; }

        public PhotosCollection CollectionPhotos { get; set; }

        #endregion variables

        #region methods

        public async Task<int> FetchRecentPhotos() {
            if (RecentPhotos == null) RecentPhotos = new PhotosCollection();

            var url = string.Format(Unsplash.GetUrl("photos"));
            if (RecentPhotos.Url == url) return 0;

            RecentPhotos.Clear();
            RecentPhotos.Page = 0;
            RecentPhotos.Url = url;
            return await RecentPhotos.Fetch();
        }

        public async Task<int> FetchCuratedPhotos() {
            if (CuratedPhotos == null) CuratedPhotos = new PhotosCollection();

            var url = string.Format(Unsplash.GetUrl("curated_photos"));
            if (CuratedPhotos.Url == url) return 0;

            CuratedPhotos.Clear();
            CuratedPhotos.Page = 0;
            CuratedPhotos.Url = url;
            return await CuratedPhotos.Fetch();
        }

        public async Task<int> SearchPhotos(string query) {
            if (PhotosSearchResults == null) PhotosSearchResults = new PhotosCollection();

            var url = string.Format(Unsplash.GetUrl("search_photos"));
            if (PhotosSearchResults.Query == query) return 0;

            PhotosSearchResults.Clear();
            PhotosSearchResults.Page = 0;
            PhotosSearchResults.Url = url;
            PhotosSearchResults.Query = query;
            return await PhotosSearchResults.Fetch();
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

        #endregion methods
    }
}
