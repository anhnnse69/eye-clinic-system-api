using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.Notifications
{
    public class Notification : EntityBase<Guid>
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; } = null!;
    }
}
