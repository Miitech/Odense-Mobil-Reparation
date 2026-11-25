using Repair.Abstractions.Api;
using Repair.Models.Api;
using Microsoft.UI.Dispatching;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Factory;
using Repair.Models.Entity.Searchable;

namespace Repair.Frontend.Presentation.Pages;

/// <summary>
/// Shows customer details and the customer's orders.
/// </summary>
internal sealed partial class CustomerDetailsPage : Border
{
    public CustomerDetailsPage(CustomerDetailsPageArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        this.ConfigurePageBorder();

        DataContext = new CustomerDetailsPageViewModel(arguments);

        var viewModel = (CustomerDetailsPageViewModel) DataContext;
        var logic = new CustomerDetailsPageLogic(viewModel);
        var ui = new CustomerDetailsPageUi(logic, viewModel);

        Child = ui.CreateContentGrid();

        _ = logic.RefreshCustomer();
    }

    internal sealed record CustomerDetailsPageArguments(
        int CustomerId,
        ICustomerApi CustomerApi,
        IUiDispatcher UiDispatcher,
        ILoggerFactory LoggerFactory,
        INavigationService NavigationService,
        IReportApi ReportApi);
}
