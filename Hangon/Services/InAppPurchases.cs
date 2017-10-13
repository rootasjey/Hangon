using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Hangon.Services {
    public class InAppPurchases {
        private static StoreContext _context = null;

        public static async Task<StorePurchaseResult> PurchaseAddon(string storeId) {
            if (_context == null) {
                _context = StoreContext.GetDefault();
            }

            return await _context.RequestPurchaseAsync(storeId);
        }

        public static async void ConsumeAddon(string storeId) {
            if (_context == null) {
                _context = StoreContext.GetDefault();
            }

            uint quantity = 1;
            Guid trackingId = Guid.NewGuid();

            StoreConsumableResult result = await _context.ReportConsumableFulfillmentAsync(
                storeId, quantity, trackingId);

        }

        public static async Task<StoreProductQueryResult> GetAllAddons() {
            if (_context == null) {
                _context = StoreContext.GetDefault();
            }

            string[] productKinds = { "Durable", "Consumable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);

            return await _context.GetAssociatedStoreProductsAsync(filterList);
        }

        public async Task<StoreProductQueryResult> GetUserAddons() {
            if (_context == null) {
                _context = StoreContext.GetDefault();
            }

            string[] productKinds = { "Durable", "Consumable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);

            return await _context.GetUserCollectionAsync(filterList);
        }
    }
}
