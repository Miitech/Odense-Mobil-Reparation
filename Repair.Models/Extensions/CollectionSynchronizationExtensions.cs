using System.Collections.ObjectModel;
using Repair.Abstractions.Entity.Metadata;

namespace Repair.Models.Extensions;

public static class CollectionSynchronizationExtensions
{
    public static void SynchronizeItems<T>(
        this ObservableCollection<T> collection, IEnumerable<T> items, Func<T, T, bool>? additionalMatch = null)
        where T : class, IEntityMetadata
    {
        List<T> incoming = items.ToList();
        Dictionary<int, T> existing = collection.ToDictionary(item => item.Id);
        Dictionary<int, T> incomingById = incoming.ToDictionary(item => item.Id);

        List<T> desired = incoming.Select(item =>
            existing.TryGetValue(item.Id, out T? current) && HasSameVersion(current, item) &&
            (additionalMatch?.Invoke(current, item) ?? true)
                ? current
                : item).ToList();

        RemoveMissingItems(collection, incomingById);
        ApplyDesiredOrder(collection, desired, existing);
    }

    public static bool HasSameVersion(this IEntityMetadata current, IEntityMetadata incoming)
    {
        return current.Id == incoming.Id && current.Version.SequenceEqual(incoming.Version);
    }

    private static void RemoveMissingItems<T>(ObservableCollection<T> collection, Dictionary<int, T> incomingById)
        where T : IEntityMetadata
    {
        for (int index = collection.Count - 1; index >= 0; index--)
        {
            if (!incomingById.ContainsKey(collection[index].Id))
            {
                collection.RemoveAt(index);
            }
        }
    }

    private static void ApplyDesiredOrder<T>(
        ObservableCollection<T> collection, List<T> desired, Dictionary<int, T> existing)
        where T : class, IEntityMetadata
    {
        for (var index = 0; index < desired.Count; index++)
        {
            T item = desired[index];

            if (IsItemAlreadyInPosition(collection, item, index))
            {
                continue;
            }

            int existingIndex = FindExistingItemIndex(collection, existing, item, index);

            if (TryInsertMissingItem(collection, item, index, existingIndex))
            {
                continue;
            }

            MoveItemIfRequired(collection, existingIndex, index);
            ReplaceItemIfChanged(collection, item, index);
        }
    }

    private static bool IsItemAlreadyInPosition<T>(ObservableCollection<T> collection, T item, int index)
        where T : class
    {
        return index < collection.Count && ReferenceEquals(collection[index], item);
    }

    private static int FindExistingItemIndex<T>(
        ObservableCollection<T> collection, Dictionary<int, T> existing, T item, int index) where T : IEntityMetadata
    {
        return existing.ContainsKey(item.Id) ? FindIndex(collection, item.Id, index) : -1;
    }

    private static bool TryInsertMissingItem<T>(
        ObservableCollection<T> collection, T item, int index, int existingIndex)
    {
        if (existingIndex >= 0)
        {
            return false;
        }

        collection.Insert(index, item);
        return true;
    }

    private static void MoveItemIfRequired<T>(ObservableCollection<T> collection, int existingIndex, int index)
    {
        if (existingIndex != index)
        {
            collection.Move(existingIndex, index);
        }
    }

    private static void ReplaceItemIfChanged<T>(ObservableCollection<T> collection, T item, int index) where T : class
    {
        if (!ReferenceEquals(collection[index], item))
        {
            collection[index] = item;
        }
    }

    private static int FindIndex<T>(ObservableCollection<T> collection, int id, int startIndex)
        where T : IEntityMetadata
    {
        for (int index = startIndex; index < collection.Count; index++)
        {
            if (collection[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }
}
