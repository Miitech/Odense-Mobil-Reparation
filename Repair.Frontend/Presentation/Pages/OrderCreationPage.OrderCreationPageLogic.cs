using Repair.Models.Dto.Add;
using Repair.Models.Dto.Get;
using Repair.Models.Api;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Core;
using Repair.Frontend.Presentation.Pieces;
using Repair.Frontend.Services;
using Repair.Models.Entity.Searchable;

namespace Repair.Frontend.Presentation.Pages;

internal sealed partial class OrderCreationPage
{
    private sealed class OrderCreationPageLogic : BaseLogic<OrderCreationPageViewModel>
    {
        private readonly IOrderApi orderApi;
        private readonly INavigationService navigationService;
        private readonly ILogger<OrderCreationPageLogic> logger;

        public OrderCreationPageLogic(OrderCreationPageViewModel viewModel) : base(viewModel)
        {
            orderApi = ViewModel.Arguments.OrderApi;
            navigationService = ViewModel.Arguments.NavigationService;
            logger = ViewModel.Arguments.LoggerFactory.CreateLogger<OrderCreationPageLogic>();
        }

        internal async Task SaveClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsUserInputValid())
                {
                    logger.LogInformation("Order creation was blocked by invalid input.");
                    return;
                }

                GetOrderDto newOrder = await orderApi.AddSingle(BuildNewOrder());
                logger.LogInformation("Created order {OrderId} for customer {CustomerId}.", newOrder.Id,
                    newOrder.CustomerId);

                OrderDetailsPage.OrderDetailsPageArguments arguments = GetArgumentsFactory()
                    .CreateOrderDetailsPageArguments(newOrder.Id);

                var details = new OrderDetailsPage(arguments);
                navigationService.NavigateTo(details, "Order Details Page");
            }
            catch (Exception exe)
            {
                logger.LogError(exe, $"Caught exception while trying to create a new Order.");
            }
        }

        private bool IsUserInputValid()
        {
            OrderEditor.OrderEditorViewModel orderEditorViewModel = ViewModel.OrderEditor.ViewModel;

            return !string.IsNullOrWhiteSpace(orderEditorViewModel.HandInWhat) &&
                   !string.IsNullOrWhiteSpace(orderEditorViewModel.RepairWhat) &&
                   orderEditorViewModel.HandInWhenPicker.ViewModel.SelectedDateTime.HasValue &&
                   GetSelectedCustomerId() > 0;
        }

        private AddOrderDto BuildNewOrder()
        {
            OrderEditor.OrderEditorViewModel orderEditorViewModel = ViewModel.OrderEditor.ViewModel;

            return new AddOrderDto
            {
                HandInWhen = orderEditorViewModel.HandInWhenPicker.ViewModel.SelectedDateTime!.Value,
                ReturnedWhen = orderEditorViewModel.ReturnedWhenPicker.ViewModel.SelectedDateTime,
                IsOrderComplete = orderEditorViewModel.IsOrderComplete,
                BorrowedPhone = orderEditorViewModel.BorrowedPhone,
                HandInWhat = orderEditorViewModel.HandInWhat,
                RepairWhat = orderEditorViewModel.RepairWhat,
                CustomerId = GetSelectedCustomerId(),
            };
        }

        private int GetSelectedCustomerId()
        {
            if (ViewModel.OrderEditor.ViewModel.CustomersGrid.ViewModel.DataGrid.SelectedItem is not GetCustomerDto customer)
            {
                return 0;
            }

            return customer.Id;
        }

        internal void CancelClicked(object sender, RoutedEventArgs e)
        {
            navigationService.NavigateBack();
        }
    }
}
