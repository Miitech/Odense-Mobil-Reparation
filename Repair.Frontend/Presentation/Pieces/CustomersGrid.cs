using Repair.Frontend.Settings;
using Repair.Models.Api;
using Microsoft.UI.Dispatching;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Factory;
using Repair.Models.Entity.Searchable;

namespace Repair.Frontend.Presentation.Pieces;

internal sealed partial class CustomersGrid : Border, INavigationRefreshable
{
    internal CustomersGridViewModel ViewModel => (CustomersGridViewModel) DataContext;

    public CustomersGrid(CustomersGridArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        this.ConfigurePieceBorder();

        DataContext = new CustomersGridViewModel(arguments);

        Logic = new CustomersGridLogic(ViewModel);
        var ui = new CustomersGridUi(Logic, ViewModel);

        Child = ui.CreateContentGrid();
        Unloaded += (_, _) => Logic.CancelPendingRefresh();

        _ = Logic.RefreshCustomers();
    }

    private CustomersGridLogic Logic { get; set; }

    internal record CustomersGridArguments(
        ICustomerApi CustomerApi,
        IUiDispatcher UiDispatcher,
        ILoggerFactory LoggerFactory,
        int SelectedCustomerId = 0,
        int DebounceDelayMilliseconds = SearchSettings.DEFAULT_DEBOUNCE_DELAY_MILLISECONDS)
    {
    }

    /// <inheritdoc />
    public void RefreshAfterNavigation()
    {
        var logger = ViewModel.Arguments.LoggerFactory.CreateLogger<CustomersGrid>();
        logger.LogInformation($"Refreshing Customers after Navigation");

        _ = Logic.RefreshCustomers();
    }
}
