using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateManager.Repositories.Entities.Base;

/// <summary>
/// Common metadata fields to be used on most entities.
/// </summary>
public class EntityBase
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Table Member configurations for EntityBase.
/// These configurations are optional, if we don't need to handle the dates at the service layer.
/// </summary>
/// <typeparam name="T">The entity type that inherits <see cref="EntityBase"/></typeparam>
[ExcludeFromCodeCoverage(Justification ="Not being used yet. Remove this when becomes used.")]
public abstract class EntityBaseConfiguration<T>
    where T : EntityBase
{
    protected void ConfigureBase(EntityTypeBuilder<T> builder)
        
    {
        builder
            .Property(x => x.CreatedAt)
            .ValueGeneratedOnAdd();

        builder
            .Property(x => x.UpdatedAt)
            .ValueGeneratedOnUpdate();
    }
}