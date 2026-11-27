using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Repair.Models.Entity.ComplexSearchable;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;
using Repair.Persistence;
using Repair.Persistence.Services;
using Repair.Tests.Core;
using Repair.Tests.Shared.Factory;

namespace Repair.Tests.QueryServiceTests;

public class OrderQueryServiceTests
{
    public class GetEntitiesComplex : BaseDatabaseTest
    {
        [Test]
        public async Task WithModifiedCustomer_ReturnsUpdatedCustomerVersion()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            await using RepairDatabaseContext readContext = CreateContext();
            var service = new OrderQueryService(readContext);
            ComplexSearchableOrder searchable = new() {HandInWhat = "Phone",};
            Order original = (await service.GetEntitiesComplex(searchable)).Single();
            byte[] originalVersion = original.Version.ToArray();
            byte[] originalCustomerVersion = original.Customer.Version.ToArray();
            customer.Name = "Updated André";
            await new CustomerQueryService(context).UpdateEntity(customer);

            // Act
            await using RepairDatabaseContext updatedContext = CreateContext();
            var updatedService = new OrderQueryService(updatedContext);
            Order updated = (await updatedService.GetEntitiesComplex(searchable)).Single();

            // Assert
            updated.Version.Should().Equal(originalVersion);
            updated.Customer.Name.Should().Be("Updated André");
            updated.Customer.Version.Should().NotBeNullOrEmpty();
            updated.Customer.Version.Should().NotEqual(originalCustomerVersion);
            await using RepairDatabaseContext verificationContext = CreateContext();
            Customer persisted = await verificationContext.Customers.SingleAsync(x => x.Id == customer.Id);
            updated.Customer.Version.Should().Equal(persisted.Version);
        }

        [Test]
        public async Task ReturnsUpdatedVersionAfterModification()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            await using RepairDatabaseContext readContext = CreateContext();
            var service = new OrderQueryService(readContext);
            ComplexSearchableOrder searchable = new() {HandInWhat = "Phone",};
            Order original = (await service.GetEntitiesComplex(searchable)).Single();
            byte[] originalVersion = original.Version.ToArray();
            order.RepairWhat = "Battery";
            await new OrderQueryService(context).UpdateEntity(order);

            // Act
            await using RepairDatabaseContext updatedContext = CreateContext();
            var updatedService = new OrderQueryService(updatedContext);
            Order updated = (await updatedService.GetEntitiesComplex(searchable)).Single();

