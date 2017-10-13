﻿using Hangon.Services;
using System.Collections.Generic;
using Windows.Services.Store;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Hangon.Views {
    public sealed partial class AchievementsPage : Page {
        public AchievementsPage() {
            InitializeComponent();
            InitializePageAnimation();
            InitializeData();
        }

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;
            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion navigation

        #region animations

        private void InitializePageAnimation() {
            TransitionCollection collection = new TransitionCollection();
            NavigationThemeTransition theme = new NavigationThemeTransition();

            var info = new SlideNavigationTransitionInfo();

            theme.DefaultNavigationTransitionInfo = info;
            collection.Add(theme);
            Transitions = collection;
        }
        
        #endregion animations

        #region data
        private void ShowInAppPurchasesLoadingView() {
            DonationsLoadingView.Visibility = Visibility.Visible;
            ProgressLoadingInAppPurchases.Visibility = Visibility.Visible;
        }

        private void HideInAppPurchasesLoadingView() {
            DonationsLoadingView.Visibility = Visibility.Collapsed;
            ProgressLoadingInAppPurchases.Visibility = Visibility.Collapsed;
        }

        private async void InitializeData() {
            ShowInAppPurchasesLoadingView();

            var queryResult = await InAppPurchases.GetAllAddons();

            if (queryResult.ExtendedError != null) {
                return;
            }

            var addonsList = new List<StoreProduct>();

            foreach (KeyValuePair<string, StoreProduct> item in queryResult.Products) {
                StoreProduct product = item.Value;
                addonsList.Add(product);
            }

            HideInAppPurchasesLoadingView();

            UnlocksListView.ItemsSource = addonsList;
        }

        #endregion data

        #region events

        private void Addon_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var item = (Grid)sender;
            var product = (StoreProduct)item.DataContext;
            Purchase(product.StoreId);
        }

        #endregion events

        #region others

        private async void Purchase(string id) {
            var result = await InAppPurchases.PurchaseAddon(id);

            string extendedError = string.Empty;
            string descriptionError = string.Empty;

            if (result.ExtendedError != null) {
                extendedError = "ExtendedError: " + result.ExtendedError.Message;
            }

            switch (result.Status) {
                case StorePurchaseStatus.AlreadyPurchased:
                    descriptionError = App.ResourceLoader.GetString("PurchaseStatusAlreadyPurchased");
                    InAppPurchases.ConsumeAddon(id);
                    break;

                case StorePurchaseStatus.Succeeded:
                    descriptionError = App.ResourceLoader.GetString("PurchaseStatusSucceeded");
                    InAppPurchases.ConsumeAddon(id);
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

            DataTransfer.ShowLocalToast(descriptionError);
        }

        #endregion others
    }
}
