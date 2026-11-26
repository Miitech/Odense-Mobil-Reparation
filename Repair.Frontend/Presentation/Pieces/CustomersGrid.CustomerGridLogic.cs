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

internal sealed partial class CustomersGrid
{
    internal sealed partial class CustomersGridLogic : BaseLogic<CustomersGridViewModel>
    {
        private readonly ICustomerApi customerApi;
        private readonly Debouncer debouncer;
        private readonly IUiDispatcher uiDispatcher;
        private readonly ILogger<CustomersGridLogic> logger;

        public CustomersGridLogic(CustomersGridViewModel viewModel) : base(viewModel)
        {
            customerApi = ViewModel.Arguments.CustomerApi;
            uiDispatcher = ViewModel.Arguments.UiDispatcher;
            logger = ViewModel.Arguments.LoggerFactory.CreateLogger<CustomersGridLogic>();

            debouncer = new Debouncer(ViewModel.Arguments.DebounceDelayMilliseconds);
            ViewModel.SearchChanged += SearchChanged;
            ViewModel.SearchTextChanged += SearchTextChanged;
        }

        private async void SearchChanged(object? sender, EventArgs e)
        {
            await RefreshCustomers();
        }

        private async void SearchTextChanged(object? sender, EventArgs e)
        {
            await debouncer.Run(SearchCustomers, delayed: true);
        }

        public void CancelPendingRefresh() => debouncer.Cancel();

        public Task RefreshCustomers() => debouncer.Run(SearchCustomers, delayed: false);

        private async Task SearchCustomers(CancellationToken cancellationToken)
        {
            try
            {
                RememberSelectedCustomer();

                ComplexSearchableCustomer searchable = CreateSearchableCustomer();

                List<GetCustomerDto> customers = (await customerApi.SearchComplex(searchable)).ToList();
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                ApplySearchResults(customers, cancellationToken);
            }
            catch (Exception exe)
            {
                logger.LogError(exe, "Exception caught during refresh of Customers");
            }
        }

        private void ApplySearchResults(List<GetCustomerDto> customers, CancellationToken cancellationToken)
        {
            customers = ViewModel.DataGrid.ApplyCurrentSort(customers).ToList();

            logger.LogInformation("Customer search returned {CustomerCount} customers. Fuzzy search: {UseFuzzySearch}",
                customers.Count, ViewModel.UseFuzzySearch);

            uiDispatcher.TryEnqueue(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                UpdateCustomersCollection(customers);
            });
        }

        private void UpdateCustomersCollection(List<GetCustomerDto> customers)
        {
            logger.LogDebug("Updating Customers collection. Existing count: {ExistingCount}",
                ViewModel.Customers.Count);

            ViewModel.Customers.ReplaceItems(customers);
            ViewModel.DataGrid.Refresh();

            RestoreSelectedCustomer();

            logger.LogDebug(
                "Customers collection updated. New count: {NewCount}, SelectedCustomerId: {SelectedCustomerId}, SelectedCustomer: '{SelectedCustomerName}'",
                ViewModel.Customers.Count, ViewModel.SelectedCustomerId, ViewModel.SelectedCustomer?.Name);
        }

        private void RememberSelectedCustomer()
        {
            if (ViewModel.SelectedCustomer is null)
            {
                return;
            }

            ViewModel.SelectedCustomerId = ViewModel.SelectedCustomer.Id;
        }

        private void RestoreSelectedCustomer()
        {
            ViewModel.SelectedCustomer = ViewModel.Customers.FirstOrDefault(x => x.Id == ViewModel.SelectedCustomerId);
        }

        private ComplexSearchableCustomer CreateSearchableCustomer()
        {
            var complex = new ComplexSearchableCustomer();

            if (ViewModel.UseFuzzySearch)
            {
                complex.Name = ViewModel.CustomerEditor.ViewModel.Name;
                complex.Phone = ViewModel.CustomerEditor.ViewModel.Phone;
                complex.Email = ViewModel.CustomerEditor.ViewModel.Email;
            }
            else
            {
                complex.Searchable.Name = ViewModel.CustomerEditor.ViewModel.Name;
                complex.Searchable.Phone = ViewModel.CustomerEditor.ViewModel.Phone;
                complex.Searchable.Email = ViewModel.CustomerEditor.ViewModel.Email;
            }

            return complex;
        }
    }
}
