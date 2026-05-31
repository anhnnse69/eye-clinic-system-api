using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.EyeExaminations
{
    public class LacrimalSystem : EntityBase<Guid>
    {
        public Guid ExamId { get; set; }
        public string Side { get; set; } = null!; // RIGHT/LEFT
        public bool IrrigationFree { get; set; } = true;
        public bool IrrigationRegurgitationSame { get; set; } = false;
        public bool IrrigationRegurgitationOpposite { get; set; } = false;
        public string? IrrigationNote { get; set; }
        public string? OtherNote { get; set; }

        public virtual EyeExamination Examination { get; set; } = null!;
    }
}
