using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Unsplasharp;
using Unsplasharp.Models;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Hangon.Data {
    public class CollectionsList : ObservableCollection<Collection>, ISupportIncrementalLoading {
        public string Url { get; set; }
        public int Page { get; set; }
        public bool HasMoreItems { get; set; }
        private UnsplasharpClient _Client { get; set; }

        public async Task<int> Fetch() {
            HasMoreItems = true;

            Page++;
            int added = 0;
            string fetchURL = string.Format("{0}?page={1}", Url, Page);

            _Client = _Client ?? new UnsplasharpClient(Credentials.ApplicationId);

            try {
                var collections = await _Client.FetchCollectionsList(fetchURL);

                added = collections.Count;

                foreach (var collection in collections) {
                    Add(collection);
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
