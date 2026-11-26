using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repair.Abstractions.Entity.Data;
using Repair.Abstractions.Persistence;
using Repair.Api.Controllers;
using Repair.Api.Exceptions;
using Repair.Models.Api;
using Repair.Models.Dto.Add;
using Repair.Models.Dto.Get;
using Repair.Models.Dto.Update;
using Repair.Models.Entity.ComplexSearchable;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;
using Repair.Tests.Shared.Factory;

namespace Repair.Tests.ControllerTests;

public class OrderControllerTests
{
    public class Fixture
    {
        protected readonly Mock<IEntityQueryService<Order, SearchableOrder>> queryService = new(MockBehavior.Strict);
        protected readonly Mock<IEntityQueryService<Customer, SearchableCustomer>> customerQueryService = new(MockBehavior.Strict);
        protected readonly Mock<IUrlHelper> urlHelper = new(MockBehavior.Strict);
        protected readonly DefaultHttpContext httpContext = new();
        protected readonly IOrderApi api;

        public Fixture()
        {
            api = new OrderController(queryService.Object, customerQueryService.Object, NullLogger<OrderController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext, },
                Url = urlHelper.Object,
            };
        }

        protected Customer CreateCustomer(int id = 12)
        {
            var customer = EntityFactory.CreateCustomer($"Customer {id}", $"123456{id}", $"customer{id}@example.com").WithId(id);
            customer.Version = [4, 5, 6];
            customer.CreatedDateTime = new DateTime(2026, 6, 17, 10, 30, 0, DateTimeKind.Utc);
            customer.UpdatedDateTime = customer.CreatedDateTime.AddHours(1);
            return customer;
        }

        protected Order CreateOrder(int id = 12)
        {
            var customer = CreateCustomer(id + 100);
            var order = EntityFactory.CreateOrder($"Phone {id}", "Screen replacement", customer).WithId(id);
            order.CustomerId = customer.Id;
            order.Version = [1, 2, 3];
            order.CreatedDateTime = new DateTime(2026, 6, 18, 10, 30, 0, DateTimeKind.Utc);
            order.UpdatedDateTime = order.CreatedDateTime.AddHours(1);
            order.HandInWhen = order.CreatedDateTime;
            order.ReturnedWhen = order.HandInWhen.AddDays(1);
            order.BorrowedPhone = "Samsung A52";
            order.IsOrderComplete = true;
            return order;
        }

        protected void SetupLookup(int id, Order? order)
        {
            queryService.Setup(x => x.GetEntity(It.Is<SearchableOrder>(s => s.Id == id))).ReturnsAsync(order);
        }

        protected void SetupCustomerLookup(int id, Customer? customer)
        {
            customerQueryService.Setup(x => x.GetEntity(It.Is<SearchableCustomer>(s => s.Id == id))).ReturnsAsync(customer);
        }

        protected void AssertDto(GetOrderDto dto, Order order)
        {
            dto.Id.Should().Be(order.Id);
            dto.Version.Should().Equal(order.Version);
            dto.CreatedDateTime.Should().Be(order.CreatedDateTime);
            dto.UpdatedDateTime.Should().Be(order.UpdatedDateTime);
            dto.HandInWhat.Should().Be(order.HandInWhat);
            dto.RepairWhat.Should().Be(order.RepairWhat);
            dto.CustomerId.Should().Be(order.CustomerId);
            dto.BorrowedPhone.Should().Be(order.BorrowedPhone);
            dto.HandInWhen.Should().Be(order.HandInWhen);
            dto.ReturnedWhen.Should().Be(order.ReturnedWhen);
            dto.IsOrderComplete.Should().Be(order.IsOrderComplete);
            dto.Customer.Should().NotBeNull();
            dto.Customer.Id.Should().Be(order.Customer.Id);
            dto.Customer.Name.Should().Be(order.Customer.Name);
            dto.Customer.Phone.Should().Be(order.Customer.Phone);
            dto.Customer.Email.Should().Be(order.Customer.Email);
            dto.Customer.Version.Should().Equal(order.Customer.Version);
            dto.Customer.CreatedDateTime.Should().Be(order.Customer.CreatedDateTime);
            dto.Customer.UpdatedDateTime.Should().Be(order.Customer.UpdatedDateTime);
        }

        protected AddOrderDto CreateAdd(int customerId = 12, string device = "iPhone 14")
        {
            return DtoFactory.CreateAddOrder(device, "Battery replacement", customerId, "Nokia 3310",
                new DateTime(2026, 7, 1), new DateTime(2026, 7, 2), true);
        }

