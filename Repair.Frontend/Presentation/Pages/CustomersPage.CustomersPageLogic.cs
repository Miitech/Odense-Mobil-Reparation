using Repair.Frontend.Presentation.Factory;
using Repair.Models.Dto.Get;
using System.ComponentModel;
using CommunityToolkit.WinUI.UI.Controls;
using Repair.Abstractions.Entity.Model;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Core;
using Repair.Frontend.Services;

namespace Repair.Frontend.Presentation.Pages;

internal sealed partial class CustomersPage
{
    private sealed class CustomersPageLogic : BaseLogic<CustomersPageViewModel>
    {
        private readonly INavigationService navigationService;

        public CustomersPageLogic(CustomersPageViewModel viewModel) : base(viewModel)
        {
            navigationService = ViewModel.Arguments.NavigationService;
        }

        public void CreateCustomerClicked(object sender, RoutedEventArgs e)
        {
            CustomerCreationPage.CustomerCreationPageArguments arguments =
                GetArgumentsFactory().CreateCustomerCreationPageArguments();

            var creationPage = new CustomerCreationPage(arguments);
            navigationService.NavigateTo(creationPage, "Customer Creation Page");
        }

        public void CustomerClicked(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid dataGrid || dataGrid.IsUpdatingSelection())
            {
                return;
            }

            if (dataGrid.SelectedItem is not GetCustomerDto customer)
            {
                return;
            }

            CustomerDetailsPage.CustomerDetailsPageArguments arguments =
                GetArgumentsFactory().CreateCustomerDetailsPageArguments(customer.Id);

            dataGrid.SelectedItem = null;

            var detailPage = new CustomerDetailsPage(arguments);
            navigationService.NavigateTo(detailPage, "Customer Details Page");

        }
    }
}
