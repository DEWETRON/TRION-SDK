using System.Globalization;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI.Converters
{
    public class ChannelToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var channel = value as Channel;
            if (channel == null)
                return string.Empty;
            return $"{channel.BoardName} - {channel.Name}";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}