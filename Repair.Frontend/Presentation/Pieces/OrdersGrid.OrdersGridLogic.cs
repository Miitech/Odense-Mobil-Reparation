using Repair.Frontend.Services;
using Repair.Models.Dto.Get;
using Repair.Models.Api;
using Microsoft.UI.Dispatching;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Extensions;
using Repair.Frontend.Presentation.Core;
using Repair.Frontend.Presentation.Factory;
using Repair.Models.Entity.ComplexSearchable;
using Repair.Models.Entity.Searchable;
using Repair.Models.Extensions;

namespace Repair.Frontend.Presentation.Pieces;

internal sealed partial class OrdersGrid
{
    internal sealed partial class OrdersGridLogic : BaseLogic<OrdersGridViewModel>
    {
        private readonly IOrderApi orderApi;
        private readonly Debouncer debouncer;
        private readonly IUiDispatcher uiDispatcher;
        private readonly ILogger<OrdersGridLogic> logger;

        public OrdersGridLogic(OrdersGridViewModel viewModel) : base(viewModel)
        {
            orderApi = ViewModel.Arguments.OrderApi;
            uiDispatcher = ViewModel.Arguments.UiDispatcher;
            logger = ViewModel.Arguments.LoggerFactory.CreateLogger<OrdersGridLogic>();

            debouncer = new Debouncer(ViewModel.Arguments.DebounceDelayMilliseconds);
            ViewModel.SearchChanged += SearchChanged;
            ViewModel.SearchTextChanged += SearchTextChanged;
        }

        internal async void SearchChanged(object? sender, EventArgs e)
        {
            await RefreshOrders();
        }

        private async void SearchTextChanged(object? sender, EventArgs e)
        {
            await debouncer.Run(SearchOrders, delayed: true);
        }

        public void CancelPendingRefresh() => debouncer.Cancel();

        public Task RefreshOrders() => debouncer.Run(SearchOrders, delayed: false);

        private async Task SearchOrders(CancellationToken cancellationToken)
        {
            try
            {
                ComplexSearchableOrder searchable = CreateSearchableOrder();

                List<GetOrderDto> orders = (await orderApi.SearchComplex(searchable)).ToList();
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                ApplySearchResults(orders, cancellationToken);
            }
            catch (Exception exe)
            {
                logger.LogError(exe, "Exception caught during refresh of Orders");
            }
        }

        private void ApplySearchResults(List<GetOrderDto> orders, CancellationToken cancellationToken)
        {
            orders = ViewModel.DataGrid.ApplyCurrentSort(orders).ToList();

            logger.LogInformation(
                "Order search returned {OrderCount} orders. CustomerId filter: {CustomerId}, Fuzzy search: {UseFuzzySearch}",
                orders.Count, ViewModel.CustomerId, ViewModel.UseFuzzySearch);

            uiDispatcher.TryEnqueue(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                UpdateOrdersCollection(orders);
            });
        }

        private void UpdateOrdersCollection(List<GetOrderDto> orders)
        {
            logger.LogDebug("Updating Orders collection. Existing count: {ExistingCount}", ViewModel.Orders.Count);

            ViewModel.Orders.ReplaceItems(orders);
            ViewModel.DataGrid.Refresh();

            logger.LogDebug("Orders collection updated. New count: {NewCount}", ViewModel.Orders.Count);
        }

        private ComplexSearchableOrder CreateSearchableOrder()
        {
            var complex = new ComplexSearchableOrder
            {
                Searchable = new SearchableOrder
                {
                    CustomerId = ViewModel.CustomerId,
                },
                UseFuzzy = ViewModel.UseFuzzySearch,
                CustomerName = ViewModel.CustomerNameSearchText,
                IsOrderComplete = ViewModel.IsOrderComplete,
            };

            if (ViewModel.UseFuzzySearch)
            {
                complex.HandInWhat = ViewModel.HandInWhatSearchText;
                complex.RepairWhat = ViewModel.RepairWhatSearchText;
                complex.BorrowedPhone = ViewModel.BorrowedPhoneSearchText;
            }
            else
            {
                complex.Searchable.HandInWhat = ViewModel.HandInWhatSearchText;
                complex.Searchable.RepairWhat = ViewModel.RepairWhatSearchText;
                complex.Searchable.BorrowedPhone = ViewModel.BorrowedPhoneSearchText;
            }

            AddDateTimeFilters(complex);

            return complex;
        }

        private void AddDateTimeFilters(ComplexSearchableOrder complex)
        {
            if (ViewModel.UseHandInFromFilter)
            {
                complex.HandInFrom = ViewModel.HandInFromDateTimePicker.ViewModel.SelectedDateTime;
            }

            if (ViewModel.UseHandInToFilter)
            {
                complex.HandInTo = ViewModel.HandInToDateTimePicker.ViewModel.SelectedDateTime;
            }

            if (ViewModel.UseReturnedFromFilter)
            {
                complex.ReturnedFrom = ViewModel.ReturnedFromDateTimePicker.ViewModel.SelectedDateTime;
            }

            if (ViewModel.UseReturnedToFilter)
            {
                complex.ReturnedTo = ViewModel.ReturnedToDateTimePicker.ViewModel.SelectedDateTime;
            }
        }

        public void IsOrderCompleteSelectionChanged(object? sender, EventArgs e)
        {
            ViewModel.IsOrderComplete = ViewModel.IsOrderCompleteOptionBar.ViewModel.SelectedValue;
        }
    }
}
