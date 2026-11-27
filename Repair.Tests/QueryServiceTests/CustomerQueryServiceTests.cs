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

public class CustomerQueryServiceTests
{
    public class GetEntitiesComplex : BaseDatabaseTest
    {
        [Test]
        public async Task ReturnsUpdatedVersionAfterModification()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            await using RepairDatabaseContext readContext = CreateContext();
            var service = new CustomerQueryService(readContext);
            ComplexSearchableCustomer searchable = new() {Phone = "12345678",};
            Customer original = (await service.GetEntitiesComplex(searchable)).Single();
            byte[] originalVersion = original.Version.ToArray();
            customer.Name = "Updated André";
            await new CustomerQueryService(context).UpdateEntity(customer);

            // Act
            await using RepairDatabaseContext updatedContext = CreateContext();
            var updatedService = new CustomerQueryService(updatedContext);
            Customer updated = (await updatedService.GetEntitiesComplex(searchable)).Single();

            // Assert
            updated.Name.Should().Be("Updated André");
            updated.Version.Should().NotBeNullOrEmpty();
            updated.Version.Should().NotEqual(originalVersion);
            await using RepairDatabaseContext verificationContext = CreateContext();
            Customer persisted = await verificationContext.Customers.SingleAsync(x => x.Id == customer.Id);
            updated.Version.Should().Equal(persisted.Version);
        }

        [Test]
        public async Task WithName_ReturnsMatchesAndIgnoresCasing()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André Madsen", "11111111", "one@example.com");
            Customer other = EntityFactory.CreateCustomer("Sofie Jensen", "22222222", "two@example.com");

