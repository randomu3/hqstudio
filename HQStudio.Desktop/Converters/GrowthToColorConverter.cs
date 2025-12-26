using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HQStudio.Converters
{
    /// <summary>
    /// Конвертер для отображения цвета в зависимости от роста (положительный/отрицательный)
    /// </summary>
    public class GrowthToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal growth)
            {
                if (growth > 0)
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                if (growth < 0)
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
            }
            return new SolidColorBrush(Color.FromRgb(150, 150, 150)); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер для вычисления процента (completed / total * 100)
    /// </summary>
    public class PercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int completed && values[1] is int total && total > 0)
            {
                return (completed * 100.0 / total).ToString("0");
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
