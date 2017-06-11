using Hangon.Models;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Data;
using System;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Hangon.Data {
    public class Unsplash {
        public static string ApplicationId {
            get {
                return "16246a4d58baa698a0a720106aab4ecedfe241c72205586da6ab9393424894a8";
            }
        }

        private static string BaseURI {
            get {
                return "https://api.unsplash.com/";
            }
        }

        private static IDictionary<string, string> Endpoints = new Dictionary<string, string>() {
            {"photos", "photos" },
            {"search", "search" },
            {"users", "users" },
            {"collections", "collections" }
        };

        public static string GetUrl(string type) {
            switch (type) {
                case "photos":
                    return BaseURI + Endpoints["photos"];
                case "users":
                    return BaseURI + Endpoints["users"];
                case "search":
                    return BaseURI + Endpoints["search"];
                case "collections":
                    return BaseURI + Endpoints["collections"];
                default:
                    return null;
            }
        }

        public static async Task<Photo> GetPhoto(string id) {
            var url = string.Format("{0}/{1}/{2}", BaseURI, Endpoints["photos"], id);
            var response = await Fetch(url);

            if (response == null) return null;
            var parsedResponse = JObject.Parse(response);

            var photo = new Photo() {
                Id = id,
                Color = (string)parsedResponse["color"],
                CreatedAt = (string)parsedResponse["created_at"],
                UpdatedAt = (string)parsedResponse["updated_at"],
                Downloads = (int)parsedResponse["downloads"],
                Likes = (int)parsedResponse["likes"],
                IsLikedByUser = (bool)parsedResponse["liked_by_user"],
                Width = (int)parsedResponse["width"],
                Height = (int)parsedResponse["height"],

                Exif = new Exif() {
                    Make = (string)parsedResponse["exif"]["make"],
                    Model = (string)parsedResponse["exif"]["model"],
                    Iso = (int?)parsedResponse["exif"]["iso"],
                    FocalLength = (string)parsedResponse["exif"]["focal_length"],
                    Aperture = (string)parsedResponse["exif"]["aperture"],
                },

                Location = string.IsNullOrEmpty((string)parsedResponse.GetValue("Location")) ?
                    null :
                    new Location() {
                        City = (string)parsedResponse["location"]["city"],
                        Country = (string)parsedResponse["location"]["country"],

                        Position = new Position() {
                            Latitude = (int)parsedResponse["location"]["position"]["latitude"],
                            Longitude = (int)parsedResponse["location"]["posityion"]["longitude"],
                        }
                    },

                User = new User() {
                    Id = (string)parsedResponse["user"]["id"],
                    Username = (string)parsedResponse["user"]["username"],
                    Name = (string)parsedResponse["user"]["name"],
                    Bio = (string)parsedResponse["user"]["bio"],
                    Location = (string)parsedResponse["user"]["location"],
                    PortfolioUrl = (string)parsedResponse["user"]["portfolio_url"],
                    TotalLikes = (int)parsedResponse["user"]["total_likes"],
                    TotalPhotos = (int)parsedResponse["user"]["total_photos"],
                    TotalCollections = (int)parsedResponse["user"]["total_collections"],

                    Links = new UserLinks() {
                        Self = (string)parsedResponse["user"]["links"]["self"],
                        Html = (string)parsedResponse["user"]["links"]["html"],
                        Photos = (string)parsedResponse["user"]["links"]["photos"],
                        Likes = (string)parsedResponse["user"]["links"]["likes"],
                        Portfolio = (string)parsedResponse["user"]["links"]["portfolio"],
                    },

                    ProfileImage = new ProfileImage() {
                        Large = (string)parsedResponse["user"]["profile_image"]["large"],
                        Medium = (string)parsedResponse["user"]["profile_image"]["medium"],
                        Small = (string)parsedResponse["user"]["profile_image"]["small"],
                    }
                },

                Urls = new Urls() {
                    Raw = (string)parsedResponse["urls"]["raw"],
                    Full = (string)parsedResponse["urls"]["full"],
                    Regular = (string)parsedResponse["urls"]["regular"],
                    Thumbnail = (string)parsedResponse["urls"]["thumb"],
                }
            };

            return photo;
        }

        public static async Task<User> GetUser(string username) {
            var url = string.Format("{0}/{1}/{2}", BaseURI, Endpoints["users"], username);
            var response = await Fetch(url);

            if (response == null) return null;
            var parsedResponse = JObject.Parse(response);

            var user = new User() {
                Id = (string)parsedResponse["id"],
                UpdatedAt = (string)parsedResponse["updated_at"],
                FirstName = (string)parsedResponse["first_name"],
                LastName = (string)parsedResponse["last_name"],
                PortfolioUrl = (string)parsedResponse["portfolio_url"],
                Bio = (string)parsedResponse["bio"],
                Location = (string)parsedResponse["location"],
                TotalLikes = (int)parsedResponse["total_likes"],
                TotalPhotos = (int)parsedResponse["total_photos"],
                TotalCollections = (int)parsedResponse["total_collections"],
                FollowedByUser = (bool)parsedResponse["followed_by_user"],
                FollowersCount=(int)parsedResponse["followers_count"],
                FollowingCount=(int)parsedResponse["following_count"],

                ProfileImage = new ProfileImage() {
                    Small = (string)parsedResponse["profile_image"]["small"],
                    Medium = (string)parsedResponse["profile_image"]["medium"],
                    Large = (string)parsedResponse["profile_image"]["large"]
                },

                Badge = parsedResponse["badge"].Type == JTokenType.String || 
                        parsedResponse["badge"].Type == JTokenType.Null ? 
                        null : 
                        new Badge() {
                            Title = (string)parsedResponse["badge"]["title"],
                            Primary = (bool)parsedResponse["badge"]["primary"],
                            Slug = (string)parsedResponse["badge"]["slug"],
                            Link = (string)parsedResponse["badge"]["link"],
                        },

                Links = new UserLinks() {
                    Self = (string)parsedResponse["links"]["self"],
                    Html = (string)parsedResponse["links"]["html"],
                    Photos = (string)parsedResponse["links"]["photos"],
                    Likes = (string)parsedResponse["links"]["likes"],
                    Portfolio = (string)parsedResponse["links"]["portfolio"],
                }
            };

            return user;
        }

        public static async Task<Collection> GetCollection(string id) {
            var url = string.Format("{0}/{1}/{2}", BaseURI, Endpoints["collections"], id);
            var response = await Fetch(url);

            if (response == null) return null;
            var parsedResponse = JObject.Parse(response);

            return new Collection() {
                Id = id,
                Title = (string)parsedResponse["title"],
                Description = (string)parsedResponse["description"],
                PublishedAt = (string)parsedResponse["published_at"],
                UpdatedAt = (string)parsedResponse["updated_at"],
                IsCurated = (bool)parsedResponse["curated"],
                IsFeatured = (bool)parsedResponse["featured"],
                TotalPhotos = (int)parsedResponse["total_photos"],
                IsPrivate = (bool)parsedResponse["private"],
                ShareKey = (string)parsedResponse["share_key"],

                CoverPhoto = new Photo() {
                    Id = (string)parsedResponse["cover_photo"]["id"],
                    Width = (int)parsedResponse["cover_photo"]["width"],
                    Height = (int)parsedResponse["cover_photo"]["height"],
                    Color = (string)parsedResponse["cover_photo"]["color"],
                    Likes = (int)parsedResponse["cover_photo"]["likes"],
                    IsLikedByUser = (bool)parsedResponse["cover_photo"]["liked_by_user"],
                    CreatedAt = (string)parsedResponse["cover_photo"]["created_at"],
                    UpdatedAt = (string)parsedResponse["cover_photo"]["updated_at"],

                    User = ExtractUser(parsedResponse["cover_photo"]["user"]),

                    Urls = new Urls() {
                        Raw = (string)parsedResponse["cover_photo"]["urls"]["raw"],
                        Full = (string)parsedResponse["cover_photo"]["urls"]["full"],
                        Regular = (string)parsedResponse["cover_photo"]["urls"]["regular"],
                        Small = (string)parsedResponse["cover_photo"]["urls"]["small"],
                        Thumbnail = (string)parsedResponse["cover_photo"]["urls"]["thumb"],
                    },

                    Categories = ExtractCategories(parsedResponse["cover_photo"]["categories"]),

                    Links = new PhotoLinks() {
                        Self = (string)parsedResponse["cover_photo"]["links"]["self"],
                        Html = (string)parsedResponse["cover_photo"]["links"]["html"],
                        Download = (string)parsedResponse["cover_photo"]["links"]["download"],
                        DownloadLocation = (string)parsedResponse["cover_photo"]["links"]["download_location"],
                    }
                },

                User = ExtractUser(parsedResponse["user"]),

                Links = new CollectionLinks() {
                    Self = (string)parsedResponse["links"]["self"],
                    Html = (string)parsedResponse["links"]["html"],
                    Photos = (string)parsedResponse["links"]["photos"],
                    Related = (string)parsedResponse["links"]["related"]
                }
            };
        }

        private static async Task<string> Fetch(string url) {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Client-ID", Unsplash.ApplicationId);
            HttpResponseMessage response = null;

            try {
                response = await http.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();
                return responseBodyAsText;

            } catch {
                return null;
            }
        }

        public static List<Categories> ExtractCategories(JToken data) {
            var categories = new List<Categories>();
            if (data == null) return categories;

            var dataCategories = (JArray)data;

            if (dataCategories.Count == 0) return categories;

            foreach (JObject item in dataCategories) {
                var category = new Categories() {
                    Id = (string)item["id"],
                    Title = (string)item["title"],
                    PhotoCount = (int)item["photo_count"],
                    Links = new CategoriesLinks() {
                        Self = (string)item["links"]["self"],
                        Photos = (string)item["links"]["photos"],
                    }
                };

                categories.Add(category);
            }

            return categories;
        }

        public static User ExtractUser(JToken data) {
            return new User() {
                Id = (string)data["id"],
                UpdatedAt = (string)data["updated_at"],
                Username = (string)data["username"],
                Name = (string)data["name"],
                PortfolioUrl = (string)data["portfolio_url"],
                Bio = (string)data["bio"],
                Location = (string)data["location"],
                TotalLikes = (int)data["total_likes"],
                TotalPhotos = (int)data["total_photos"],
                TotalCollections = (int)data["total_collections"],

                ProfileImage = new ProfileImage() {
                    Small = (string)data["profile_image"]["small"],
                    Medium = (string)data["profile_image"]["medium"],
                    Large = (string)data["profile_image"]["large"],
                },

                Links = new UserLinks() {
                    Self = (string)data["links"]["self"],
                    Html = (string)data["links"]["html"],
                    Photos = (string)data["links"]["photos"],
                    Likes = (string)data["links"]["likes"],
                    Portfolio = (string)data["links"]["portfolio"],
                }
            };
        }

        public static string GetProfileImageLink(User user) {
            if (user == null || user.ProfileImage == null) return "";

            return user.ProfileImage.Medium ??
                    user.ProfileImage.Large ??
                    user.ProfileImage.Small;
        }
    }

    public class PhotosCollection: ObservableCollection<Photo>, ISupportIncrementalLoading {
        public string Url { get; set; }
        public int Page { get; set; }
        public bool HasMoreItems { get; set; }

        public async Task<int> Fetch() {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Client-ID", Unsplash.ApplicationId);
            HttpResponseMessage response = null;

            HasMoreItems = true;

            Page++;
            int added = 0;
            string fetchURL = string.Format("{0}?page={1}", Url, Page);

            try {
                response = await http.GetAsync(fetchURL);
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                JArray jsonList = JArray.Parse(responseBodyAsText);

                foreach (JObject item in jsonList) {
                    var photo = new Photo() {
                        Id = (string)item["id"],
                        Width = (int)item["width"],
                        Height = (int)item["height"],
                        Color = (string)item["color"],
                        Likes = (int)item["likes"],
                        IsLikedByUser = (bool)item["liked_by_user"],

                        User = new User() {
                            Id = (string)item["user"]["id"],
                            Username = (string)item["user"]["username"],
                            Name = (string)item["user"]["name"],
                            PortfolioUrl = (string)item["user"]["portfolio_url"],
                            Bio = (string)item["user"]["bio"],
                            Location = (string)item["user"]["location"],
                            TotalLikes = (int)item["user"]["total_likes"],
                            TotalPhotos = (int)item["user"]["total_photos"],
                            TotalCollections = (int)item["user"]["total_collections"],

                            ProfileImage = new ProfileImage() {
                                Small = (string)item["user"]["profile_image"]["small"],
                                Medium = (string)item["user"]["profile_image"]["medium"],
                                Large = (string)item["user"]["profile_image"]["large"],
                            },

                            Links = new UserLinks() {
                                Self = (string)item["user"]["links"]["self"],
                                Html = (string)item["user"]["links"]["html"],
                                Photos = (string)item["user"]["links"]["photos"],
                                Likes = (string)item["user"]["links"]["likes"],
                                Portfolio = (string)item["user"]["links"]["portfolio"],
                            }
                        },

                        Urls = new Urls() {
                            Raw = (string)item["urls"]["raw"],
                            Full = (string)item["urls"]["full"],
                            Regular = (string)item["urls"]["regular"],
                            Small = (string)item["urls"]["small"],
                            Thumbnail = (string)item["urls"]["thumb"],
                        },

                        Categories = Unsplash.ExtractCategories(item["categories"]),

                        Links = new PhotoLinks() {
                            Self = (string)item["links"]["self"],
                            Html = (string)item["links"]["html"],
                            Download = (string)item["links"]["download"],
                        }
                    };

                    Add(photo);
                    added++;
                }

                if (added == 0) HasMoreItems = false;
                return added;

            } catch /*(HttpRequestException hre)*/ {
                HasMoreItems = false;
                return 0;
            }
        }

        IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count) {
            return LoadMore(count).AsAsyncOperation();
        }

        public virtual async Task<LoadMoreItemsResult> LoadMore(uint count) {
            var added = await Fetch();
            return new LoadMoreItemsResult() { Count = (uint)added };
        }
    }

    public class CollectionsCollection: ObservableCollection<Collection>, ISupportIncrementalLoading {
        public string Url { get; set; }
        public int Page { get; set; }
        public bool HasMoreItems { get; set; }

        public async Task<int> Fetch() {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Client-ID", Unsplash.ApplicationId);
            HttpResponseMessage response = null;

            HasMoreItems = true;

            Page++;
            int added = 0;
            string fetchURL = string.Format("{0}?page={1}", Url, Page);

            try {
                response = await http.GetAsync(fetchURL);
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                JArray jsonList = JArray.Parse(responseBodyAsText);

                foreach (JObject item in jsonList) {
                    var collection = new Collection() {
                        Id = (string)item["id"],
                        Title = (string)item["title"],
                        Description = (string)item["description"],
                        PublishedAt = (string)item["published_at"],
                        UpdatedAt = (string)item["updated_at"],
                        IsCurated = (bool)item["curated"],
                        TotalPhotos = (int)item["total_photos"],
                        IsPrivate=(bool)item["private"],
                        ShareKey = (string)item["share_key"],

                        CoverPhoto = item["cover_photo"].Type == JTokenType.Null ||
                                     item["cover_photo"].Type == JTokenType.String ?
                                    null :
                                    new Photo() {
                                        Id = (string)item["cover_photo"]["id"],
                                        Width = (int)item["cover_photo"]["width"],
                                        Height = (int)item["cover_photo"]["height"],
                                        Color = (string)item["cover_photo"]["color"],
                                        Likes = (int)item["cover_photo"]["likes"],
                                        IsLikedByUser = (bool)item["cover_photo"]["liked_by_user"],

                                        User = new User() {
                                            Id = (string)item["cover_photo"]["user"]["id"],
                                            Username = (string)item["cover_photo"]["user"]["username"],
                                            Name = (string)item["cover_photo"]["user"]["name"],
                                            PortfolioUrl = (string)item["cover_photo"]["user"]["portfolio_url"],
                                            Bio = (string)item["cover_photo"]["user"]["bio"],
                                            Location = (string)item["cover_photo"]["user"]["location"],
                                            TotalLikes = (int)item["cover_photo"]["user"]["total_likes"],
                                            TotalPhotos = (int)item["cover_photo"]["user"]["total_photos"],
                                            TotalCollections = (int)item["cover_photo"]["user"]["total_collections"],

                                            ProfileImage = new ProfileImage() {
                                                Small = (string)item["cover_photo"]["user"]["profile_image"]["small"],
                                                Medium = (string)item["cover_photo"]["user"]["profile_image"]["medium"],
                                                Large = (string)item["cover_photo"]["user"]["profile_image"]["large"],
                                            },

                                            Links = new UserLinks() {
                                                Self = (string)item["cover_photo"]["user"]["links"]["self"],
                                                Html = (string)item["cover_photo"]["user"]["links"]["html"],
                                                Photos = (string)item["cover_photo"]["user"]["links"]["photos"],
                                                Likes = (string)item["cover_photo"]["user"]["links"]["likes"],
                                                Portfolio = (string)item["cover_photo"]["user"]["links"]["portfolio"],
                                            }
                                        },

                                        Urls = new Urls() {
                                            Raw = (string)item["cover_photo"]["urls"]["raw"],
                                            Full = (string)item["cover_photo"]["urls"]["full"],
                                            Regular = (string)item["cover_photo"]["urls"]["regular"],
                                            Small = (string)item["cover_photo"]["urls"]["small"],
                                            Thumbnail = (string)item["cover_photo"]["urls"]["thumb"],
                                        },

                                        Categories =  Unsplash.ExtractCategories(item["cover_photo"]["categories"]),

                                        Links = new PhotoLinks() {
                                            Self = (string)item["cover_photo"]["links"]["self"],
                                            Html = (string)item["cover_photo"]["links"]["html"],
                                            Download = (string)item["cover_photo"]["links"]["download"],
                                        }
                                    },

                        User = new User() {
                            Id = (string)item["user"]["id"],
                            UpdatedAt = (string)item["user"]["updated_at"],
                            Username = (string)item["user"]["username"],
                            Name = (string)item["user"]["name"],
                            PortfolioUrl = (string)item["user"]["portfolio_url"],
                            Bio = (string)item["user"]["bio"],
                            Location = (string)item["user"]["location"],
                            TotalLikes = (int)item["user"]["total_likes"],
                            TotalPhotos = (int)item["user"]["total_photos"],
                            TotalCollections = (int)item["user"]["total_collections"],

                            ProfileImage = new ProfileImage() {
                                Small = (string)item["user"]["profile_image"]["small"],
                                Medium = (string)item["user"]["profile_image"]["mediuim"],
                                Large = (string)item["user"]["profile_image"]["large"],
                            },

                            Links = new UserLinks() {
                                Self = (string)item["user"]["links"]["self"],
                                Html = (string)item["user"]["links"]["html"],
                                Photos = (string)item["user"]["links"]["photos"],
                                Likes = (string)item["user"]["links"]["likes"],
                                Portfolio = (string)item["user"]["links"]["portfolio"],
                            }
                        },

                        Links = new CollectionLinks() {
                            Self = (string)item["links"]["self"],
                            Html = (string)item["links"]["html"],
                            Photos = (string)item["links"]["photos"],
                            Related = (string)item["links"]["related"],
                        }
                    };

                    Add(collection);
                    added++;
                }

                if (added == 0) HasMoreItems = false;
                return added;

            } catch /*(HttpRequestException hre)*/ {
                HasMoreItems = false;
                return 0;
            }
        }

        IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count) {
            return LoadMore(count).AsAsyncOperation();
        }

        public virtual async Task<LoadMoreItemsResult> LoadMore(uint count) {
            var added = await Fetch();
            return new LoadMoreItemsResult() { Count = (uint)added };
        }
    }
}
