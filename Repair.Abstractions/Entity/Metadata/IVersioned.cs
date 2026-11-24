namespace Repair.Abstractions.Entity.Metadata;

public interface IVersioned
{
    /// <summary>
    /// Represents the version of the entity used for concurrency control.
    /// Allows detection of changes and prevents concurrent updates
    /// from overwriting each other.
    /// </summary>
    byte[] Version { get; set; }
}
