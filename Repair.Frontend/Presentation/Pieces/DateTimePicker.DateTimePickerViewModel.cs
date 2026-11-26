namespace Repair.Frontend.Presentation.Pieces;

internal sealed partial class DateTimePicker
{
    internal sealed partial class DateTimePickerViewModel : ObservableObject
    {
        private bool isSettingDateTime;

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanClear))]
        private bool isReadOnly;

        public bool CanClear => Arguments.AllowClear && !IsReadOnly &&
                                (SelectedDate.HasValue || SelectedTime.HasValue);

        public DateTimePickerArguments Arguments { get; }
        public event EventHandler? SelectedDateTimeChanged;

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(SelectedDateText))] [NotifyPropertyChangedFor(nameof(CanClear))]
        private DateTimeOffset? selectedDate;

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(SelectedTimeText))] [NotifyPropertyChangedFor(nameof(CanClear))]
        private TimeSpan? selectedTime;

        private readonly ILogger<DateTimePickerViewModel> logger;

        public string SelectedDateText => SelectedDate.HasValue
            ? SelectedDate.Value.ToString("dd.MM.yyyy")
            : "No Date is Set";

        public string SelectedTimeText => SelectedTime.HasValue
            ? SelectedTime.Value.ToString(@"hh\:mm")
            : "No Time is Set";

        /// <inheritdoc/>
        public DateTimePickerViewModel(DateTimePickerArguments arguments)
        {
            Arguments = arguments;
            selectedDate = arguments.InitialValue;
            selectedTime = arguments.InitialValue?.TimeOfDay;

            logger = Arguments.LoggerFactory.CreateLogger<DateTimePickerViewModel>();
        }

        internal void SetSelectedDateTime(DateTime? dateTime)
        {
            DateTime? previous = SelectedDateTime;
            isSettingDateTime = true;
            try
            {
                SelectedDate = dateTime;
                SelectedTime = dateTime?.TimeOfDay;
            }
            finally
            {
                isSettingDateTime = false;
            }

            if (previous != SelectedDateTime)
            {
                RaiseSelectedDateTimeChanged();
            }
        }

        internal DateTime? SelectedDateTime =>
            SelectedDate.HasValue && SelectedTime.HasValue ? SelectedDate.Value.Date.Add(SelectedTime.Value) : null;

        internal DatePicker DatePicker { get; set; } = null!;
        internal TimePicker TimePicker { get; set; } = null!;
        internal Button DateButton { get; set; } = null!;
        internal Button TimeButton { get; set; } = null!;

        partial void OnSelectedDateChanged(DateTimeOffset? value)
        {
            RaiseSelectedDateTimeChangedIfComplete();
        }

        partial void OnSelectedTimeChanged(TimeSpan? value)
        {
            RaiseSelectedDateTimeChangedIfComplete();
        }

        private void RaiseSelectedDateTimeChangedIfComplete()
        {
            if (isSettingDateTime || !SelectedDateTime.HasValue)
            {
                return;
            }

            RaiseSelectedDateTimeChanged();
        }

        private void RaiseSelectedDateTimeChanged()
        {
            logger.LogDebug("Date/time changed for '{ArgumentsHeader}'", Arguments.Header);
            SelectedDateTimeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
