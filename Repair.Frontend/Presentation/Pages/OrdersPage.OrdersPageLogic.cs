using Repair.Models.Dto.Get;
using CommunityToolkit.WinUI.UI.Controls;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Core;

namespace Repair.Frontend.Presentation.Pages;

internal sealed partial class OrdersPage
{
    private sealed class OrdersPageLogic : BaseLogic<OrdersPageViewModel>
    {
        private readonly INavigationService navigationService;

        public OrdersPageLogic(OrdersPageViewModel viewModel) : base(viewModel)
        {
            navigationService = ViewModel.Arguments.NavigationService;
        }

        internal void OrderClicked(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
            {
                return;
            }

            if (dataGrid.SelectedItem is not GetOrderDto order)
            {
                return;
            }

            OrderDetailsPage.OrderDetailsPageArguments arguments =
                GetArgumentsFactory().CreateOrderDetailsPageArguments(order.Id);

            var detailPage = new OrderDetailsPage(arguments);
            navigationService.NavigateTo(detailPage, "Order Details Page");

            dataGrid.SelectedItem = null;
        }

        internal void CreateOrderClicked(object sender, RoutedEventArgs e)
        {
            OrderCreationPage.OrderCreationPageArguments arguments =
                GetArgumentsFactory().CreateOrderCreationPageArguments();

            var creationPage = new OrderCreationPage(arguments);
            navigationService.NavigateTo(creationPage, "Order Creation Page");
        }
    }
}
