using Repair.Frontend.Presentation.Factory;

namespace Repair.Frontend.Presentation.Pieces;

internal sealed partial class DateTimePicker : Border
{
    internal DateTimePickerViewModel ViewModel => (DateTimePickerViewModel) DataContext;

    public DateTimePicker(DateTimePickerArguments arguments)
    {
        DataContext = new DateTimePickerViewModel(arguments);
        this.ConfigurePieceBorder();

        var logic = new DateTimePickerLogic(ViewModel);
        var ui = new DateTimePickerUi(logic, ViewModel);

        Child = ui.CreateContentGrid();
    }

    internal void SetReadOnly(bool isReadOnly)
    {
        ViewModel.IsReadOnly = isReadOnly;

        ViewModel.DateButton.IsHitTestVisible = !isReadOnly;
        ViewModel.DateButton.IsTabStop = !isReadOnly;

        ViewModel.TimeButton.IsHitTestVisible = !isReadOnly;
        ViewModel.TimeButton.IsTabStop = !isReadOnly;
    }

    internal record DateTimePickerArguments(
        string Header,
        ILoggerFactory LoggerFactory,
        DateTime? InitialValue = null,
        int MinuteIncrement = 5,
        bool AllowClear = false);
}
