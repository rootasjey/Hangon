using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.UI.Notifications;

namespace Hangon.Services {
    public static class DataTransfer {

        static DataTransferManager _dataTransferManager;

        //private static Quote _sharingQuote;

        public static void Share() {
            //_sharingQuote = quote;
            DataTransferManager.ShowShareUI();
        }

        public static void RegisterForShare() {
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(ShareTextHandler);
        }

        public static void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e) {
            //string text = _sharingQuote.Content + " - " + _sharingQuote.Author;
            //if (!string.IsNullOrWhiteSpace(_sharingQuote.Reference)) {
            //    text += " (" + _sharingQuote.Reference + ")";
            //}

            DataRequest request = e.Request;
            request.Data.Properties.Title = "Citations 365";
            request.Data.Properties.Description = "Share a quote";
            //request.Data.SetText(text);
        }

        public static void Copy(string text) {
            DataPackage dataPackage = new DataPackage() {
                RequestedOperation = DataPackageOperation.Copy
            };

            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);

            ShowLocalToast("User link copied!");
        }

        public static void ShowLocalToast(string message) {
            ToastContent content = new ToastContent() {
                Visual = new ToastVisual() {
                    BindingGeneric = new ToastBindingGeneric() {
                        Children = {
                            new AdaptiveText() {
                                Text = "Download"
                            },
                            new AdaptiveText() {
                                Text = message
                            }
                        }
                    }
                }
            };

            XmlDocument xmlContent = content.GetXml();
            ToastNotification notification = new ToastNotification(xmlContent);
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }
    }
}