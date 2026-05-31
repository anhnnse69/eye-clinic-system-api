namespace ECS.Domain.Entities.General.Interfaces;

public interface IEntityBase<T>
{
    T Id { get; set; }
}
