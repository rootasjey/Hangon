using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Services.Store;
using System.Linq;

namespace Hangon.Services {
    public class InAppPurchases {
        private static StoreContext _context = null;

        private static void InitializeContext() {
            if (_context == null) {
                _context = StoreContext.GetDefault();
            }
        }

        public static async Task<StorePurchaseResult> PurchaseAddon(string storeId) {
            InitializeContext();
            return await _context.RequestPurchaseAsync(storeId);
        }

        public static string GetMessagePurchaseResult(StorePurchaseResult result) {
            string extendedError = string.Empty;
            string descriptionError = string.Empty;

            if (result.ExtendedError != null) {
                extendedError = "ExtendedError: " + result.ExtendedError.Message;
            }

            switch (result.Status) {
                case StorePurchaseStatus.AlreadyPurchased:
                    descriptionError = App.ResourceLoader.GetString("PurchaseStatusAlreadyPurchased");
                    break;

                case StorePurchaseStatus.Succeeded:
                    descriptionError = App.ResourceLoader.GetString("PurchaseStatusSucceeded");
                    break;

                case StorePurchaseStatus.NotPurchased:
                    descriptionError = string.Format("{0}. {1}",
                        App.ResourceLoader.GetString("PurchaseStatusNotPurchased"),
                        extendedError);
                    break;

                case StorePurchaseStatus.NetworkError:
                    descriptionError = string.Format("{0}. {1}",
                        App.ResourceLoader.GetString("PurchaseStatusNetworkError"),
                        extendedError);
                    break;

                case StorePurchaseStatus.ServerError:
                    descriptionError = string.Format("{0}. {1}",
                        App.ResourceLoader.GetString("PurchaseStatusServerError"),
                        extendedError);
                    break;

                default:
                    descriptionError = string.Format("{0}. {1}",
                        App.ResourceLoader.GetString("PurchaseStatusUknownError"),
                        extendedError);
                    break;
            }

            return descriptionError;
        }

        public static async void ConsumeAddon(string storeId) {
            InitializeContext();

            uint quantity = 1;
            Guid trackingId = Guid.NewGuid();

            StoreConsumableResult result = await _context.ReportConsumableFulfillmentAsync(
                storeId, quantity, trackingId);
        }

        public static async Task<StoreConsumableResult> GetRemainingBalance(string storeId) {
            InitializeContext();

            StoreConsumableResult result = await _context.GetConsumableBalanceRemainingAsync(storeId);
            return result;
        }

        public static async Task<StoreProductQueryResult> GetAllAddons() {
            InitializeContext();

            string[] productKinds = { "Durable", "Consumable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);

            return await _context.GetAssociatedStoreProductsAsync(filterList);
        }

        public static async Task<StoreProductQueryResult> GetUserAddons() {
            InitializeContext();

            string[] productKinds = { "Durable", "Consumable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);

            return await _context.GetUserCollectionAsync(filterList);
        }

        public static async Task<bool> DoesUserHaveAddon(string id) {
            InitializeContext();
            
            var foundProducts = await GetUserAddons();

            var matches = foundProducts
                        .Products
                        .Where(x => x.Value.StoreId == id);

            return matches.Count() > 0;
        }
    }
}
