using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Request body for updating a medical record.
    /// **Refactored (2026-07-20)**: follows the same MongoDB-backed JSON envelope
    /// pattern as CreateMedicalRecordRequest. The full form payload is passed as a
    /// single <see cref="FormData"/> JSON object — backend serializes and updates
    /// the MongoDB document.
    ///
    /// Scope: "Bệnh Án" + "Khám bệnh" sections only (matching Create flow).
    /// Administrative / diagnosis / discharge sections are omitted.
    /// </summary>
    public class UpdateMedicalRecordRequest
    {
        /// <summary>
        /// Free-form notes from doctor (separate from clinical form).
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Professional reason and justification for updating the medical record (submitted to Clinic Admin).
        /// </summary>
        public string? EditReason { get; set; }

        /// <summary>
        /// License or formal permission document reference for editing the medical record (submitted to Clinic Admin).
        /// </summary>
        public string? EditPermissionDocument { get; set; }

        /// <summary>
        /// The full medical record form payload.
        /// Server does NOT inspect this object — it serializes to JSON and updates MongoDB as-is.
        /// Validate the schema on the FE side using zod before sending.
        /// </summary>
        [JsonConverter(typeof(RawJsonConverter))]
        public JsonElement FormData { get; set; }
    }

    /// <summary>
    /// Pass-through converter so ASP.NET model binding keeps the JSON subtree as a
    /// <see cref="JsonElement"/> rather than trying to deserialize into a typed object.
    /// </summary>
    public class RawJsonConverter : JsonConverter<JsonElement>
    {
        public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonDocument.ParseValue(ref reader).RootElement.Clone();

        public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
            => value.WriteTo(writer);
    }
}