        protected UpdateOrderDto CreateUpdate(byte[] version, int customerId = 12)
        {
            return DtoFactory.CreateUpdateOrder("Updated phone", "Battery replacement", customerId, "Nokia 3310",
                new DateTime(2026, 7, 1), null, false, version);
        }

        protected void AssertInput(Order order, IOrderData dto, Customer customer)
        {
            order.HandInWhat.Should().Be(dto.HandInWhat);
            order.RepairWhat.Should().Be(dto.RepairWhat);
            order.CustomerId.Should().Be(dto.CustomerId);
            order.BorrowedPhone.Should().Be(dto.BorrowedPhone);
            order.HandInWhen.Should().Be(dto.HandInWhen);
            order.ReturnedWhen.Should().Be(dto.ReturnedWhen);
            order.IsOrderComplete.Should().Be(dto.IsOrderComplete);
            order.Customer.Should().BeSameAs(customer);
        }
    }

    public class GetSingle : Fixture
    {
        [Test]
        public async Task WithExistingId_ReturnsMappedOrder()
        {
            // Arrange
            var order = CreateOrder();
            SetupLookup(order.Id, order);

            // Act
            var result = await api.GetSingle(order.Id);

            // Assert
            AssertDto(result, order);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithMissingId_ThrowsEntityNotFoundException()
        {
            // Arrange
            SetupLookup(12, null);

            // Act
            Func<Task> act = () => api.GetSingle(12);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>();
            queryService.VerifyAll();
        }
    }

    public class GetAll : Fixture
    {
        [Test]
        public async Task WithResults_ReturnsMappedOrders()
        {
            // Arrange
            Order[] orders = [CreateOrder(), CreateOrder(13)];
            queryService.Setup(x => x.GetAllEntities()).ReturnsAsync(orders);

            // Act
            var result = (await api.GetAll()).ToList();

            // Assert
            result.Should().HaveCount(2);
            AssertDto(result[0], orders[0]);
            AssertDto(result[1], orders[1]);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithNoResults_ReturnsEmptyCollection()
        {
            // Arrange
            Order[] orders = [];
            queryService.Setup(x => x.GetAllEntities()).ReturnsAsync(orders);

            // Act
            var result = (await api.GetAll()).ToList();

            // Assert
            result.Should().BeEmpty();
            queryService.VerifyAll();
        }
    }

    public class Search : Fixture
    {
        [Test]
        public async Task WithResults_ReturnsMappedOrders()
        {
            // Arrange
            var searchable = new SearchableOrder { HandInWhat = "Phone 12", };
            Order[] orders = [CreateOrder(), CreateOrder(13)];
            queryService.Setup(x => x.GetEntities(searchable)).ReturnsAsync(orders);

            // Act
            var result = (await api.Search(searchable)).ToList();

            // Assert
            result.Should().HaveCount(2);
            AssertDto(result[0], orders[0]);
            AssertDto(result[1], orders[1]);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithNoResults_ReturnsEmptyCollection()
        {
            // Arrange
            var searchable = new SearchableOrder { HandInWhat = "Phone 12", };
            Order[] orders = [];
            queryService.Setup(x => x.GetEntities(searchable)).ReturnsAsync(orders);

            // Act
            var result = (await api.Search(searchable)).ToList();

            // Assert
            result.Should().BeEmpty();
            queryService.VerifyAll();
        }
    }

    public class SearchComplex : Fixture
    {
        [Test]
        public async Task WithResults_ReturnsMappedOrders()
        {
            // Arrange
            var searchable = new ComplexSearchableOrder { HandInWhat = "Phone", };
            Order[] orders = [CreateOrder(), CreateOrder(13)];
            queryService.Setup(x => x.GetEntitiesComplex(searchable)).ReturnsAsync(orders);

            // Act
            var result = (await api.SearchComplex(searchable)).ToList();

            // Assert
            result.Should().HaveCount(2);
            AssertDto(result[0], orders[0]);
            AssertDto(result[1], orders[1]);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithNoResults_ReturnsEmptyCollection()
        {
            // Arrange
            var searchable = new ComplexSearchableOrder { HandInWhat = "Phone", };
            Order[] orders = [];
            queryService.Setup(x => x.GetEntitiesComplex(searchable)).ReturnsAsync(orders);

            // Act
            var result = (await api.SearchComplex(searchable)).ToList();

            // Assert
            result.Should().BeEmpty();
            queryService.VerifyAll();
        }
    }

    public class AddSingle : Fixture
    {
        [Test]
        public async Task WithValidCustomer_AddsOrderAndReturnsCreatedDto()
        {
            // Arrange
            var customer = CreateCustomer();
            var dto = CreateAdd(customer.Id);
            SetupCustomerLookup(customer.Id, customer);
            Order? capturedOrder = null;
            queryService.Setup(x => x.AddEntity(It.IsAny<Order>(), true))
                .Callback<Order, bool>((order, _) =>
                {
                    capturedOrder = order;
                    order.WithId(42);
                }).Returns(Task.CompletedTask);
            urlHelper.Setup(x => x.Action(It.Is<UrlActionContext>(c => c.Action == nameof(OrderController.GetSingle)
                && new Microsoft.AspNetCore.Routing.RouteValueDictionary(c.Values)["id"]!.Equals(42))))
                .Returns("/api/Order/GetSingle/42");

            // Act
            var result = await api.AddSingle(dto);

            // Assert
            capturedOrder.Should().NotBeNull();
            AssertInput(capturedOrder!, dto, customer);
            AssertDto(result, capturedOrder!);
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
            httpContext.Response.Headers.Location.ToString().Should().Be("/api/Order/GetSingle/42");
            queryService.Verify(x => x.AddEntity(It.IsAny<Order>(), true), Times.Once());
            customerQueryService.VerifyAll();
            urlHelper.VerifyAll();
        }

        [Test]
        public async Task WithInvalidCustomerId_ThrowsEntityValidationException()
        {
            // Arrange
            var dto = CreateAdd(0);

            // Act
            Func<Task> act = () => api.AddSingle(dto);

            // Assert
            await act.Should().ThrowAsync<EntityValidationException>();
            customerQueryService.VerifyNoOtherCalls();
            queryService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task WithMissingCustomer_ThrowsEntityValidationException()
        {
            // Arrange
            var dto = CreateAdd();
            SetupCustomerLookup(dto.CustomerId, null);

            // Act
            Func<Task> act = () => api.AddSingle(dto);

            // Assert
            await act.Should().ThrowAsync<EntityValidationException>();
            customerQueryService.VerifyAll();
            queryService.VerifyNoOtherCalls();
        }
    }

    public class AddMultiple : Fixture
    {
        [Test]
        public async Task WithValidCustomers_AddsOrdersAndLoadsEachCustomerOnce()
        {
            // Arrange
            var first = CreateCustomer(12);
            var second = CreateCustomer(13);
            AddOrderDto[] dtos = [CreateAdd(12), CreateAdd(13, "Pixel"), CreateAdd(12, "Galaxy")];
            Customer[] expectedCustomers = [first, second, first];
            SetupCustomerLookup(12, first);
            SetupCustomerLookup(13, second);
            var capturedOrders = new List<Order>();
            queryService.Setup(x => x.AddEntities(It.IsAny<IEnumerable<Order>>(), true))
                .Callback<IEnumerable<Order>, bool>((orders, _) =>
                {
                    capturedOrders.AddRange(orders);
                    for (int i = 0; i < capturedOrders.Count; i++)
                        capturedOrders[i].WithId(42 + i);
                }).Returns(Task.CompletedTask);

            // Act
            var result = (await api.AddMultiple(dtos)).ToList();

            // Assert
            capturedOrders.Should().HaveCount(3);
            result.Should().HaveCount(3);
            for (int i = 0; i < dtos.Length; i++)
            {
                AssertInput(capturedOrders[i], dtos[i], expectedCustomers[i]);
                AssertDto(result[i], capturedOrders[i]);
            }
            queryService.Verify(x => x.AddEntities(It.IsAny<IEnumerable<Order>>(), true), Times.Once());
            customerQueryService.Verify(x => x.GetEntity(It.Is<SearchableCustomer>(s => s.Id == 12)), Times.Once());
            customerQueryService.Verify(x => x.GetEntity(It.Is<SearchableCustomer>(s => s.Id == 13)), Times.Once());
        }

        [Test]
        public async Task WithMissingCustomer_RejectsEntireBatch()
        {
            // Arrange
            SetupCustomerLookup(12, CreateCustomer());
            SetupCustomerLookup(13, null);
            AddOrderDto[] dtos = [CreateAdd(12), CreateAdd(13)];

            // Act
            Func<Task> act = () => api.AddMultiple(dtos);

            // Assert
            await act.Should().ThrowAsync<EntityValidationException>();
            customerQueryService.VerifyAll();
            queryService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task WithInvalidCustomerId_RejectsEntireBatch()
        {
            // Arrange
            SetupCustomerLookup(12, CreateCustomer());
            AddOrderDto[] dtos = [CreateAdd(12), CreateAdd(-1)];

            // Act
            Func<Task> act = () => api.AddMultiple(dtos);

            // Assert
            await act.Should().ThrowAsync<EntityValidationException>();
            customerQueryService.VerifyAll();
            customerQueryService.VerifyNoOtherCalls();
            queryService.VerifyNoOtherCalls();
        }
    }

    public class UpdateSingle : Fixture
    {
        [Test]
        public async Task WithMatchingVersion_ReassignsCustomerAndUpdatesOrder()
        {
            // Arrange
            var order = CreateOrder();
            var dto = CreateUpdate(order.Version.ToArray());
            var customer = CreateCustomer(dto.CustomerId);
            SetupCustomerLookup(customer.Id, customer);
            SetupLookup(order.Id, order);
            queryService.Setup(x => x.UpdateEntity(order, true)).Returns(Task.CompletedTask);

            // Act
            var result = await api.UpdateSingle(order.Id, dto);

            // Assert
            AssertInput(order, dto, customer);
            AssertDto(result, order);
            queryService.VerifyAll();
            customerQueryService.VerifyAll();
        }

        [Test]
        public async Task WithMissingId_ThrowsEntityNotFoundException()
        {
            // Arrange
            SetupLookup(12, null);
            var dto = CreateUpdate([1]);
            SetupCustomerLookup(dto.CustomerId, CreateCustomer());

            // Act
            Func<Task> act = () => api.UpdateSingle(12, dto);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>();
            queryService.VerifyAll();
            customerQueryService.VerifyAll();
        }

        [Test]
        public async Task WithStaleVersion_ThrowsEntityConcurrencyException()
        {
            // Arrange
            var order = CreateOrder();
            SetupLookup(12, order);
            var dto = CreateUpdate([9]);
            SetupCustomerLookup(dto.CustomerId, CreateCustomer());

            // Act
            Func<Task> act = () => api.UpdateSingle(12, dto);

            // Assert
            await act.Should().ThrowAsync<EntityConcurrencyException>();
            queryService.VerifyAll();
            customerQueryService.VerifyAll();
        }

        [Test]
        public async Task WithDatabaseConflict_ThrowsEntityConcurrencyException()
        {
            // Arrange
            var order = CreateOrder();
            SetupLookup(12, order);
            var dto = CreateUpdate(order.Version.ToArray());
            SetupCustomerLookup(dto.CustomerId, CreateCustomer());
            queryService.Setup(x => x.UpdateEntity(order, true))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            // Act
            Func<Task> act = () => api.UpdateSingle(12, dto);

            // Assert
            await act.Should().ThrowAsync<EntityConcurrencyException>();
            queryService.VerifyAll();
            customerQueryService.VerifyAll();
        }
        [Test]
        public async Task WithMissingCustomer_ThrowsEntityValidationException()
        {
            // Arrange
            var dto = CreateUpdate([1]);
            SetupCustomerLookup(dto.CustomerId, null);

            // Act
            Func<Task> act = () => api.UpdateSingle(42, dto);

            // Assert
            await act.Should().ThrowAsync<EntityValidationException>();
            customerQueryService.VerifyAll();
            queryService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task WithInvalidCustomerId_ThrowsEntityValidationException()
        {
            // Arrange
            var dto = CreateUpdate([1], 0);

            // Act
            Func<Task> act = () => api.UpdateSingle(42, dto);

            // Assert
            await act.Should().ThrowAsync<EntityValidationException>();
            customerQueryService.VerifyNoOtherCalls();
            queryService.VerifyNoOtherCalls();
        }
    }

    public class DeleteSingle : Fixture
    {
        [Test]
        public async Task WithExistingId_DeletesOrder()
        {
            // Arrange
            SetupLookup(12, CreateOrder());
            queryService.Setup(x => x.DeleteEntityById(12, true)).Returns(Task.CompletedTask);

            // Act
            await api.DeleteSingle(12);

            // Assert
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithMissingId_ThrowsEntityNotFoundException()
        {
            // Arrange
            SetupLookup(12, null);

            // Act
            Func<Task> act = () => api.DeleteSingle(12);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>();
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithDatabaseConflict_ThrowsEntityConcurrencyException()
        {
            // Arrange
            SetupLookup(12, CreateOrder());
            queryService.Setup(x => x.DeleteEntityById(12, true))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            // Act
            Func<Task> act = () => api.DeleteSingle(12);

            // Assert
            await act.Should().ThrowAsync<EntityConcurrencyException>();
            queryService.VerifyAll();
        }
    }
}