            // Assert
            updated.RepairWhat.Should().Be("Battery");
            updated.Version.Should().NotBeNullOrEmpty();
            updated.Version.Should().NotEqual(originalVersion);
            await using RepairDatabaseContext verificationContext = CreateContext();
            Order persisted = await verificationContext.Orders.SingleAsync(x => x.Id == order.Id);
            updated.Version.Should().Equal(persisted.Version);
        }

        [Test]
        public async Task WithHandInWhat_ReturnsMatchesAndIgnoresCasing()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("iPhone 13", "Screen", customer);
            Order other = EntityFactory.CreateOrder("Samsung", "Screen", customer);

            context.Orders.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new() {HandInWhat = "iphone",};

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithRepairWhat_ReturnsMatchesAndIgnoresCasing()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Battery replacement", customer);
            Order other = EntityFactory.CreateOrder("Phone", "Screen repair", customer);

            context.Orders.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new() {RepairWhat = "BATTERY",};

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithFuzzyCustomerName_ReturnsMatchesAndIgnoresCasing()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer expectedCustomer = EntityFactory.CreateCustomer("Andre Madsen", "12345678", "andre@example.com");
            Customer otherCustomer = EntityFactory.CreateCustomer("Sofie Jensen", "87654321", "sofie@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", expectedCustomer);
            Order other = EntityFactory.CreateOrder("Tablet", "Battery", otherCustomer);

            context.Orders.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new()
            {
                CustomerName = "madsen",
                UseFuzzy = true,
            };

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithExactCustomerName_ReturnsMatchesAndIgnoresCasing()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer expectedCustomer = EntityFactory.CreateCustomer("Andre Madsen", "12345678", "andre@example.com");
            Customer otherCustomer = EntityFactory.CreateCustomer("Andre", "87654321", "other@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", expectedCustomer);
            Order other = EntityFactory.CreateOrder("Tablet", "Battery", otherCustomer);

            context.Orders.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new()
            {
                CustomerName = "andre madsen",
                UseFuzzy = false,
            };

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithIsOrderComplete_ReturnsMatches()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);
            Order other = EntityFactory.CreateOrder("Tablet", "Battery", customer);

            expected.IsOrderComplete = true;
            other.IsOrderComplete = false;

            context.Orders.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new() {IsOrderComplete = true,};

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithBorrowedDevice_ReturnsMatchesAndIgnoresCasing()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);
            Order other = EntityFactory.CreateOrder("Tablet", "Battery", customer);
            Order noBorrowedDevice = EntityFactory.CreateOrder("Laptop", "Keyboard", customer);

            expected.BorrowedDevice = "iPhone 8";
            other.BorrowedDevice = "Samsung A52";
            noBorrowedDevice.BorrowedDevice = null;

            context.Orders.AddRange(expected, other, noBorrowedDevice);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new() {BorrowedDevice = "iphone",};

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithHandInFromAndHandInTo_ReturnsMatchesInsideRange()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);
            Order tooEarly = EntityFactory.CreateOrder("Tablet", "Battery", customer);
            Order tooLate = EntityFactory.CreateOrder("Laptop", "Keyboard", customer);

            expected.HandInWhen = new DateTime(2026, 05, 10);
            tooEarly.HandInWhen = new DateTime(2026, 05, 01);
            tooLate.HandInWhen = new DateTime(2026, 05, 20);

            context.Orders.AddRange(expected, tooEarly, tooLate);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new()
            {
                HandInFrom = new DateTime(2026, 05, 05),
                HandInTo = new DateTime(2026, 05, 15),
            };

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithReturnedFromAndReturnedTo_ReturnsMatchesInsideRange()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);
            Order tooEarly = EntityFactory.CreateOrder("Tablet", "Battery", customer);
            Order noReturnedWhen = EntityFactory.CreateOrder("Laptop", "Keyboard", customer);

            expected.ReturnedWhen = new DateTime(2026, 05, 10);
            tooEarly.ReturnedWhen = new DateTime(2026, 05, 01);
            noReturnedWhen.ReturnedWhen = null;

            context.Orders.AddRange(expected, tooEarly, noReturnedWhen);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new()
            {
                ReturnedFrom = new DateTime(2026, 05, 05),
                ReturnedTo = new DateTime(2026, 05, 15),
            };

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithStringBooleanAndDateFilters_ReturnsOnlyFullMatch()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("Andre Madsen", "12345678", "andre@example.com");

            Order expected = EntityFactory.CreateOrder("iPhone 13", "Screen replacement", customer);
            Order wrongRepair = EntityFactory.CreateOrder("iPhone 13", "Battery replacement", customer);
            Order wrongComplete = EntityFactory.CreateOrder("iPhone 13", "Screen replacement", customer);

            expected.IsOrderComplete = true;
            expected.HandInWhen = new DateTime(2026, 05, 10);

            wrongRepair.IsOrderComplete = true;
            wrongRepair.HandInWhen = new DateTime(2026, 05, 10);

            wrongComplete.IsOrderComplete = false;
            wrongComplete.HandInWhen = new DateTime(2026, 05, 10);

            expected.BorrowedDevice = "iPhone 8";
            wrongRepair.BorrowedDevice = "iPhone 8";
            wrongComplete.BorrowedDevice = "Samsung A52";

            context.Orders.AddRange(expected, wrongRepair, wrongComplete);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            ComplexSearchableOrder complex = new()
            {
                HandInWhat = "iphone",
                RepairWhat = "SCREEN",
                BorrowedDevice = "iphone",
                IsOrderComplete = true,
                HandInFrom = new DateTime(2026, 05, 05),
                HandInTo = new DateTime(2026, 05, 15),
            };

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }
    }

    public class GetEntity : BaseDatabaseTest
    {
        [Test]
        public async Task PreservesVersionAcrossReads()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            byte[] originalVersion = order.Version.ToArray();
            SearchableOrder searchable = new() {Id = order.Id,};

            // Act
            await using RepairDatabaseContext firstContext = CreateContext();
            Order? first = await new OrderQueryService(firstContext).GetEntity(searchable);
            await using RepairDatabaseContext secondContext = CreateContext();
            Order? second = await new OrderQueryService(secondContext).GetEntity(searchable);

            // Assert
            originalVersion.Should().NotBeNullOrEmpty();
            first.Should().NotBeNull();
            second.Should().NotBeNull();
            first!.Version.Should().Equal(originalVersion);
            second!.Version.Should().Equal(originalVersion);
        }

        [Test]
        public async Task WithMatchingBorrowedDevice_ReturnsOrder()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);

            expected.BorrowedDevice = "iPhone 8";

            context.Orders.Add(expected);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {BorrowedDevice = "IPHONE 8",};

            Order? result = await service.GetEntity(searchable);

            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.BorrowedDevice.Should().Be("iPhone 8");
        }

        [Test]
        public async Task WithMatchingId_ReturnsOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(expected);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {Id = expected.Id,};

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.HandInWhat.Should().Be("Phone");
        }

        [Test]
        public async Task WithMatchingHandInWhat_ReturnsOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(expected);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {HandInWhat = "Phone",};

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.HandInWhat.Should().Be("Phone");
        }

        [Test]
        public async Task WithHandInWhat_IsCaseInsensitive()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(expected);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {HandInWhat = "pHoNe",};

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.HandInWhat.Should().Be("Phone");
        }

        [Test]
        public async Task WithMatchingRepairWhat_ReturnsOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(expected);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {RepairWhat = "Screen",};

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.RepairWhat.Should().Be("Screen");
        }

        [Test]
        public async Task WithRepairWhat_IsCaseInsensitive()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(expected);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {RepairWhat = "sCrEeN",};

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.RepairWhat.Should().Be("Screen");
        }

        [Test]
        public async Task WithMatchingCustomerId_ReturnsOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expectedCustomer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Customer otherCustomer = EntityFactory.CreateCustomer("Sofie", "87654321", "sofie@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", expectedCustomer);
            Order other = EntityFactory.CreateOrder("Tablet", "Battery", otherCustomer);

            context.Orders.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {CustomerId = expectedCustomer.Id,};

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.CustomerId.Should().Be(expectedCustomer.Id);
        }

        [Test]
        public async Task WithCombinedSearchArguments_ReturnsMatchingOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order expected = EntityFactory.CreateOrder("Phone", "Screen", customer);
            Order other = EntityFactory.CreateOrder("Phone", "Battery", customer);

            context.Orders.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new()
            {
                HandInWhat = "PHONE",
                RepairWhat = "screen",
                CustomerId = customer.Id,
            };

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithNonMatchingHandInWhat_ReturnsNull()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {HandInWhat = "Tablet",};

            // Act
            Order? result = await service.GetEntity(searchable);

            // Assert
            result.Should().BeNull();
        }
    }

    public class GetEntities : BaseDatabaseTest
    {
        [Test]
        public async Task WithMatchingHandInWhat_ReturnsAllMatches()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order[] orders =
            [
                EntityFactory.CreateOrder("Phone", "Screen", customer),
                EntityFactory.CreateOrder("phone", "Battery", customer),
                EntityFactory.CreateOrder("Tablet", "Screen", customer),
            ];

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {HandInWhat = "PHONE",};

            // Act
            var result = await service.GetEntities(searchable);
            var enumerable = result.ToList();

            // Assert
            enumerable.Should().HaveCount(2);
            enumerable.Select(x => x.RepairWhat).Should().BeEquivalentTo("Screen", "Battery");
        }

        [Test]
        public async Task WithNoMatches_ReturnsEmptyCollection()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            context.Orders.AddRange(EntityFactory.CreateOrder("Phone", "Screen", customer),
                EntityFactory.CreateOrder("Tablet", "Battery", customer));
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            SearchableOrder searchable = new() {HandInWhat = "Laptop",};

            // Act
            var result = await service.GetEntities(searchable);

            // Assert
            result.Should().BeEmpty();
        }
    }

    public class GetAllEntities : BaseDatabaseTest
    {
        [Test]
        public async Task WhenOrdersExist_ReturnsAllOrders()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order[] orders =
            [
                EntityFactory.CreateOrder("Phone", "Screen", customer),
                EntityFactory.CreateOrder("Tablet", "Battery", customer),
                EntityFactory.CreateOrder("Laptop", "Keyboard", customer),
            ];

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);

            // Act
            var result = await service.GetAllEntities();
            var enumerable = result.ToList();

            // Assert
            enumerable.Should().HaveCount(3);
            enumerable.Select(x => x.HandInWhat).Should().BeEquivalentTo("Phone", "Tablet", "Laptop");
        }

        [Test]
        public async Task WhenNoOrdersExist_ReturnsEmptyCollection()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new OrderQueryService(context);

            // Act
            var result = await service.GetAllEntities();

            // Assert
            result.Should().BeEmpty();
        }
    }

    public class AddEntity : BaseDatabaseTest
    {
        [Test]
        public async Task GeneratesVersion()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            var service = new OrderQueryService(context);
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);

            // Act
            await service.AddEntity(order);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            Order persisted = await verificationContext.Orders.SingleAsync(x => x.Id == order.Id);
            persisted.Version.Should().NotBeNullOrEmpty();
            order.Version.Should().Equal(persisted.Version);
        }

        [Test]
        public async Task WithSaveImmediatelyTrue_PersistsOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new OrderQueryService(context);
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);

            // Act
            await service.AddEntity(order);

            // Assert
            Order? persisted = await context.Orders.SingleOrDefaultAsync(x => x.HandInWhat == "Phone");
            persisted.Should().NotBeNull();
            persisted.RepairWhat.Should().Be("Screen");
        }

        [Test]
        public async Task WithSaveImmediatelyFalse_DoesNotPersistUntilSaveChanges()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new OrderQueryService(context);
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);

            // Act
            await service.AddEntity(order, false);

            // Assert
            await using RepairDatabaseContext verificationContextBeforeSave = CreateContext();
            bool existsBeforeSave = await verificationContextBeforeSave.Orders.AnyAsync(x => x.HandInWhat == "Phone");
            existsBeforeSave.Should().BeFalse();

            await context.SaveChangesAsync();

            await using RepairDatabaseContext verificationContextAfterSave = CreateContext();
            bool existsAfterSave = await verificationContextAfterSave.Orders.AnyAsync(x => x.HandInWhat == "Phone");
            existsAfterSave.Should().BeTrue();
        }
    }

    public class AddEntities : BaseDatabaseTest
    {
        [Test]
        public async Task GeneratesVersions()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            var service = new OrderQueryService(context);
            Order[] entities = [EntityFactory.CreateOrder("Phone", "Screen", customer), EntityFactory.CreateOrder("Tablet", "Keyboard", customer)];

            // Act
            await service.AddEntities(entities);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            var persisted = await verificationContext.Orders.ToListAsync();
            persisted.Should().HaveCount(2);
            foreach (Order item in persisted)
            {
                item.Version.Should().NotBeNullOrEmpty();
                entities.Single(x => x.Id == item.Id).Version.Should().Equal(item.Version);
            }
        }

        [Test]
        public async Task PersistsOrders()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new OrderQueryService(context);
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order[] orders =
            [
                EntityFactory.CreateOrder("Phone", "Screen", customer),
                EntityFactory.CreateOrder("Tablet", "Battery", customer),
            ];

            // Act
            await service.AddEntities(orders);

            // Assert
            int persistedCount = await context.Orders.CountAsync();
            persistedCount.Should().Be(2);
        }
    }

    public class UpdateEntity : BaseDatabaseTest
    {
        [Test]
        public async Task ChangesVersionOnConsecutiveUpdates()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            byte[] originalVersion = order.Version.ToArray();
            var service = new OrderQueryService(context);

            // Act
            order.RepairWhat = "Battery";
            await service.UpdateEntity(order);
            byte[] firstVersion = order.Version.ToArray();
            await using RepairDatabaseContext firstContext = CreateContext();
            Order first = await firstContext.Orders.SingleAsync(x => x.Id == order.Id);
            order.RepairWhat = "Keyboard";
            await service.UpdateEntity(order);

            // Assert
            firstVersion.Should().NotBeNullOrEmpty();
            firstVersion.Should().NotEqual(originalVersion);
            first.Version.Should().Equal(firstVersion);
            first.RepairWhat.Should().Be("Battery");
            await using RepairDatabaseContext verificationContext = CreateContext();
            Order persisted = await verificationContext.Orders.SingleAsync(x => x.Id == order.Id);
            persisted.RepairWhat.Should().Be("Keyboard");
            persisted.Version.Should().NotBeNullOrEmpty();
            persisted.Version.Should().NotEqual(firstVersion);
            persisted.Version.Should().NotEqual(originalVersion);
            order.Version.Should().Equal(persisted.Version);
        }

        [Test]
        public async Task WithStaleTrackedEntity_ThrowsConcurrencyException()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            await using RepairDatabaseContext staleContext = CreateContext();
            Order stale = await staleContext.Orders.SingleAsync(x => x.Id == order.Id);
            byte[] staleVersion = stale.Version.ToArray();
            order.RepairWhat = "Battery";
            await new OrderQueryService(context).UpdateEntity(order);
            byte[] updatedVersion = order.Version.ToArray();
            stale.RepairWhat = "Keyboard";
            var service = new OrderQueryService(staleContext);

            // Act
            Func<Task> act = () => service.UpdateEntity(stale);

            // Assert
            updatedVersion.Should().NotEqual(staleVersion);
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            await using RepairDatabaseContext verificationContext = CreateContext();
            Order persisted = await verificationContext.Orders.SingleAsync(x => x.Id == order.Id);
            persisted.RepairWhat.Should().Be("Battery");
            persisted.Version.Should().Equal(updatedVersion);
        }

        [Test]
        public async Task WithStaleDetachedEntity_ThrowsConcurrencyException()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            await using RepairDatabaseContext staleContext = CreateContext();
            Order stale = await staleContext.Orders.AsNoTracking().SingleAsync(x => x.Id == order.Id);
            byte[] staleVersion = stale.Version.ToArray();
            order.RepairWhat = "Battery";
            await new OrderQueryService(context).UpdateEntity(order);
            byte[] updatedVersion = order.Version.ToArray();
            stale.RepairWhat = "Keyboard";
            var service = new OrderQueryService(staleContext);

            // Act
            Func<Task> act = () => service.UpdateEntity(stale);

            // Assert
            updatedVersion.Should().NotEqual(staleVersion);
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            await using RepairDatabaseContext verificationContext = CreateContext();
            Order persisted = await verificationContext.Orders.SingleAsync(x => x.Id == order.Id);
            persisted.RepairWhat.Should().Be("Battery");
            persisted.Version.Should().Equal(updatedVersion);
        }

        [Test]
        public async Task ChangesVersionAndPreservesUnrelatedVersion()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);
            Order other = EntityFactory.CreateOrder("Tablet", "Keyboard", customer);
            context.Orders.AddRange(order, other);
            await context.SaveChangesAsync();

            await using RepairDatabaseContext beforeContext = CreateContext();
            var before = await beforeContext.Orders.ToDictionaryAsync(x => x.Id, x => x.Version.ToArray());
            var service = new OrderQueryService(context);
            order.RepairWhat = "Battery";

            // Act
            await service.UpdateEntity(order);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            Order updated = await verificationContext.Orders.SingleAsync(x => x.Id == order.Id);
            Order unchanged = await verificationContext.Orders.SingleAsync(x => x.Id == other.Id);
            updated.RepairWhat.Should().Be("Battery");
            updated.Version.Should().NotBeNullOrEmpty();
            updated.Version.Should().NotEqual(before[order.Id]);
            order.Version.Should().Equal(updated.Version);
            unchanged.Version.Should().Equal(before[other.Id]);
        }

        [Test]
        public async Task UpdatesOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            order.RepairWhat = "Battery";

            // Act
            await service.UpdateEntity(order);

            // Assert
            Order? updated = await context.Orders.SingleOrDefaultAsync(x => x.Id == order.Id);
            updated.Should().NotBeNull();
            updated.RepairWhat.Should().Be("Battery");
        }
    }

    public class UpdateEntities : BaseDatabaseTest
    {
        [Test]
        public async Task ChangesVersions()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order[] entities = [EntityFactory.CreateOrder("Phone", "Screen", customer), EntityFactory.CreateOrder("Tablet", "Keyboard", customer)];
            context.Orders.AddRange(entities);
            await context.SaveChangesAsync();

            await using RepairDatabaseContext beforeContext = CreateContext();
            var before = await beforeContext.Orders.ToDictionaryAsync(x => x.Id, x => x.Version.ToArray());
            var service = new OrderQueryService(context);
            foreach (Order item in entities)
                item.RepairWhat += " Updated";

            // Act
            await service.UpdateEntities(entities);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            var updated = await verificationContext.Orders.ToListAsync();
            updated.Should().HaveCount(2);
            foreach (Order item in updated)
            {
                item.Version.Should().NotBeNullOrEmpty();
                item.Version.Should().NotEqual(before[item.Id]);
                entities.Single(x => x.Id == item.Id).Version.Should().Equal(item.Version);
            }
        }

        [Test]
        public async Task UpdatesOrders()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order[] orders =
            [
                EntityFactory.CreateOrder("Phone", "Screen", customer),
                EntityFactory.CreateOrder("Tablet", "Battery", customer),
            ];

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);
            orders[0].RepairWhat = "Updated Screen";
            orders[1].RepairWhat = "Updated Battery";

            // Act
            await service.UpdateEntities(orders);

            // Assert
            var updated = await context.Orders.OrderBy(x => x.HandInWhat).ToListAsync();
            updated.Select(x => x.RepairWhat).Should().BeEquivalentTo("Updated Screen", "Updated Battery");
        }
    }

    public class DeleteEntity : BaseDatabaseTest
    {
        [Test]
        public async Task RemovesOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);

            // Act
            await service.DeleteEntity(new SearchableOrder {HandInWhat = order.HandInWhat,});

            // Assert
            Order? deleted = await context.Orders.SingleOrDefaultAsync(x => x.Id == order.Id);
            deleted.Should().BeNull();
        }
    }

    public class DeleteEntityById : BaseDatabaseTest
    {
        [Test]
        public async Task RemovesOrder()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Order order = EntityFactory.CreateOrder("Phone", "Screen", customer);

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderQueryService(context);

            // Act
            await service.DeleteEntityById(order.Id);

            // Assert
            Order? deleted = await context.Orders.SingleOrDefaultAsync(x => x.Id == order.Id);
            deleted.Should().BeNull();
        }
    }
}
