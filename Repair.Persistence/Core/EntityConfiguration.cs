using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Repair.Abstractions.Persistence;
using Repair.Persistence.Core.Abstraction;

namespace Repair.Persistence.Core;

public abstract class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity
{
    private readonly DatabaseType type;

    protected EntityConfiguration(DatabaseType type)
    {
        this.type = type;
    }

    /// <inheritdoc />
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        Type type = typeof(TEntity);
        var idName = $"{type.Name}Id";

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName(idName);

        switch (this.type)
        {
            case DatabaseType.SQL_LITE:
                builder.Property(x => x.Version).IsRowVersion()
                    .HasColumnType("BLOB").HasDefaultValueSql("randomblob(16)");

                // SQLite RETURNING does not include changes made by AFTER triggers.
                builder.ToTable(table => table.UseSqlReturningClause(false));

                break;
            case DatabaseType.POSTGRESQL:
                builder.Property(x => x.Version).IsRowVersion();
                break;
        }
    }
}
