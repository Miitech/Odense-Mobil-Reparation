using Repair.Abstractions.Api;
using Repair.Models.Api;
using Microsoft.UI.Dispatching;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Factory;
using Repair.Models.Entity.Searchable;

namespace Repair.Frontend.Presentation.Pages;

/// <summary>
/// Shows order details and information about the related customer.
/// </summary>
internal sealed partial class OrderDetailsPage : Border
{
    public OrderDetailsPage(OrderDetailsPageArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        this.ConfigurePageBorder();

        DataContext = new OrderDetailsPageViewModel(arguments);

        var viewModel = (OrderDetailsPageViewModel) DataContext;
        var logic = new OrderDetailsPageLogic(viewModel);
        var ui = new OrderDetailsPageUi(logic, viewModel);

        Child = ui.CreateContentGrid();

        _ = logic.RefreshOrder();
    }

    internal sealed record OrderDetailsPageArguments(
        int OrderId,
        IOrderApi OrderApi,
        IUiDispatcher UiDispatcher,
        ILoggerFactory LoggerFactory,
        INavigationService NavigationService,
        IReportApi ReportApi);
}
