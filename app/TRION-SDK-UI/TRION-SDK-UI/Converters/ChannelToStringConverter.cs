using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using TRION_SDK_UI.Models; // Adjust namespace if needed

namespace TRION_SDK_UI.Converters
{
    public class ChannelToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var channel = value as Channel;
            if (channel == null)
                return string.Empty;
            return $"Board {channel.BoardID} - {channel.Name}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}