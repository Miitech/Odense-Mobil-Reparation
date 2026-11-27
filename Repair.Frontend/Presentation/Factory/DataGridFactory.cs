using System.Collections.ObjectModel;
using Repair.Abstractions.Entity.Metadata;
using System.Collections;
using System.ComponentModel;
using CommunityToolkit.WinUI.UI.Controls;
using Repair.Frontend.Extensions;
using Repair.Models.Extensions;

namespace Repair.Frontend.Presentation.Factory;

internal static class DataGridFactory
{
    private static readonly HashSet<DataGrid> updatingGrids = [];

    public static bool IsUpdatingSelection(this DataGrid grid)
    {
        return updatingGrids.Contains(grid);
    }

    public static void UpdateSelectionSafely(this DataGrid grid, Action update)
    {
        bool ownsUpdate = updatingGrids.Add(grid);
        try
        {
            update();
        }
        finally
        {
            if (ownsUpdate)
            {
                updatingGrids.Remove(grid);
            }
        }
    }

    private static DataGrid Create<TColumn>(
        IEnumerable<object> itemsSource, Func<TColumn, string> getBindingPath,
        Func<TColumn, IValueConverter?>? getColumnConverter = null, Action<DataGrid>? sortItems = null)
        where TColumn : struct, Enum
    {
        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            ItemsSource = itemsSource,
            CanUserSortColumns = true,
            Foreground = new SolidColorBrush(Colors.Black),
        };

        dataGrid.Sorting += (sender, args) => SortColumnClicked(sender, args, sortItems);

        foreach (TColumn column in Enum.GetValues<TColumn>())
        {
            string header = column.ToColumnHeader();
            string bindingPath = getBindingPath(column);
            IValueConverter? converter = getColumnConverter?.Invoke(column);

            dataGrid.Columns.Add(CreateTextColumn(header, bindingPath, converter));
        }

        return dataGrid;
    }

    public static DataGrid Create<TColumn, TItem>(
        ObservableCollection<TItem> itemsSource, Func<TColumn, string> getBindingPath,
        Func<TColumn, IValueConverter?>? getColumnConverter = null)
        where TColumn : struct, Enum where TItem : class, IEntityMetadata
    {
        return Create(itemsSource, getBindingPath, getColumnConverter, grid => SortItems(grid, itemsSource));
    }

    private static void SortItems<T>(DataGrid grid, ObservableCollection<T> items)
        where T : class, IEntityMetadata
    {
        object? selectedItem = grid.SelectedItem;
        List<T> sorted = grid.ApplyCurrentSort(items).ToList();
        grid.UpdateSelectionSafely(() =>
        {
            items.SynchronizeItems(sorted);
            grid.SelectedItem = selectedItem;
        });
    }

    private static void SortColumnClicked(object? sender, DataGridColumnEventArgs e, Action<DataGrid>? sortItems)
    {
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        e.Column.SortDirection = e.Column.SortDirection switch
        {
            null => DataGridSortDirection.Ascending,
            DataGridSortDirection.Ascending => DataGridSortDirection.Descending,
            var _ => null,
        };

        foreach (DataGridColumn column in dataGrid.Columns)
        {
            if (column != e.Column)
            {
                column.SortDirection = null;
            }
        }

        if (sortItems is null)
        {
            dataGrid.ApplyCurrentSort();
            return;
        }

        sortItems(dataGrid);
    }

    private static void ApplyCurrentSort(this DataGrid dataGrid)
    {
        if (dataGrid.ItemsSource is not IEnumerable<object> items || dataGrid.ItemsSource is not IList list)
        {
            return;
        }

        List<object> sortedItems = dataGrid.ApplyCurrentSort(items).ToList();

        list.ReplaceItems(sortedItems);
    }

    public static IEnumerable<T> ApplyCurrentSort<T>(this DataGrid dataGrid, IEnumerable<T> items)
    {
        DataGridColumn? sortedColumn = dataGrid.Columns.FirstOrDefault(x => x.SortDirection is not null);

        if (sortedColumn?.Tag is not string bindingPath || sortedColumn.SortDirection is null)
        {
            return items.OrderBy(x => (x as IEntityMetadata)?.Id);
        }

        return sortedColumn.SortDirection switch
        {
            DataGridSortDirection.Ascending => items.OrderBy(x => GetPropertyValue(x!, bindingPath)),

            DataGridSortDirection.Descending => items.OrderByDescending(x => GetPropertyValue(x!, bindingPath)),

            var _ => items,
        };
    }

    private static object? GetPropertyValue(object item, string propertyPath)
    {
        object? currentValue = item;

        foreach (string propertyName in propertyPath.Split('.'))
        {
            if (currentValue is null)
            {
                return null;
            }

            currentValue = currentValue.GetType().GetProperty(propertyName)?.GetValue(currentValue);
        }

        return currentValue;
    }

    private static DataGridTextColumn CreateTextColumn(
        string header, string bindingPath, IValueConverter? converter = null)
    {
        return new DataGridTextColumn
        {
            Header = header,
            MaxWidth = 500,
            CanUserSort = true,
            Tag = bindingPath,
            Binding = new Binding
            {
                Path = new PropertyPath(bindingPath),
                Converter = converter,
            },
        };
    }

    public static void Refresh(this DataGrid dataGrid)
    {
        IEnumerable? source = dataGrid.ItemsSource;

        dataGrid.ItemsSource = null;
        dataGrid.ItemsSource = source;
    }
}
