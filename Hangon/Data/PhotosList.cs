using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Unsplasharp;
using Unsplasharp.Models;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Hangon.Data {
    public class PhotosList : ObservableCollection<Photo>, ISupportIncrementalLoading {
        public string Url { get; set; }
        public int Page { get; set; }
        public bool HasMoreItems { get; set; }
        public string Query { get; set; }
        public int TotalPhotoCount { get; set; }
        private UnsplasharpClient _Client {get;set;}

        public async Task<int> Fetch() {
            HasMoreItems = true;

            Page++;
            int added = 0;
            string fetchURL = string.Format("{0}?page={1}", Url, Page);

            _Client = _Client ?? new UnsplasharpClient(Credentials.ApplicationId);

            addSearchParametersIfRequested();

            try {
                List<Photo> photos = null;
                
                if (string.IsNullOrEmpty(Query)) {
                    photos = await _Client.FetchPhotosList(fetchURL);
                }
                else {
                    photos = await _Client.FetchSearchPhotosList(fetchURL);
                }

                added = photos.Count;

                foreach (var photo in photos) {
                    Add(photo);
                }

                if (added == 0) HasMoreItems = false;
                return added;

            } catch /*(HttpRequestException hre)*/ {
                HasMoreItems = false;
                return 0;
            }

            void addSearchParametersIfRequested()
            {
                if (!string.IsNullOrEmpty(Query)) {
                    fetchURL += "&query=" + Query;
                }
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
