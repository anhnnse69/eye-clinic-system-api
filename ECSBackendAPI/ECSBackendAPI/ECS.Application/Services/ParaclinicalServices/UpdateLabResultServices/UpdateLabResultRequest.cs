using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.ParaclinicalServices.UpdateLabResultServices
{
    public class UpdateLabResultRequest
    {
        public string LabResultId { get; set; } = string.Empty;

        public string? Status { get; set; }
        public string? ClinicalConclusion { get; set; }
        public string? ImageUrl { get; set; }
        public string? TechnicianName { get; set; }
        public string? MachineName { get; set; }
        public string? ScanPattern { get; set; }
        public DateTime? PerformedAt { get; set; }

        /// <summary>
        /// Optional full replacement of modality-specific measurements.
        /// If null/empty, measurements are not modified.
        /// </summary>
        [JsonConverter(typeof(ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices.RawJsonConverter))]
        public JsonElement? Measurements { get; set; }
    }

    public class UpdateLabResultResponse
    {
        public string LabResultId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public bool IsSuccess { get; set; }
    }
}