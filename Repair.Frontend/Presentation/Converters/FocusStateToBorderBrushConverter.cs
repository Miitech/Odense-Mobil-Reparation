namespace Repair.Frontend.Presentation.Converters;

internal sealed class FocusStateToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is FocusState.Unfocused
            ? new SolidColorBrush(Colors.Black)
            : new SolidColorBrush(Colors.DodgerBlue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
