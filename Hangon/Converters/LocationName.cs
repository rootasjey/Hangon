using System;
using Unsplasharp.Models;
using Windows.UI.Xaml.Data;

namespace Hangon.Converters {
    public class LocationName : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null) return "";
            var location = (Location)value;

            return location.Title ?? string.Format("{0}, {1}", location.City, location.Country);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
