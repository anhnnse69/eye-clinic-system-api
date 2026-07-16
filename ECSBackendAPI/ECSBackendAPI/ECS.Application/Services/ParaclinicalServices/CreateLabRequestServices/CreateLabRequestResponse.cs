namespace ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices
{
    public class CreateLabRequestResponse
    {
        public string LabResultId { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string LabType { get; set; } = string.Empty;
        public string? Side { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public bool IsSuccess { get; set; }
    }
}