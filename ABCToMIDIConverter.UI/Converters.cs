using System;
using System.Globalization;
using System.Windows.Data;

namespace ABCToMIDIConverter.UI
{
    /// <summary>
    /// Utility converters for data binding
    /// </summary>
    public static class Converters
    {
        public static readonly BooleanNegationConverter BooleanNegationConverter = new();
    }

    /// <summary>
    /// Converts boolean to its negation
    /// </summary>
    public class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }
}
