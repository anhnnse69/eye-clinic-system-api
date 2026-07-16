namespace ECS.Infrastructure.CloudStorage
{
    /// <summary>
    /// Configuration options for Cloudinary.
    /// Bound from <c>appsettings.json</c> section <c>"Cloudinary"</c>.
    /// </summary>
    public class CloudinaryOptions
    {
        public const string SectionName = "Cloudinary";

        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;

        /// <summary>
        /// Root folder for medical records (e.g. <c>ecs-medical-records</c>).
        /// </summary>
        public string Folder { get; set; } = "ecs-medical-records";

        /// <summary>
        /// TTL (seconds) for signed URLs when reading raw JSON.
        /// </summary>
        public int SignedUrlTtlSeconds { get; set; } = 300;
    }
}