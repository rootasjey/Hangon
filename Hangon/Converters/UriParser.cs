using System;
using Windows.UI.Xaml.Data;

namespace Hangon.Converters {
    public class UriParser: IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var path = "ms-appx:///Assets/Icons/user.png";

            if (value != null && !string.IsNullOrEmpty((string)value)) {
                path = (string)value;
            }

            return new Uri(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value == null) return "";
            return ((Uri)value).AbsoluteUri;
        }
    }
}
