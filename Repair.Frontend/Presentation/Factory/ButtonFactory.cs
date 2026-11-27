using Repair.Frontend.Presentation.Converters;

namespace Repair.Frontend.Presentation.Factory
{
    public static class ButtonFactory
    {
        public static Button CreateClearButton()
        {
            Button button = new()
            {
                Content = "×",
                FontSize = 20,
                Foreground = new SolidColorBrush(Colors.Black),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 0, 4, 0),
                MinWidth = 28,
                MinHeight = 28,
                VerticalAlignment = VerticalAlignment.Top,
            };

            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, "Clear text");
            ToolTipService.SetToolTip(button, "Clear text");

            return button;
        }

        public static HyperlinkButton BuildHyperlinkButton(string url)
        {
            Uri.TryCreate(url, UriKind.Absolute, out Uri? uri);

            return new HyperlinkButton
            {
                NavigateUri = uri,
                IsEnabled = uri is not null,
                VerticalAlignment = VerticalAlignment.Center,
                Content = new TextBlock
                {
                    Text = url,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                },
            };
        }

        public static RadioButton CreateNullableBooleanOptionButton(string text)
        {
            RadioButton button = new()
            {
                Content = text,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
            };

            return ControlMarkup.Template(button, () => CreateNullableBooleanOptionButtonTemplate(button));
        }

        private static Border CreateNullableBooleanOptionButtonTemplate(RadioButton button)
        {
            Border border = CreateNullableBooleanOptionButtonBorder(button);

            ContentPresenter content = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            content.SetBinding(ContentPresenter.ContentProperty, CreateTemplateBinding(button, nameof(ContentControl.Content)));

            border.Child = content;

            return border;
        }

        private static Border CreateNullableBooleanOptionButtonBorder(RadioButton button)
        {
            Border border = new()
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 0, 8, 0),
            };

            border.SetBinding(Border.BackgroundProperty, CreateCheckedBackgroundBinding(button));
            border.SetBinding(Border.BorderBrushProperty, CreateCheckedBorderBrushBinding(button));

            return border;
        }

        private static Binding CreateCheckedBackgroundBinding(RadioButton button)
        {
            Binding binding = CreateTemplateBinding(button, nameof(ToggleButton.IsChecked));
            binding.Converter = new CheckedToBackgroundBrushConverter();

            return binding;
        }

        private static Binding CreateCheckedBorderBrushBinding(RadioButton button)
        {
            Binding binding = CreateTemplateBinding(button, nameof(ToggleButton.IsChecked));
            binding.Converter = new CheckedToBorderBrushConverter();

            return binding;
        }

        private static Binding CreateTemplateBinding(RadioButton button, string propertyName)
        {
            return new Binding
            {
                Source = button,
                Path = new PropertyPath(propertyName),
                Mode = BindingMode.OneWay,
            };
        }
    }
}
