using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.MedicalRecordsServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Request body for creating a medical record.
    /// **Refactored (2026-07-14)**: complex form data is now passed as a single
    /// <see cref="FormData"/> JSON object — backend serializes and uploads to Cloudinary.
    ///
    /// Scope (per user requirement): ONLY "Bệnh Án" + "Khám bệnh" sections.
    /// Administrative / patient-management / diagnosis / discharge sections are omitted.
    /// </summary>
    public class CreateMedicalRecordRequest
    {
        /// <summary>
        /// Appointment ID (GUID string).
        /// </summary>
        public string AppointmentId { get; set; } = string.Empty;

        /// <summary>
        /// Patient ID (GUID string). Could be derived from Appointment but explicit for clarity.
        /// </summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>
        /// Record type discriminator: MS21_TRAUMA | MS22_ANTERIOR | MS23_FUNDUS |
        /// MS24_GLAUCOMA | MS25_STRABISMUS_PTOSIS | MS26_PEDIATRIC.
        /// </summary>
        public string RecordType { get; set; } = string.Empty;

        /// <summary>
        /// Free-form notes from doctor (separate from clinical form).
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// The full medical record form payload.
        /// Server does NOT inspect this object — it serializes to JSON and uploads as-is.
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