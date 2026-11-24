using Repair.Abstractions.Persistence;

namespace Repair.Abstractions.Entity.Metadata;

public interface IEntityMetadata : ISearchable, IVersioned
{
    DateTime CreatedDateTime { get; set; }

    DateTime UpdatedDateTime { get; set; }
}
