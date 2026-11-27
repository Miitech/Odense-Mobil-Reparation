namespace Repair.Frontend.Presentation.Converters;

internal sealed class ReadOnlyToEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
