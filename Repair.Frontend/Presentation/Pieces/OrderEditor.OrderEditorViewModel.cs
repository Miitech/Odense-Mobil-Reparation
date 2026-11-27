namespace Repair.Frontend.Presentation.Pieces;

internal sealed partial class OrderEditor
{
    internal sealed partial class OrderEditorViewModel(OrderEditorArguments arguments) : ObservableObject
    {
        internal OrderEditorArguments Arguments { get; } = arguments;

        public event EventHandler? IsReadOnlyChanged;
        public event EventHandler? HandInWhatChanged;
        public event EventHandler? RepairWhatChanged;
        public event EventHandler? IsOrderCompleteChanged;
        public event EventHandler? BorrowedDeviceChanged;

        internal DateTimePicker HandInWhenPicker { get; set; } = null!;
        internal DateTimePicker ReturnedWhenPicker { get; set; } = null!;
        internal CheckBox IsOrderCompleteCheckBox { get; set; } = null!;
        internal TextBox BorrowedDeviceTextBox { get; set; } = null!;
        internal TextBox HandInWhatTextBox { get; set; } = null!;
        internal TextBox RepairWhatTextBox { get; set; } = null!;
        internal CustomersGrid CustomersGrid { get; set; } = null!;
        internal TextBlock SelectedCustomerTextBlock { get; set; } = null!;

        [ObservableProperty] private string selectedCustomerText = "Current Selected Customer: Missing Selection";
        [ObservableProperty] private string handInWhat = arguments.Order?.HandInWhat ?? string.Empty;
        [ObservableProperty] private string repairWhat = arguments.Order?.RepairWhat ?? string.Empty;
        [ObservableProperty] private string borrowedDevice = arguments.Order?.BorrowedDevice ?? string.Empty;
        [ObservableProperty] private bool isOrderComplete = arguments.Order?.IsOrderComplete ?? false;
        [ObservableProperty] private bool isReadOnly = true;

        public DateTime HandInWhen => HandInWhenPicker.ViewModel.SelectedDateTime ?? DateTime.Now;
        public DateTime? ReturnedWhen => ReturnedWhenPicker.ViewModel.SelectedDateTime;
        public int CustomerId => CustomersGrid.ViewModel.SelectedCustomerId;

        partial void OnIsReadOnlyChanged(bool value)
        {
            IsReadOnlyChanged?.Invoke(this, EventArgs.Empty);
        }

        partial void OnHandInWhatChanged(string value)
        {
            HandInWhatChanged?.Invoke(this, EventArgs.Empty);
        }

        partial void OnRepairWhatChanged(string value)
        {
            RepairWhatChanged?.Invoke(this, EventArgs.Empty);
        }

        partial void OnIsOrderCompleteChanged(bool value)
        {
            IsOrderCompleteChanged?.Invoke(this, EventArgs.Empty);
        }

        partial void OnBorrowedDeviceChanged(string value)
        {
            BorrowedDeviceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
