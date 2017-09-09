using System;
using Unsplasharp.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Hangon.Converters {
    class LocationVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null) return Visibility.Collapsed;

            var location = (Location)value;

            if (string.IsNullOrEmpty(location.City) && 
                string.IsNullOrEmpty(location.Country) && 
                string.IsNullOrEmpty(location.Title)) {

                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
