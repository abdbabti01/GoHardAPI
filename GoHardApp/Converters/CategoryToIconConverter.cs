using System.Globalization;

namespace GoHardApp.Converters
{
    public class CategoryToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "🏋️";

            var category = value.ToString()?.ToLower();

            return category switch
            {
                "strength" => "💪",
                "cardio" => "❤️",
                "flexibility" => "🧘",
                "core" => "⚡",
                _ => "🏋️"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