            context.Customers.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            ComplexSearchableCustomer complex = new() {Name = "madsen",};

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithPhone_ReturnsMatches()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "12345678", "one@example.com");
            Customer other = EntityFactory.CreateCustomer("Sofie", "87654321", "two@example.com");

            context.Customers.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            ComplexSearchableCustomer complex = new() {Phone = "3456",};

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithEmail_ReturnsMatchesAndIgnoresCasing()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "11111111", "andre.private@example.com");
            Customer other = EntityFactory.CreateCustomer("Sofie", "22222222", "sofie@example.com");

            context.Customers.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            ComplexSearchableCustomer complex = new() {Email = "PRIVATE",};

            var result = (await service.GetEntitiesComplex(complex)).ToList();

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithNamePhoneAndEmail_ReturnsOnlyFullMatch()
        {
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André Madsen", "12345678", "andre.private@example.com");
            Customer wrongPhone = EntityFactory.CreateCustomer("André Madsen", "87654321", "andre.work@example.com");
            Customer wrongEmail = EntityFactory.CreateCustomer("André Hansen", "12345678", "andre.work@example.com");
            Customer wrongName = EntityFactory.CreateCustomer("Sofie Jensen", "12345678", "andre.private@example.com");

            context.Customers.AddRange(expected, wrongPhone, wrongEmail, wrongName);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            ComplexSearchableCustomer complex = new()
            {
                Name = "madsen",
                Phone = "3456",
                Email = "PRIVATE",
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
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            byte[] originalVersion = customer.Version.ToArray();
            SearchableCustomer searchable = new() {Id = customer.Id,};

            // Act
            await using RepairDatabaseContext firstContext = CreateContext();
            Customer? first = await new CustomerQueryService(firstContext).GetEntity(searchable);
            await using RepairDatabaseContext secondContext = CreateContext();
            Customer? second = await new CustomerQueryService(secondContext).GetEntity(searchable);

            // Assert
            originalVersion.Should().NotBeNullOrEmpty();
            first.Should().NotBeNull();
            second.Should().NotBeNull();
            first!.Version.Should().Equal(originalVersion);
            second!.Version.Should().Equal(originalVersion);
        }

        [Test]
        public async Task WithMatchingId_ReturnsCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(expected);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Id = expected.Id,};

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.Name.Should().Be("André");
        }

        [Test]
        public async Task WithMatchingName_ReturnsCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(expected);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Name = "André",};

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.Name.Should().Be("André");
        }

        [Test]
        public async Task WithName_IsCaseInsensitive()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(expected);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Name = "aNdRé",};

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.Name.Should().Be("André");
        }

        [Test]
        public async Task WithMatchingPhone_ReturnsCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(expected);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Phone = "12345678",};

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.Phone.Should().Be("12345678");
        }

        [Test]
        public async Task WithPhone_IsCaseInsensitive()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "PhoneABC", "andre@example.com");

            context.Customers.Add(expected);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Phone = "phoneabc",};

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.Phone.Should().Be("PhoneABC");
        }

        [Test]
        public async Task WithMatchingEmail_ReturnsCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "12345678", "Andre@Example.com");

            context.Customers.Add(expected);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Email = "andre@example.com",};

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            result.Email.Should().Be("Andre@Example.com");
        }

        [Test]
        public async Task WithCombinedSearchArguments_ReturnsMatchingCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer expected = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Customer other = EntityFactory.CreateCustomer("André", "87654321", "other@example.com");

            context.Customers.AddRange(expected, other);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new()
            {
                Name = "andré",
                Phone = "12345678",
                Email = "ANDRE@EXAMPLE.COM",
            };

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
        }

        [Test]
        public async Task WithNonMatchingName_ReturnsNull()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Name = "Not André",};

            // Act
            Customer? result = await service.GetEntity(searchable);

            // Assert
            result.Should().BeNull();
        }
    }

    public class GetEntities : BaseDatabaseTest
    {
        [Test]
        public async Task WithMatchingName_ReturnsAllMatches()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer[] customers =
            [
                EntityFactory.CreateCustomer("André", "11111111", "one@example.com"),
                EntityFactory.CreateCustomer("andré", "22222222", "two@example.com"),
                EntityFactory.CreateCustomer("Sofie", "33333333", "three@example.com"),
            ];

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Name = "ANDRÉ",};

            // Act
            var result = await service.GetEntities(searchable);
            var enumerable = result.ToList();

            // Assert
            enumerable.Should().HaveCount(2);
            enumerable.Select(x => x.Email).Should().BeEquivalentTo("one@example.com", "two@example.com");
        }

        [Test]
        public async Task WithNoMatches_ReturnsEmptyCollection()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            context.Customers.AddRange(EntityFactory.CreateCustomer("André", "11111111", "one@example.com"),
                EntityFactory.CreateCustomer("Sofie", "22222222", "two@example.com"));
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            SearchableCustomer searchable = new() {Name = "Mikkel",};

            // Act
            var result = await service.GetEntities(searchable);

            // Assert
            result.Should().BeEmpty();
        }
    }

    public class GetAllEntities : BaseDatabaseTest
    {
        [Test]
        public async Task WhenCustomersExist_ReturnsAllCustomers()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer[] customers =
            [
                EntityFactory.CreateCustomer("André", "11111111", "one@example.com"),
                EntityFactory.CreateCustomer("Sofie", "22222222", "two@example.com"),
                EntityFactory.CreateCustomer("Mikkel", "33333333", "three@example.com"),
            ];

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);

            // Act
            var result = await service.GetAllEntities();
            var enumerable = result.ToList();

            // Assert
            enumerable.Should().HaveCount(3);
            enumerable.Select(x => x.Name).Should().BeEquivalentTo("André", "Sofie", "Mikkel");
        }

        [Test]
        public async Task WhenNoCustomersExist_ReturnsEmptyCollection()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new CustomerQueryService(context);

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
            var service = new CustomerQueryService(context);
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            // Act
            await service.AddEntity(customer);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            Customer persisted = await verificationContext.Customers.SingleAsync(x => x.Id == customer.Id);
            persisted.Version.Should().NotBeNullOrEmpty();
            customer.Version.Should().Equal(persisted.Version);
        }

        [Test]
        public async Task WithSaveImmediatelyTrue_PersistsCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new CustomerQueryService(context);
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            // Act
            await service.AddEntity(customer);

            // Assert
            Customer? persisted = await context.Customers.SingleOrDefaultAsync(x => x.Email == "andre@example.com");
            persisted.Should().NotBeNull();
            persisted.Name.Should().Be("André");
        }

        [Test]
        public async Task WithSaveImmediatelyFalse_DoesNotPersistUntilSaveChanges()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new CustomerQueryService(context);
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            // Act
            await service.AddEntity(customer, false);

            // Assert
            await using RepairDatabaseContext verificationContextBeforeSave = CreateContext();
            bool existsBeforeSave =
                await verificationContextBeforeSave.Customers.AnyAsync(x => x.Email == "andre@example.com");
            existsBeforeSave.Should().BeFalse();

            await context.SaveChangesAsync();

            await using RepairDatabaseContext verificationContextAfterSave = CreateContext();
            bool existsAfterSave =
                await verificationContextAfterSave.Customers.AnyAsync(x => x.Email == "andre@example.com");
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
            var service = new CustomerQueryService(context);
            Customer[] entities = [EntityFactory.CreateCustomer("André", "12345678", "andre@example.com"), EntityFactory.CreateCustomer("Sofie", "87654321", "sofie@example.com")];

            // Act
            await service.AddEntities(entities);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            var persisted = await verificationContext.Customers.ToListAsync();
            persisted.Should().HaveCount(2);
            foreach (Customer item in persisted)
            {
                item.Version.Should().NotBeNullOrEmpty();
                entities.Single(x => x.Id == item.Id).Version.Should().Equal(item.Version);
            }
        }

        [Test]
        public async Task PersistsCustomers()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            var service = new CustomerQueryService(context);
            Customer[] customers =
            [
                EntityFactory.CreateCustomer("André", "11111111", "one@example.com"),
                EntityFactory.CreateCustomer("Sofie", "22222222", "two@example.com"),
            ];

            // Act
            await service.AddEntities(customers);

            // Assert
            int persistedCount = await context.Customers.CountAsync();
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
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            byte[] originalVersion = customer.Version.ToArray();
            var service = new CustomerQueryService(context);

            // Act
            customer.Name = "Updated André";
            await service.UpdateEntity(customer);
            byte[] firstVersion = customer.Version.ToArray();
            await using RepairDatabaseContext firstContext = CreateContext();
            Customer first = await firstContext.Customers.SingleAsync(x => x.Id == customer.Id);
            customer.Name = "Final André";
            await service.UpdateEntity(customer);

            // Assert
            firstVersion.Should().NotBeNullOrEmpty();
            firstVersion.Should().NotEqual(originalVersion);
            first.Version.Should().Equal(firstVersion);
            first.Name.Should().Be("Updated André");
            await using RepairDatabaseContext verificationContext = CreateContext();
            Customer persisted = await verificationContext.Customers.SingleAsync(x => x.Id == customer.Id);
            persisted.Name.Should().Be("Final André");
            persisted.Version.Should().NotBeNullOrEmpty();
            persisted.Version.Should().NotEqual(firstVersion);
            persisted.Version.Should().NotEqual(originalVersion);
            customer.Version.Should().Equal(persisted.Version);
        }

        [Test]
        public async Task WithStaleTrackedEntity_ThrowsConcurrencyException()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            await using RepairDatabaseContext staleContext = CreateContext();
            Customer stale = await staleContext.Customers.SingleAsync(x => x.Id == customer.Id);
            byte[] staleVersion = stale.Version.ToArray();
            customer.Name = "Updated André";
            await new CustomerQueryService(context).UpdateEntity(customer);
            byte[] updatedVersion = customer.Version.ToArray();
            stale.Name = "Final André";
            var service = new CustomerQueryService(staleContext);

            // Act
            Func<Task> act = () => service.UpdateEntity(stale);

            // Assert
            updatedVersion.Should().NotEqual(staleVersion);
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            await using RepairDatabaseContext verificationContext = CreateContext();
            Customer persisted = await verificationContext.Customers.SingleAsync(x => x.Id == customer.Id);
            persisted.Name.Should().Be("Updated André");
            persisted.Version.Should().Equal(updatedVersion);
        }

        [Test]
        public async Task WithStaleDetachedEntity_ThrowsConcurrencyException()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            await using RepairDatabaseContext staleContext = CreateContext();
            Customer stale = await staleContext.Customers.AsNoTracking().SingleAsync(x => x.Id == customer.Id);
            byte[] staleVersion = stale.Version.ToArray();
            customer.Name = "Updated André";
            await new CustomerQueryService(context).UpdateEntity(customer);
            byte[] updatedVersion = customer.Version.ToArray();
            stale.Name = "Final André";
            var service = new CustomerQueryService(staleContext);

            // Act
            Func<Task> act = () => service.UpdateEntity(stale);

            // Assert
            updatedVersion.Should().NotEqual(staleVersion);
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            await using RepairDatabaseContext verificationContext = CreateContext();
            Customer persisted = await verificationContext.Customers.SingleAsync(x => x.Id == customer.Id);
            persisted.Name.Should().Be("Updated André");
            persisted.Version.Should().Equal(updatedVersion);
        }

        [Test]
        public async Task ChangesVersionAndPreservesUnrelatedVersion()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");
            Customer other = EntityFactory.CreateCustomer("Sofie", "87654321", "sofie@example.com");
            context.Customers.AddRange(customer, other);
            await context.SaveChangesAsync();

            await using RepairDatabaseContext beforeContext = CreateContext();
            var before = await beforeContext.Customers.ToDictionaryAsync(x => x.Id, x => x.Version.ToArray());
            var service = new CustomerQueryService(context);
            customer.Name = "Updated André";

            // Act
            await service.UpdateEntity(customer);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            Customer updated = await verificationContext.Customers.SingleAsync(x => x.Id == customer.Id);
            Customer unchanged = await verificationContext.Customers.SingleAsync(x => x.Id == other.Id);
            updated.Name.Should().Be("Updated André");
            updated.Version.Should().NotBeNullOrEmpty();
            updated.Version.Should().NotEqual(before[customer.Id]);
            customer.Version.Should().Equal(updated.Version);
            unchanged.Version.Should().Equal(before[other.Id]);
        }

        [Test]
        public async Task UpdatesCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            customer.Name = "Updated André";

            // Act
            await service.UpdateEntity(customer);

            // Assert
            Customer? updated = await context.Customers.SingleOrDefaultAsync(x => x.Id == customer.Id);
            updated.Should().NotBeNull();
            updated.Name.Should().Be("Updated André");
        }
    }

    public class UpdateEntities : BaseDatabaseTest
    {
        [Test]
        public async Task ChangesVersions()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer[] entities = [EntityFactory.CreateCustomer("André", "12345678", "andre@example.com"), EntityFactory.CreateCustomer("Sofie", "87654321", "sofie@example.com")];
            context.Customers.AddRange(entities);
            await context.SaveChangesAsync();

            await using RepairDatabaseContext beforeContext = CreateContext();
            var before = await beforeContext.Customers.ToDictionaryAsync(x => x.Id, x => x.Version.ToArray());
            var service = new CustomerQueryService(context);
            foreach (Customer item in entities)
                item.Name += " Updated";

            // Act
            await service.UpdateEntities(entities);

            // Assert
            await using RepairDatabaseContext verificationContext = CreateContext();
            var updated = await verificationContext.Customers.ToListAsync();
            updated.Should().HaveCount(2);
            foreach (Customer item in updated)
            {
                item.Version.Should().NotBeNullOrEmpty();
                item.Version.Should().NotEqual(before[item.Id]);
                entities.Single(x => x.Id == item.Id).Version.Should().Equal(item.Version);
            }
        }

        [Test]
        public async Task UpdatesCustomers()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer[] customers =
            [
                EntityFactory.CreateCustomer("André", "11111111", "one@example.com"),
                EntityFactory.CreateCustomer("Sofie", "22222222", "two@example.com"),
            ];

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);
            customers[0].Name = "Updated André";
            customers[1].Name = "Updated Sofie";

            // Act
            await service.UpdateEntities(customers);

            // Assert
            var updated = await context.Customers.OrderBy(x => x.Email).ToListAsync();
            updated.Select(x => x.Name).Should().BeEquivalentTo("Updated André", "Updated Sofie");
        }
    }

    public class DeleteEntity : BaseDatabaseTest
    {
        [Test]
        public async Task RemovesCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);

            // Act
            await service.DeleteEntity(new SearchableCustomer {Email = customer.Email,});

            // Assert
            Customer? deleted = await context.Customers.SingleOrDefaultAsync(x => x.Id == customer.Id);
            deleted.Should().BeNull();
        }
    }

    public class DeleteEntityById : BaseDatabaseTest
    {
        [Test]
        public async Task RemovesCustomer()
        {
            // Arrange
            await using RepairDatabaseContext context = CreateContext();
            Customer customer = EntityFactory.CreateCustomer("André", "12345678", "andre@example.com");

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerQueryService(context);

            // Act
            await service.DeleteEntityById(customer.Id);

            // Assert
            Customer? deleted = await context.Customers.SingleOrDefaultAsync(x => x.Id == customer.Id);
            deleted.Should().BeNull();
        }
    }
}
