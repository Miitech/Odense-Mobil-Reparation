using Repair.Frontend.Extensions;
using Repair.Frontend.Presentation.Core;
using Repair.Frontend.Presentation.Factory;

namespace Repair.Frontend.Presentation.Pieces;

internal sealed partial class DateTimePicker
{
    internal sealed class DateTimePickerUi(DateTimePickerLogic logic, DateTimePickerViewModel viewModel)
        : BaseUi<DateTimePickerLogic, DateTimePickerViewModel>(logic, viewModel)
    {
        protected override void ConfigureGrid(Grid grid)
        {
            grid.DefineRows(GridLength.Auto, GridLength.Auto);
        }

        protected override void AddControlsToGrid(Grid grid)
        {
            TextBlock header = CreateHeader().SetRow(0);
            Grid pickerGrid = CreatePickerGrid().SetRow(1);

            grid.Children.Add(header);
            grid.Children.Add(pickerGrid);
        }

        private TextBlock CreateHeader()
        {
            TextBlock header = TextBlockFactory.CreateBlackText(ViewModel.Arguments.Header);

            header.Margin = new Thickness(6, 4, 8, 0);

            return header;
        }

        private Grid CreatePickerGrid()
        {
            Grid grid = GridFactory.CreateDefaultGrid();

            grid.Margin = new Thickness(4);
            grid.ColumnSpacing = 4;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.DefineColumns(GridUnitType.Star, [1, 1,]);

            if (ViewModel.Arguments.AllowClear)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.Children.Add(CreateClearButton().SetColumn(2));
            }

            Grid dateGrid = CreateDatePickerHost().SetColumn(0);
            Grid timeGrid = CreateTimePickerHost().SetColumn(1);

            grid.Children.Add(dateGrid);
            grid.Children.Add(timeGrid);

            return grid;
        }

        private Button CreateClearButton()
        {
            Button button = new()
            {
                Content = "×",
                VerticalAlignment = VerticalAlignment.Stretch,
                MinWidth = 36,
            };

            button.SetBinding(Control.IsEnabledProperty, new Binding
            {
                Path = new PropertyPath(nameof(DateTimePickerViewModel.CanClear)),
                Mode = BindingMode.OneWay,
            });
            button.Click += Logic.ClearButtonClicked;
            return button;
        }

        private Grid CreateDatePickerHost()
        {
            Grid grid = GridFactory.CreateDefaultGrid();

            DatePicker picker = CreateDatePicker();
            Button button = CreateDateButton();

            grid.Children.Add(picker);
            grid.Children.Add(button);

            return grid;
        }

        private Grid CreateTimePickerHost()
        {
            Grid grid = GridFactory.CreateDefaultGrid();

            TimePicker picker = CreateTimePicker();
            Button button = CreateTimeButton();

            grid.Children.Add(picker);
            grid.Children.Add(button);

            return grid;
        }

        private Button CreateDateButton()
        {
            Button button = new()
            {
                HorizontalContentAlignment = HorizontalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            ViewModel.DateButton = button;

            button.SetBinding(ContentControl.ContentProperty, new Binding
            {
                Path = new PropertyPath(nameof(DateTimePickerViewModel.SelectedDateText)),
                Mode = BindingMode.OneWay,
            });

            button.Click += Logic.DateButtonClicked;

            return button;
        }

        private DatePicker CreateDatePicker()
        {
            DatePicker picker = new()
            {
                Width = 1,
                Height = 1,
                Opacity = 0,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
            };

            ViewModel.DatePicker = picker;

            picker.SetBinding(DatePicker.SelectedDateProperty, new Binding
            {
                Path = new PropertyPath(nameof(DateTimePickerViewModel.SelectedDate)),
                Mode = BindingMode.TwoWay,
            });

            return picker;
        }

        private Button CreateTimeButton()
        {
            Button button = new()
            {
                HorizontalContentAlignment = HorizontalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            ViewModel.TimeButton = button;

            button.SetBinding(ContentControl.ContentProperty, new Binding
            {
                Path = new PropertyPath(nameof(DateTimePickerViewModel.SelectedTimeText)),
                Mode = BindingMode.OneWay,
            });

            button.Click += Logic.TimeButtonClicked;

            return button;
        }

        private TimePicker CreateTimePicker()
        {
            TimePicker picker = new()
            {
                Width = 1,
                Height = 1,
                Opacity = 0,
                IsHitTestVisible = false,
                ClockIdentifier = Windows.Globalization.ClockIdentifiers.TwentyFourHour,
                MinuteIncrement = ViewModel.Arguments.MinuteIncrement,
                Visibility = Visibility.Collapsed,
            };

            ViewModel.TimePicker = picker;

            picker.SetBinding(TimePicker.SelectedTimeProperty, new Binding
            {
                Path = new PropertyPath(nameof(DateTimePickerViewModel.SelectedTime)),
                Mode = BindingMode.TwoWay,
            });

            return picker;
        }
    }
}
