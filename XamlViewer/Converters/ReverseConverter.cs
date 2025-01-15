

namespace XamlViewer.Converters;
public class ReverseConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool v) return !v;

        return false;

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool v) return !v;

        return false;
    }
}
