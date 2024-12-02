
namespace XamlViewer.Converters;
public class PercentageConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var v = decimal.Parse(value?.ToString() ?? "0");

        var v1 = System.Convert.ChangeType(v * 100, targetType);
        return v1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var v = decimal.Parse(value?.ToString() ?? "0");

        var v1 = System.Convert.ChangeType(v / 100, targetType);
        return v1;
    }
}
