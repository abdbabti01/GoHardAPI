using System.Globalization;
using Microsoft.Maui.Graphics;

namespace GoHardApp.Converters
{
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Colors.Gray;

            var category = value.ToString()?.ToLower();

            return category switch
            {
                "strength" => Color.FromArgb("#E53935"), // Red
                "cardio" => Color.FromArgb("#1E88E5"),   // Blue
                "flexibility" => Color.FromArgb("#43A047"), // Green
                "core" => Color.FromArgb("#FB8C00"),     // Orange
                _ => Color.FromArgb("#9E9E9E")           // Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
