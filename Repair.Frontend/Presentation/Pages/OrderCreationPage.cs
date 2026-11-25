using Repair.Models.Api;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Factory;
using Repair.Models.Entity.Searchable;

namespace Repair.Frontend.Presentation.Pages;

/// <summary>
/// Creates a new order. Editing is always enabled.
/// </summary>
internal sealed partial class OrderCreationPage : Border
{
    public OrderCreationPage(OrderCreationPageArguments arguments)
    {
        DataContext = new OrderCreationPageViewModel(arguments);
        this.ConfigurePageBorder();

        var viewModel = (OrderCreationPageViewModel) DataContext;
        var logic = new OrderCreationPageLogic(viewModel);
        var ui = new OrderCreationPageUi(logic, viewModel);

        Child = ui.CreateContentGrid();
    }

    internal sealed record OrderCreationPageArguments(
        IOrderApi OrderApi,
        INavigationService NavigationService,
        ILoggerFactory LoggerFactory,
        int SelectedCustomerId = 0);
}
