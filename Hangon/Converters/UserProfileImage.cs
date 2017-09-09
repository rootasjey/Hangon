using System;
using Unsplasharp.Models;
using Windows.UI.Xaml.Data;

namespace Hangon.Converters
{
    public class UserProfileImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var profileImage = (ProfileImage)value;
            var path = profileImage.Medium ??
                        profileImage.Large ??
                        profileImage.Small ??
                        "ms-appx:///Assets/Icons/user.png";

            return new Uri(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
