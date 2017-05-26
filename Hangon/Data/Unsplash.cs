using Hangon.Models;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Data;
using System;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Hangon.Data {
    public class Unsplash {
        public RecentWallpapers Recent { get; set; }

        private static string _APIKEY {
            get {
                return "16246a4d58baa698a0a720106aab4ecedfe241c72205586da6ab9393424894a8";
            }
        }

        private static string _BaseURI {
            get {
                return "https://api.unsplash.com/photos";
            }
        }

        public static async Task<Photo> GetPhoto(string id) {
            var url = string.Format("{0}/{1}", _BaseURI, id);
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
                LikedByUser = (bool)parsedResponse["liked_by_user"],
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

        private static async Task<string> Fetch(string url) {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", "16246a4d58baa698a0a720106aab4ecedfe241c72205586da6ab9393424894a8");
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
    }

    public class RecentWallpapers: ObservableCollection<Photo>, ISupportIncrementalLoading {
        public string URL {
            get {
                return "https://api.unsplash.com/photos";
            }
        }

        public int Page { get; set; }

        public async Task<int> FetchRecent() {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", "16246a4d58baa698a0a720106aab4ecedfe241c72205586da6ab9393424894a8");
            HttpResponseMessage response = null;

            HasMoreItems = true;

            Page++;
            int added = 0;
            string fetchURL = string.Format("{0}?page={1}", URL, Page);

            try {
                response = await http.GetAsync(fetchURL);
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                JArray jsonList = JArray.Parse(responseBodyAsText);

                foreach (JObject jsonItem in jsonList) {
                    var wallpaper = new Photo() {
                        Id = (string)jsonItem.GetValue("id"),
                        Urls = new Urls() {
                            Raw = (string)jsonItem["urls"]["raw"],
                            Regular = (string)jsonItem["urls"]["regular"],
                            Thumbnail = (string)jsonItem["urls"]["thumb"],
                            Full = (string)jsonItem["urls"]["full"],
                            Small = (string)jsonItem["urls"]["small"]
                        }
                    };

                    Add(wallpaper);
                    added++;
                }

                if (added == 0) HasMoreItems = false;
                return added;

            } catch /*(HttpRequestException hre)*/ {
                HasMoreItems = false;
                return 0;
            }
        }
        
        public bool HasMoreItems { get; set; }

        IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count) {
            return LoadMore(count).AsAsyncOperation();
        }

        public virtual async Task<LoadMoreItemsResult> LoadMore(uint count) {
            var added = await FetchRecent();
            return new LoadMoreItemsResult() { Count = (uint)added };

        }
    }
}
