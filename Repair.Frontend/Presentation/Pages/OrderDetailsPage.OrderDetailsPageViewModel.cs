using Repair.Models.Dto.Get;
using Repair.Frontend.Presentation.Core;
using Repair.Frontend.Presentation.Core.Details;
using Repair.Frontend.Presentation.Pieces;

namespace Repair.Frontend.Presentation.Pages;

internal sealed partial class OrderDetailsPage
{
    internal sealed partial class OrderDetailsPageViewModel(OrderDetailsPageArguments arguments)
        : BaseDetailsPageViewModel
    {
        public OrderDetailsPageArguments Arguments { get; } = arguments;

        [ObservableProperty] private GetOrderDto order = null!;

        public OrderEditor OrderEditor { get; set; } = null!;
    }
}
