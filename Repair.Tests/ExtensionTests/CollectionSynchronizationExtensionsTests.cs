using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using FluentAssertions;
using Repair.Models.Dto.Get;
using Repair.Models.Extensions;
using Repair.Tests.Shared.Factory;

namespace Repair.Tests.ExtensionTests;

public class CollectionSynchronizationExtensionsTests
{
    public class HasSameVersion
    {
        [Test]
        public void WithEqualVersionContents_ReturnsTrue()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(1, 1);

            // Act
            bool result = current.HasSameVersion(incoming);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void WithDifferentVersion_ReturnsFalse()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(1, 2);

            // Act
            bool result = current.HasSameVersion(incoming);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void WithDifferentId_ReturnsFalse()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(2, 1);

            // Act
            bool result = current.HasSameVersion(incoming);

            // Assert
            result.Should().BeFalse();
        }

    }

    public class SynchronizeItems
    {
        [Test]
        public void WithUnchangedCustomer_RetainsReferenceWithoutNotifications()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            ObservableCollection<GetCustomerDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(1, 1);

            // Act
            collection.SynchronizeItems([incoming]);

            // Assert
            collection.Single().Should().BeSameAs(current);
            changes.Should().BeEmpty();
        }

        [Test]
        public void WithChangedNameAndVersion_ReplacesCustomer()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            ObservableCollection<GetCustomerDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(1, 2, "Updated Customer");

            // Act
            collection.SynchronizeItems([incoming]);

            // Assert
            collection.Single().Should().BeSameAs(incoming);
            collection.Single().Name.Should().Be("Updated Customer");
            changes.Should().ContainSingle();
            changes[0].Action.Should().Be(NotifyCollectionChangedAction.Replace);
            changes[0].OldItems![0].Should().BeSameAs(current);
            changes[0].NewItems![0].Should().BeSameAs(incoming);
        }

        [Test]
        public void WithChangedNameButSameVersion_RetainsOriginalCustomer()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            ObservableCollection<GetCustomerDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(1, 1, "Updated Customer");

            // Act
            collection.SynchronizeItems([incoming]);

            // Assert
            collection.Single().Should().BeSameAs(current);
            changes.Should().BeEmpty();
        }

        [Test]
        public void WithFilteredModifiedCustomer_RemovesOtherCustomersAndReplacesMatch()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1, "Original Customer");
            ObservableCollection<GetCustomerDto> collection = new()
            {
                current, DtoFactory.CreateGetCustomer(2, 1), DtoFactory.CreateGetCustomer(3, 1),
            };
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(1, 2, "Updated Customer");
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([incoming]);

            // Assert
            collection.Single().Should().BeSameAs(incoming);
            collection.Single().Name.Should().Be("Updated Customer");
            changes.Select(x => x.Action).Should().BeEquivalentTo<NotifyCollectionChangedAction>([
                NotifyCollectionChangedAction.Remove,
                NotifyCollectionChangedAction.Remove,
                NotifyCollectionChangedAction.Replace,
            ]);
        }

        [Test]
        public void WithNewCustomer_InsertsOnlyNewCustomer()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            ObservableCollection<GetCustomerDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);
            GetCustomerDto added = DtoFactory.CreateGetCustomer(2, 1);

            // Act
            collection.SynchronizeItems([DtoFactory.CreateGetCustomer(1, 1), added,]);

            // Assert
            collection[0].Should().BeSameAs(current);
            collection[1].Should().BeSameAs(added);
            changes.Select(x => x.Action).Should().Equal(NotifyCollectionChangedAction.Add);
        }

        [Test]
        public void WithMissingCustomer_RemovesOnlyMissingCustomer()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            ObservableCollection<GetCustomerDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);
            GetCustomerDto retained = DtoFactory.CreateGetCustomer(2, 1);
            collection.Add(retained);
            changes.Clear();

            // Act
            collection.SynchronizeItems([DtoFactory.CreateGetCustomer(2, 1),]);

            // Assert
            collection.Single().Should().BeSameAs(retained);
            changes.Select(x => x.Action).Should().Equal(NotifyCollectionChangedAction.Remove);
        }

        [Test]
        public void WithReorderedCustomers_MovesExistingReferences()
        {
            // Arrange
            GetCustomerDto first = DtoFactory.CreateGetCustomer(1, 1);
            GetCustomerDto second = DtoFactory.CreateGetCustomer(2, 1);
            ObservableCollection<GetCustomerDto> collection = new() {first, second,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([DtoFactory.CreateGetCustomer(2, 1), DtoFactory.CreateGetCustomer(1, 1),]);

            // Assert
            collection[0].Should().BeSameAs(second);
            collection[1].Should().BeSameAs(first);
            changes.Select(x => x.Action).Should().Equal(NotifyCollectionChangedAction.Move);
        }

        [Test]
        public void WithModifiedAndReorderedCustomer_MovesAndReplaces()
        {
            // Arrange
            GetCustomerDto first = DtoFactory.CreateGetCustomer(1, 1);
            GetCustomerDto second = DtoFactory.CreateGetCustomer(2, 1);
            GetCustomerDto updated = DtoFactory.CreateGetCustomer(2, 2);
            ObservableCollection<GetCustomerDto> collection = new() {first, second,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([updated, DtoFactory.CreateGetCustomer(1, 1),]);

            // Assert
            collection[0].Should().BeSameAs(updated);
            collection[1].Should().BeSameAs(first);
            changes.Select(x => x.Action).Should().BeEquivalentTo<NotifyCollectionChangedAction>([
                NotifyCollectionChangedAction.Move,
                NotifyCollectionChangedAction.Replace,
            ]);
        }

        [Test]
        public void WithEmptyIncomingCollection_RemovesAllWithoutReset()
        {
            // Arrange
            GetCustomerDto current = DtoFactory.CreateGetCustomer(1, 1);
            ObservableCollection<GetCustomerDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([]);

            // Assert
            collection.Should().BeEmpty();
            changes.Select(x => x.Action).Should().Equal(NotifyCollectionChangedAction.Remove);
        }

        [Test]
        public void WithEmptyExistingCollection_InsertsIncomingCustomers()
        {
            // Arrange
            ObservableCollection<GetCustomerDto> collection = new();
            GetCustomerDto incoming = DtoFactory.CreateGetCustomer(1, 1);
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([incoming]);

            // Assert
            collection.Single().Should().BeSameAs(incoming);
            changes.Select(x => x.Action).Should().Equal(NotifyCollectionChangedAction.Add);
        }

        [Test]
        public void WithMixedChanges_SynchronizesWithoutReset()
        {
            // Arrange
            GetCustomerDto retained = DtoFactory.CreateGetCustomer(1, 1);
            GetCustomerDto updated = DtoFactory.CreateGetCustomer(2, 2);
            GetCustomerDto added = DtoFactory.CreateGetCustomer(4, 1);
            ObservableCollection<GetCustomerDto> collection = new()
            {
                retained, DtoFactory.CreateGetCustomer(2, 1), DtoFactory.CreateGetCustomer(3, 1),
            };
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([updated, added, DtoFactory.CreateGetCustomer(1, 1),]);

            // Assert
            collection.Should().HaveCount(3);
            collection[0].Should().BeSameAs(updated);
            collection[1].Should().BeSameAs(added);
            collection[2].Should().BeSameAs(retained);
            changes.Select(x => x.Action).Should().BeEquivalentTo<NotifyCollectionChangedAction>([
                NotifyCollectionChangedAction.Remove,
                NotifyCollectionChangedAction.Move,
                NotifyCollectionChangedAction.Replace,
                NotifyCollectionChangedAction.Add,
            ]);
        }

        [Test]
        public void WithModifiedOrder_ReplacesOrder()
        {
            // Arrange
            GetOrderDto current = DtoFactory.CreateGetOrder(1, 1, DtoFactory.CreateGetCustomer(1, 1));
            GetOrderDto incoming = DtoFactory.CreateGetOrder(1, 2, DtoFactory.CreateGetCustomer(1, 1));
            ObservableCollection<GetOrderDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([incoming], (existingOrder, incomingOrder) =>
                existingOrder.Customer.HasSameVersion(incomingOrder.Customer));

            // Assert
            collection.Single().Should().BeSameAs(incoming);
            changes.Select(x => x.Action).Should().Equal(NotifyCollectionChangedAction.Replace);
        }

        [Test]
        public void WithModifiedNestedCustomer_ReplacesOrder()
        {
            // Arrange
            GetOrderDto current = DtoFactory.CreateGetOrder(1, 1, DtoFactory.CreateGetCustomer(1, 1));
            GetOrderDto incoming = DtoFactory.CreateGetOrder(1, 1, DtoFactory.CreateGetCustomer(1, 2));
            ObservableCollection<GetOrderDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([incoming], (existingOrder, incomingOrder) =>
                existingOrder.Customer.HasSameVersion(incomingOrder.Customer));

            // Assert
            collection.Single().Should().BeSameAs(incoming);
            changes.Select(x => x.Action).Should().Equal(NotifyCollectionChangedAction.Replace);
        }

        [Test]
        public void WithUnchangedOrderAndCustomer_RetainsOrder()
        {
            // Arrange
            GetOrderDto current = DtoFactory.CreateGetOrder(1, 1, DtoFactory.CreateGetCustomer(1, 1));
            GetOrderDto incoming = DtoFactory.CreateGetOrder(1, 1, DtoFactory.CreateGetCustomer(1, 1));
            ObservableCollection<GetOrderDto> collection = new() {current,};
            List<NotifyCollectionChangedEventArgs> changes = TrackChanges(collection);

            // Act
            collection.SynchronizeItems([incoming], (existingOrder, incomingOrder) =>
                existingOrder.Customer.HasSameVersion(incomingOrder.Customer));

            // Assert
            collection.Single().Should().BeSameAs(current);
            changes.Should().BeEmpty();
        }

    }

    private static List<NotifyCollectionChangedEventArgs> TrackChanges<T>(ObservableCollection<T> collection)
    {
        List<NotifyCollectionChangedEventArgs> changes = [];
        collection.CollectionChanged += (_, args) => changes.Add(args);
        return changes;
    }
}
