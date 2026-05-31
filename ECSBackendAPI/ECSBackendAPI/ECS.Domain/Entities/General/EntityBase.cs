using ECS.Domain.Entities.General.Interfaces;

namespace ECS.Domain.Entities.General;

public abstract class EntityBase<TKey> : IEntityBase<TKey>
{
    public TKey Id { get; set; } = default!;
}
