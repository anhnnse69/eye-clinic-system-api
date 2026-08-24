namespace ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices
{
    public class AiSymptomSuggestRequest
    {
        public string? RecordId { get; set; }
        public string? AppointmentId { get; set; }

        public string Symptom { get; set; } = string.Empty;
        public string Duration { get; set; } = "acute";
        public string PainLevel { get; set; } = "low";
        public string EyeRedness { get; set; } = "no";
        public string BlurredVision { get; set; } = "no";
        public string LightSensitivity { get; set; } = "no";
        public string Discharge { get; set; } = "no";
        public string Tearing { get; set; } = "no";
        public string Swelling { get; set; } = "no";
        public string ForeignBodySensation { get; set; } = "no";
        public string Floaters { get; set; } = "no";
        public string Halos { get; set; } = "no";
        public string EyePressure { get; set; } = "normal";
        public string CornealOpacity { get; set; } = "no";
        public string PupilResponse { get; set; } = "normal";
        public string NightBlindness { get; set; } = "no";
        public string DoubleVision { get; set; } = "no";
        public string EyeTurning { get; set; } = "no";
        public string WhiteReflection { get; set; } = "no";
        public string Headache { get; set; } = "no";
        public string Nausea { get; set; } = "no";
        public string Age { get; set; } = "adult";
        public string Diabetes { get; set; } = "no";
        public string Hypertension { get; set; } = "no";
        public string FamilyHistory { get; set; } = "no";
    }
}
