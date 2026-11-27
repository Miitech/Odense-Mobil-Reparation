using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Repair.Abstractions.Persistence;

namespace Repair.Persistence.Core;

public abstract class BaseDatabaseContext<TContext> : DbContext where TContext : DbContext
{
    protected BaseDatabaseContext([NotNull] DbContextOptions<TContext> options) : base(options)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateEntityMetadata();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateEntityMetadata();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateEntityMetadata()
    {
        var entries = ChangeTracker.Entries<IEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);
        var now = DateTime.UtcNow;
        var generateVersion = Database.IsSqlite();

        foreach (var entry in entries)
        {
            UpdateDateTime(entry, now);

            if (generateVersion)
            {
                UpdateVersion(entry);
            }
        }
    }

    private void UpdateDateTime(EntityEntry<IEntity> entry, DateTime now)
    {
        entry.Entity.UpdatedDateTime = now;

        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedDateTime = now;
        }
    }

    private void UpdateVersion(EntityEntry<IEntity> entry)
    {
        var version = entry.Property<byte[]>(nameof(IEntity.Version));
        version.CurrentValue = Guid.NewGuid().ToByteArray();

        if (entry.State == EntityState.Modified)
            version.IsModified = true;
    }
}
