using System.Text.Json.Serialization;

namespace ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices
{
    /// <summary>
    /// Request body for creating a paraclinical / lab request (UC42-44).
    /// The <c>LabType</c> field controls how the free-form payload is interpreted
    /// (OCT, VisualField, Ultrasound, or general lab).
    /// </summary>
    public class CreateLabRequestRequest
    {
        /// <summary>
        /// Parent medical record ID (FK → <c>MedicalRecord.Id</c>).
        /// </summary>
        public string RecordId { get; set; } = string.Empty;

        /// <summary>
        /// Lab modality: OCT | VISUAL_FIELD | ULTRASOUND | GENERAL_LAB.
        /// </summary>
        public string LabType { get; set; } = string.Empty;

        /// <summary>
        /// Eye side: OD | OS | BOTH (nullable for non-eye modalities).
        /// </summary>
        public string? Side { get; set; }

        /// <summary>
        /// ICD-10 / clinical indication that motivates the request.
        /// </summary>
        public string? Indication { get; set; }

        /// <summary>
        /// Technician ID (optional, for auditing).
        /// </summary>
        public string? TechnicianId { get; set; }

        /// <summary>
        /// Technician display name.
        /// </summary>
        public string? TechnicianName { get; set; }

        /// <summary>
        /// Machine / instrument name.
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// Scan pattern (modality-specific).
        /// </summary>
        public string? ScanPattern { get; set; }

        /// <summary>
        /// URL of the source image (OCT B-scan, fundus photo, etc.).
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Free-form clinical conclusion (the doctor's written summary).
        /// </summary>
        public string? ClinicalConclusion { get; set; }

        /// <summary>
        /// Modality-specific measurements passed as raw JSON.
        /// Examples:
        ///   OCT:         rnflAverageOd, rnflAverageOs, cmtOd, cmtOs, cupDiscRatioOd, cupDiscRatioOs
        ///   VisualField: mdValue, psdValue, vfiPercent, reliable, strategy
        ///   Ultrasound:  axialLengthMm, acDepthMm, lensThicknessMm, vitreousLengthMm
        ///                 + lensStatus, retinaStatus
        /// </summary>
        [JsonConverter(typeof(RawJsonConverter))]
        public System.Text.Json.JsonElement Measurements { get; set; }

        /// <summary>
        /// Optional initial status (defaults to REQUESTED).
        /// REQUESTED | IN_PROGRESS | COMPLETED | CANCELLED.
        /// </summary>
        public string? Status { get; set; }
    }

    /// <summary>
    /// Pass-through converter so ASP.NET model binding keeps the JSON subtree as a
    /// <see cref="System.Text.Json.JsonElement"/> rather than trying to deserialize
    /// into a typed object.
    /// </summary>
    public class RawJsonConverter : JsonConverter<System.Text.Json.JsonElement>
    {
        public override System.Text.Json.JsonElement Read(
            ref System.Text.Json.Utf8JsonReader reader,
            Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options)
            => System.Text.Json.JsonDocument.ParseValue(ref reader).RootElement.Clone();

        public override void Write(
            System.Text.Json.Utf8JsonWriter writer,
            System.Text.Json.JsonElement value,
            System.Text.Json.JsonSerializerOptions options)
            => value.WriteTo(writer);
    }
}