using Hangon.Models;
using Hangon.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Unsplasharp;
using Unsplasharp.Models;

namespace Hangon.Data {
    public class DataSource {
        #region variables
        private UnsplasharpClient _Client { get; set; }

        public PhotosList RecentPhotos { get; set; }

        public PhotosList CuratedPhotos { get; set; }

        public PhotosList PhotosSearchResults { get; set; }

        public PhotosList UserPhotos { get; set; }

        public CollectionsList UserCollections { get; set; }

        public PhotosList CollectionPhotos { get; set; }

        public PhotosKeyedCollection LocalFavorites { get; set; }

        #endregion variables

        #region methods

        public DataSource() {
            _Client = new UnsplasharpClient(Credentials.ApplicationId);
        }

        public async Task<int> FetchRecentPhotos() {
            if (RecentPhotos == null) RecentPhotos = new PhotosList();

            var url = GetUrl("photos");
            if (RecentPhotos.Url == url) return 0;

            RecentPhotos.Clear();
            RecentPhotos.Page = 0;
            RecentPhotos.Url = url;
            return await RecentPhotos.Fetch();
        }

        public async Task<int> ReloadRecentPhotos() {
            if (RecentPhotos == null) return 0;

            RecentPhotos.Clear();
            RecentPhotos.Page = 0;
            return await RecentPhotos.Fetch();
        }

        public async Task<int> FetchCuratedPhotos() {
            if (CuratedPhotos == null) CuratedPhotos = new PhotosList();

            var url = string.Format(GetUrl("curated_photos"));
            if (CuratedPhotos.Url == url) return 0;

            CuratedPhotos.Clear();
            CuratedPhotos.Page = 0;
            CuratedPhotos.Url = url;
            return await CuratedPhotos.Fetch();
        }

        public async Task<int> ReloadCuratedPhotos() {
            if (CuratedPhotos == null) return 0;

            CuratedPhotos.Clear();
            CuratedPhotos.Page = 0;
            return await CuratedPhotos.Fetch();
        }

        public async Task<int> SearchPhotos(string query) {
            if (PhotosSearchResults == null) PhotosSearchResults = new PhotosList();

            var url = string.Format(GetUrl("search_photos"));
            if (PhotosSearchResults.Query == query) return 0;

            PhotosSearchResults.Clear();
            PhotosSearchResults.Page = 0;
            PhotosSearchResults.Url = url;
            PhotosSearchResults.Query = query;
            return await PhotosSearchResults.Fetch();
        }

        public async Task<Photo> GetPhoto(string id) {
            return await _Client.GetPhoto(id);
        }

        public async Task<User> GetUser(string username) {
            return await _Client.GetUser(username);
        }

        public async Task<int> GetUserPhotos(string username) {
            if (UserPhotos == null) UserPhotos = new PhotosList();

            var url = string.Format("{0}/{1}/photos", GetUrl("users"), username);
            if (UserPhotos.Url == url) return 0;

            UserPhotos.Clear();
            UserPhotos.Page = 0;
            UserPhotos.Url = url;
            return await UserPhotos.Fetch();
        }

        public async Task<int> GetUserCollections(string username) {
            if (UserCollections == null) UserCollections = new CollectionsList();

            var url = string.Format("{0}/{1}/collections", GetUrl("users"), username);
            if (UserCollections.Url == url) return 0;
            
            UserCollections.Clear();
            UserCollections.Page = 0;
            UserCollections.Url = url;
            return await UserCollections.Fetch();
        }

        public async Task<Collection> GetCollection(string id) {
            return await _Client.GetCollection(id);
        }

        public async Task<int> GetCollectionPhotos(string collectionId) {
            if (CollectionPhotos == null) CollectionPhotos = new PhotosList();

            var url = string.Format("{0}/{1}/photos", GetUrl("collections"), collectionId);
            if (CollectionPhotos.Url == url) return 0;

            CollectionPhotos.Clear();
            CollectionPhotos.Page = 0;
            CollectionPhotos.Url = url;
            return await CollectionPhotos.Fetch();
        }

        private static string BaseURI {
            get {
                return "https://api.unsplash.com/";
            }
        }

        private static IDictionary<string, string> Endpoints = new Dictionary<string, string>() {
            {"photos", "photos" },
            {"curated_photos", "photos/curated" },
            {"search", "search" },
            {"search_photos", "search/photos" },
            {"users", "users" },
            {"collections", "collections" }
        };

        public static string GetUrl(string type) {
            switch (type) {
                case "photos":
                    return BaseURI + Endpoints["photos"];
                case "curated_photos":
                    return BaseURI + Endpoints["curated_photos"];
                case "users":
                    return BaseURI + Endpoints["users"];
                case "search":
                    return BaseURI + Endpoints["search"];
                case "search_photos":
                    return BaseURI + Endpoints["search_photos"];
                case "collections":
                    return BaseURI + Endpoints["collections"];
                default:
                    return null;
            }
        }
        

        public string GetProfileImageLink(User user) {
            if (user == null || user.ProfileImage == null) return "";

            return user.ProfileImage.Medium ??
                    user.ProfileImage.Large ??
                    user.ProfileImage.Small;
        }

        public string GetUsernameFormated(User user) {
            return user.Username ??
                   user.Name ??
                   string.Format("{0} {1}", user.FirstName, user.LastName);
        }

        public string GetUsername(User user) {
            return user.Username ??
                   string.Format("{0}{1}", user.FirstName, user.LastName).ToLower();
        }

        public async Task LoadLocalFavorites() {
            if (LocalFavorites == null) {
                var savedFavorites = await Settings.GetFavorites();

                if (savedFavorites == null) {
                    LocalFavorites = new PhotosKeyedCollection();
                    return;
                }

                LocalFavorites = savedFavorites;
            }
        }

        public async Task AddToFavorites(Photo photo) {
            if (LocalFavorites.Contains(photo.Id)) { return; }

            LocalFavorites.Add(photo);
            await Settings.SaveFavorites(LocalFavorites);
        }

        public async Task RemoveFromFavorites(Photo photo) {
            if (!LocalFavorites.Contains(photo.Id)) { return; }

            LocalFavorites.Remove(photo.Id);
            await Settings.SaveFavorites(LocalFavorites);
        }

        #endregion methods
    }

}
