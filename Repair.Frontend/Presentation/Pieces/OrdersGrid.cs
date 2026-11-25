using Repair.Models.Api;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Dispatching;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Factory;
using Repair.Models.Entity.Searchable;

namespace Repair.Frontend.Presentation.Pieces;

internal sealed partial class OrdersGrid : Border, INavigationRefreshable
{
    internal OrdersGridViewModel ViewModel => (OrdersGridViewModel) DataContext;

    public OrdersGrid(OrdersGridArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        this.ConfigurePieceBorder();

        DataContext = new OrdersGridViewModel(arguments);

        Logic = new OrdersGridLogic(ViewModel);
        var ui = new OrdersGridUi(Logic, ViewModel);

        Child = ui.CreateContentGrid();

        _ = Logic.RefreshOrders();
    }

    private OrdersGridLogic Logic { get; set; }

    internal record OrdersGridArguments(
        IOrderApi OrderApi,
        IUiDispatcher UiDispatcher,
        ILoggerFactory LoggerFactory,
        int CustomerId = 0)
    {
    }

    /// <inheritdoc />
    public void RefreshAfterNavigation()
    {
        var logger = ViewModel.Arguments.LoggerFactory.CreateLogger<OrdersGrid>();
        logger.LogInformation($"Refreshing Orders after Navigation");

        _ = Logic.RefreshOrders();
    }
}
