using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Hangon.Converters {
    public class CreatedAt : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null) return "";

            var createdAt = (string)value;
            if (string.IsNullOrEmpty(createdAt)) return "";

            return DateTime
                    .ParseExact(createdAt, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                    .ToLocalTime()
                    .ToString("dd MMMM yyyy");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
