using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

public class CustomerControllerTests
{
    public class Fixture
    {
        protected readonly Mock<IEntityQueryService<Customer, SearchableCustomer>> queryService = new(MockBehavior.Strict);
        protected readonly Mock<IUrlHelper> urlHelper = new(MockBehavior.Strict);
        protected readonly DefaultHttpContext httpContext = new();
        protected readonly ICustomerApi api;

        public Fixture()
        {
            api = new CustomerController(queryService.Object, NullLogger<CustomerController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext, },
                Url = urlHelper.Object,
            };
        }

        protected Customer CreateCustomer(int id = 12)
        {
            var customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com").WithId(id);
            customer.Version = [1, 2, 3];
            customer.CreatedDateTime = new DateTime(2026, 6, 18, 10, 30, 0, DateTimeKind.Utc);
            customer.UpdatedDateTime = customer.CreatedDateTime.AddHours(1);
            return customer;
        }

        protected void SetupLookup(int id, Customer? customer)
        {
            queryService.Setup(x => x.GetEntity(It.Is<SearchableCustomer>(s => s.Id == id)))
                .ReturnsAsync(customer);
        }

        protected void AssertDto(GetCustomerDto dto, Customer customer)
        {
            dto.Id.Should().Be(customer.Id);
            dto.Name.Should().Be(customer.Name);
            dto.Phone.Should().Be(customer.Phone);
            dto.Email.Should().Be(customer.Email);
            dto.Version.Should().Equal(customer.Version);
            dto.CreatedDateTime.Should().Be(customer.CreatedDateTime);
            dto.UpdatedDateTime.Should().Be(customer.UpdatedDateTime);
        }

