using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.Configurations
{
    /// <summary>
    /// Platform configuration key-value store.
    /// </summary>
    public class PlatformConfig : EntityBase<Guid>
    {
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
