using Repair.Models.Dto.Get;
using Repair.Abstractions.Api;
using Repair.Models.Api;
using Microsoft.UI.Dispatching;
using Repair.Frontend.Abstraction;
using Repair.Frontend.Presentation.Pages;
using Repair.Frontend.Presentation.Pieces;
using Repair.Models.Entity.Searchable;

namespace Repair.Frontend.Services;

internal class ArgumentsFactory(
    ICustomerApi customerApi,
    IOrderApi orderApi,
    IUiDispatcher uiDispatcher,
    ILoggerFactory loggerFactory,
    INavigationService navigationService,
    IReportApi reportApi)
{
    internal CustomerDetailsPage.CustomerDetailsPageArguments CreateCustomerDetailsPageArguments(int customerId)
    {
        return new CustomerDetailsPage.CustomerDetailsPageArguments(customerId, customerApi, uiDispatcher,
            loggerFactory, navigationService, reportApi);
    }

    internal OrdersGrid.OrdersGridArguments CreateOrderGridArguments(int customerId = 0)
    {
        return new OrdersGrid.OrdersGridArguments(orderApi, uiDispatcher, loggerFactory, customerId);
    }

    internal CustomersPage.CustomersPageArguments CreateCustomersPageArguments()
    {
        return new CustomersPage.CustomersPageArguments(navigationService);
    }

    internal OrdersPage.OrdersPageArguments CreateOrdersPageArguments()
    {
        return new OrdersPage.OrdersPageArguments(navigationService);
    }

    internal CustomerCreationPage.CustomerCreationPageArguments CreateCustomerCreationPageArguments()
    {
        return new CustomerCreationPage.CustomerCreationPageArguments(customerApi, navigationService,
            loggerFactory);
    }

    internal CustomerEditor.CustomerEditorArguments CreateCustomerEditorArguments(bool isSearchMode = false)
    {
        return new CustomerEditor.CustomerEditorArguments(isSearchMode);
    }

    internal NullableBooleanOptionBar.NullableBooleanOptionBarArguments CreateNullableBooleanOptionBarArguments(
        string header)
    {
        return new NullableBooleanOptionBar.NullableBooleanOptionBarArguments(header);
    }

    internal OrderCreationPage.OrderCreationPageArguments CreateOrderCreationPageArguments(int selectedCustomerId = 0)
    {
        return new OrderCreationPage.OrderCreationPageArguments(orderApi, navigationService, loggerFactory,
            selectedCustomerId);
    }

    internal DateTimePicker.DateTimePickerArguments CreateDateTimePickerArguments(
        string header, DateTime? initialValue = null, int minuteIncrement = 5)
    {
        return new DateTimePicker.DateTimePickerArguments(header, loggerFactory, initialValue, minuteIncrement);
    }

    internal CustomersGrid.CustomersGridArguments CreateCustomersGridArguments(int selectedCustomerId = 0)
    {
        return new CustomersGrid.CustomersGridArguments(customerApi, uiDispatcher, loggerFactory,
            selectedCustomerId);
    }

    internal OrderEditor.OrderEditorArguments CreateOrderEditorArguments(
        GetOrderDto? order = null, int selectedCustomerId = 0)
    {
        return new OrderEditor.OrderEditorArguments(order, selectedCustomerId);
    }

    internal OrderDetailsPage.OrderDetailsPageArguments CreateOrderDetailsPageArguments(int orderId)
    {
        return new OrderDetailsPage.OrderDetailsPageArguments(orderId, orderApi, uiDispatcher, loggerFactory,
            navigationService, reportApi);
    }
}
    