        protected UpdateCustomerDto CreateUpdate(byte[] version)
        {
            return DtoFactory.CreateUpdateCustomer("Updated name", "87654321", "updated@example.com", version);
        }
    }

    public class GetSingle : Fixture
    {
        [Test]
        public async Task WithExistingId_ReturnsMappedCustomer()
        {
            // Arrange
            var customer = CreateCustomer();
            SetupLookup(customer.Id, customer);

            // Act
            var result = await api.GetSingle(customer.Id);

            // Assert
            AssertDto(result, customer);
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
        public async Task WithResults_ReturnsMappedCustomers()
        {
            // Arrange
            Customer[] customers = [CreateCustomer(), CreateCustomer(13)];
            queryService.Setup(x => x.GetAllEntities()).ReturnsAsync(customers);

            // Act
            var result = (await api.GetAll()).ToList();

            // Assert
            result.Should().HaveCount(2);
            AssertDto(result[0], customers[0]);
            AssertDto(result[1], customers[1]);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithNoResults_ReturnsEmptyCollection()
        {
            // Arrange
            Customer[] customers = [];
            queryService.Setup(x => x.GetAllEntities()).ReturnsAsync(customers);

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
        public async Task WithResults_ReturnsMappedCustomers()
        {
            // Arrange
            var searchable = new SearchableCustomer { Name = "André", };
            Customer[] customers = [CreateCustomer(), CreateCustomer(13)];
            queryService.Setup(x => x.GetEntities(searchable)).ReturnsAsync(customers);

            // Act
            var result = (await api.Search(searchable)).ToList();

            // Assert
            result.Should().HaveCount(2);
            AssertDto(result[0], customers[0]);
            AssertDto(result[1], customers[1]);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithNoResults_ReturnsEmptyCollection()
        {
            // Arrange
            var searchable = new SearchableCustomer { Name = "André", };
            Customer[] customers = [];
            queryService.Setup(x => x.GetEntities(searchable)).ReturnsAsync(customers);

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
        public async Task WithResults_ReturnsMappedCustomers()
        {
            // Arrange
            var searchable = new ComplexSearchableCustomer { Name = "and", };
            Customer[] customers = [CreateCustomer(), CreateCustomer(13)];
            queryService.Setup(x => x.GetEntitiesComplex(searchable)).ReturnsAsync(customers);

            // Act
            var result = (await api.SearchComplex(searchable)).ToList();

            // Assert
            result.Should().HaveCount(2);
            AssertDto(result[0], customers[0]);
            AssertDto(result[1], customers[1]);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithNoResults_ReturnsEmptyCollection()
        {
            // Arrange
            var searchable = new ComplexSearchableCustomer { Name = "and", };
            Customer[] customers = [];
            queryService.Setup(x => x.GetEntitiesComplex(searchable)).ReturnsAsync(customers);

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
        public async Task WithCustomerData_AddsCustomerAndReturnsCreatedDto()
        {
            // Arrange
            AddCustomerDto dto = DtoFactory.CreateAddCustomer("New", "12345678", "new@example.com");
            Customer? capturedCustomer = null;

            queryService.Setup(x => x.AddEntity(It.IsAny<Customer>(), true)).Callback<Customer, bool>((customer, _) =>
            {
                capturedCustomer = customer;
                customer.WithId(12);
            }).Returns(Task.CompletedTask);
            urlHelper.Setup(x => x.Action(It.Is<UrlActionContext>(c => c.Action == nameof(CustomerController.GetSingle)
                                                                       && new Microsoft.AspNetCore.Routing.RouteValueDictionary(c.Values)["id"]!.Equals(12))))
                .Returns("/api/Customer/GetSingle/12");

            // Act
            var result = await api.AddSingle(dto);

            // Assert
            capturedCustomer.Should().NotBeNull();
            capturedCustomer!.Name.Should().Be(dto.Name);
            capturedCustomer.Phone.Should().Be(dto.Phone);
            capturedCustomer.Email.Should().Be(dto.Email);
            AssertDto(result, capturedCustomer);
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
            httpContext.Response.Headers.Location.ToString().Should().Be("/api/Customer/GetSingle/12");
            queryService.Verify(x => x.AddEntity(It.IsAny<Customer>(), true), Times.Once());
            urlHelper.VerifyAll();
        }
    }

    public class AddMultiple : Fixture
    {
        [Test]
        public async Task WithCustomers_AddsCustomersAndReturnsCreatedDtos()
        {
            // Arrange
            AddCustomerDto[] dtos =
            [
                DtoFactory.CreateAddCustomer("André", "12345678", "andre@example.com"),
                DtoFactory.CreateAddCustomer("Sofie", "87654321", "sofie@example.com"),
            ];
            var capturedCustomers = new List<Customer>();
            queryService.Setup(x => x.AddEntities(It.IsAny<IEnumerable<Customer>>(), true))
                .Callback<IEnumerable<Customer>, bool>((customers, _) =>
                {
                    capturedCustomers.AddRange(customers);

                    for (var i = 0; i < capturedCustomers.Count; i++)
                    {
                        capturedCustomers[i].WithId(12 + i);
                    }
                }).Returns(Task.CompletedTask);

            // Act
            var result = (await api.AddMultiple(dtos)).ToList();

            // Assert
            capturedCustomers.Should().HaveCount(2);
            result.Should().HaveCount(2);
            for (int i = 0; i < dtos.Length; i++)
            {
                capturedCustomers[i].Name.Should().Be(dtos[i].Name);
                capturedCustomers[i].Phone.Should().Be(dtos[i].Phone);
                capturedCustomers[i].Email.Should().Be(dtos[i].Email);
                AssertDto(result[i], capturedCustomers[i]);
            }

            queryService.Verify(x => x.AddEntities(It.IsAny<IEnumerable<Customer>>(), true), Times.Once());
        }
    }

    public class UpdateSingle : Fixture
    {
        [Test]
        public async Task WithMatchingVersion_UpdatesAndReturnsCustomer()
        {
            // Arrange
            var customer = CreateCustomer();
            var dto = CreateUpdate(customer.Version.ToArray());
            SetupLookup(customer.Id, customer);
            queryService.Setup(x => x.UpdateEntity(customer, true)).Returns(Task.CompletedTask);

            // Act
            var result = await api.UpdateSingle(customer.Id, dto);

            // Assert
            customer.Name.Should().Be(dto.Name);
            customer.Phone.Should().Be(dto.Phone);
            customer.Email.Should().Be(dto.Email);
            AssertDto(result, customer);
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithMissingId_ThrowsEntityNotFoundException()
        {
            // Arrange
            SetupLookup(12, null);
            var dto = CreateUpdate([1]);

            // Act
            Func<Task> act = () => api.UpdateSingle(12, dto);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>();
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithStaleVersion_ThrowsEntityConcurrencyException()
        {
            // Arrange
            var customer = CreateCustomer();
            SetupLookup(12, customer);
            var dto = CreateUpdate([9]);

            // Act
            Func<Task> act = () => api.UpdateSingle(12, dto);

            // Assert
            await act.Should().ThrowAsync<EntityConcurrencyException>();
            queryService.VerifyAll();
        }

        [Test]
        public async Task WithDatabaseConflict_ThrowsEntityConcurrencyException()
        {
            // Arrange
            var customer = CreateCustomer();
            SetupLookup(12, customer);
            var dto = CreateUpdate(customer.Version.ToArray());
            queryService.Setup(x => x.UpdateEntity(customer, true))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            // Act
            Func<Task> act = () => api.UpdateSingle(12, dto);

            // Assert
            await act.Should().ThrowAsync<EntityConcurrencyException>();
            queryService.VerifyAll();
        }
    }

    public class DeleteSingle : Fixture
    {
        [Test]
        public async Task WithExistingId_DeletesCustomer()
        {
            // Arrange
            SetupLookup(12, CreateCustomer());
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
            SetupLookup(12, CreateCustomer());
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
