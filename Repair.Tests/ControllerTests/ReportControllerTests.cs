using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repair.Abstractions.Api;
using Repair.Abstractions.Entity.Model;
using Repair.Abstractions.Persistence;
using Repair.Abstractions.Services;
using Repair.Api.Controllers;
using Repair.Api.Exceptions;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;
using Repair.Tests.Shared.Factory;

namespace Repair.Tests.ControllerTests;

public class ReportControllerTests
{
    public class Fixture
    {
        protected readonly Mock<IEntityQueryService<Customer, SearchableCustomer>> customerQueryService = new(MockBehavior.Strict);
        protected readonly Mock<IEntityQueryService<Order, SearchableOrder>> orderQueryService = new(MockBehavior.Strict);
        protected readonly Mock<IReportService> reportService = new(MockBehavior.Strict);
        protected readonly ReportController controller;
        protected readonly IReportApi api;

        public Fixture()
        {
            controller = new ReportController(customerQueryService.Object, orderQueryService.Object,
                reportService.Object, NullLogger<ReportController>.Instance);
            api = controller;
        }

        protected void SetupOrderLookup(int id, Order? order)
        {
            orderQueryService.Setup(x => x.GetEntity(It.Is<SearchableOrder>(s => s.Id == id))).ReturnsAsync(order);
        }

        protected void SetupCustomerLookup(int id, Customer? customer)
        {
            customerQueryService.Setup(x => x.GetEntity(It.Is<SearchableCustomer>(s => s.Id == id))).ReturnsAsync(customer);
        }

        protected void SetupCustomerOrders(int id, Order[] orders)
        {
            orderQueryService.Setup(x => x.GetEntities(It.Is<SearchableOrder>(s => s.CustomerId == id))).ReturnsAsync(orders);
        }

        protected void AssertFile(IActionResult result, GeneratedReport expected)
        {
            var file = result.Should().BeOfType<FileContentResult>().Subject;
            file.ContentType.Should().Be("application/pdf");
            file.FileDownloadName.Should().Be(expected.FileName);
            file.FileContents.Should().Equal(expected.Content);
        }
    }

    public class GenerateOrderReport : Fixture
    {
        [Test]
        public async Task WithExistingOrder_ReturnsGeneratedReport()
        {
            // Arrange
            var order = EntityFactory.CreateDefaultOrder();
            var expected = new GeneratedReport("order.pdf", [1, 2, 3]);
            SetupOrderLookup(order.Id, order);
            reportService.Setup(x => x.GenerateReport(It.Is<IOrder>(o => ReferenceEquals(o, order)))).Returns(expected);

            // Act
            var result = await api.GenerateOrderReport(order.Id);

            // Assert
            result.Should().BeSameAs(expected);
            orderQueryService.VerifyAll();
            reportService.Verify(x => x.GenerateReport(It.Is<IOrder>(o => ReferenceEquals(o, order))), Times.Once());
            customerQueryService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task WithMissingOrder_ThrowsEntityNotFoundException()
        {
            // Arrange
            SetupOrderLookup(42, null);

            // Act
            Func<Task> act = () => api.GenerateOrderReport(42);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>();
            orderQueryService.VerifyAll();
            reportService.VerifyNoOtherCalls();
            customerQueryService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task WithExistingOrder_ReturnsPdfFile()
        {
            // Arrange
            var order = EntityFactory.CreateDefaultOrder();
            var expected = new GeneratedReport("order.pdf", [4, 5, 6]);
            SetupOrderLookup(order.Id, order);
            reportService.Setup(x => x.GenerateReport(It.Is<IOrder>(o => ReferenceEquals(o, order)))).Returns(expected);

            // Act
            var result = await controller.GenerateOrderReport(order.Id);

            // Assert
            AssertFile(result, expected);
            orderQueryService.VerifyAll();
            reportService.VerifyAll();
        }
    }

    public class GenerateCustomerReport : Fixture
    {
        [Test]
        public async Task WithExistingCustomer_LoadsOrdersAndReturnsGeneratedReport()
        {
            // Arrange
            var customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com").WithId(12);
            Order[] orders =
            [
                EntityFactory.CreateOrder("iPhone", "Screen replacement", customer).WithId(42),
                EntityFactory.CreateOrder("Pixel", "Battery replacement", customer).WithId(43),
            ];
            foreach (var order in orders)
                order.CustomerId = customer.Id;
            customer.Orders = new List<IOrder>();
            var expected = new GeneratedReport("customer.pdf", [7, 8, 9]);
            SetupCustomerLookup(customer.Id, customer);
            SetupCustomerOrders(customer.Id, orders);
            reportService.Setup(x => x.GenerateReport(It.Is<ICustomer>(c => ReferenceEquals(c, customer)
                && c.Orders.Count == 2 && ReferenceEquals(c.Orders.First(), orders[0])
                && ReferenceEquals(c.Orders.Last(), orders[1])))).Returns(expected);

            // Act
            var result = await api.GenerateCustomerReport(customer.Id);

            // Assert
            result.Should().BeSameAs(expected);
            customer.Orders.Should().Equal(orders);
            customerQueryService.VerifyAll();
            orderQueryService.VerifyAll();
            reportService.Verify(x => x.GenerateReport(It.IsAny<ICustomer>()), Times.Once());
        }

        [Test]
        public async Task WithNoOrders_ClearsExistingOrdersAndReturnsGeneratedReport()
        {
            // Arrange
            var customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com").WithId(12);
            customer.Orders = new List<IOrder> { EntityFactory.CreateOrder("Old phone", "Repair", customer), };
            var expected = new GeneratedReport("customer.pdf", [1, 3, 5]);
            SetupCustomerLookup(customer.Id, customer);
            SetupCustomerOrders(customer.Id, []);
            reportService.Setup(x => x.GenerateReport(It.Is<ICustomer>(c => ReferenceEquals(c, customer)
                && c.Orders.Count == 0))).Returns(expected);

            // Act
            var result = await api.GenerateCustomerReport(customer.Id);

            // Assert
            result.Should().BeSameAs(expected);
            customer.Orders.Should().BeEmpty();
            customerQueryService.VerifyAll();
            orderQueryService.VerifyAll();
            reportService.Verify(x => x.GenerateReport(It.IsAny<ICustomer>()), Times.Once());
        }

        [Test]
        public async Task WithMissingCustomer_ThrowsEntityNotFoundException()
        {
            // Arrange
            SetupCustomerLookup(12, null);

            // Act
            Func<Task> act = () => api.GenerateCustomerReport(12);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>();
            customerQueryService.VerifyAll();
            orderQueryService.VerifyNoOtherCalls();
            reportService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task WithExistingCustomer_ReturnsPdfFile()
        {
            // Arrange
            var customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com").WithId(12);
            var expected = new GeneratedReport("customer.pdf", [2, 4, 6]);
            SetupCustomerLookup(customer.Id, customer);
            SetupCustomerOrders(customer.Id, []);
            reportService.Setup(x => x.GenerateReport(It.Is<ICustomer>(c => ReferenceEquals(c, customer)))).Returns(expected);

            // Act
            var result = await controller.GenerateCustomerReport(customer.Id);

            // Assert
            AssertFile(result, expected);
            customerQueryService.VerifyAll();
            orderQueryService.VerifyAll();
            reportService.VerifyAll();
        }
    }
}
