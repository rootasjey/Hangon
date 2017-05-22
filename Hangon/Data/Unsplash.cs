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
