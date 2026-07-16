namespace ECS.Infrastructure.CloudStorage
{
    /// <summary>
    /// Abstraction over Cloud Storage provider (Cloudinary) for medical record JSON payloads.
    /// Single source of truth — DB only stores <see cref="Url"/>; the actual form data lives here.
    /// </summary>
    public interface ICloudStorageService
    {
        /// <summary>
        /// Upload JSON content as a private raw resource.
        /// </summary>
        /// <param name="folder">Cloudinary folder (e.g. <c>ecs-medical-records/GLAUCOMA</c>).</param>
        /// <param name="publicId">Public id without extension (e.g. <c>record-id</c>).</param>
        /// <param name="jsonContent">UTF-8 JSON string.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Upload result with URL, public id, size and SHA-256 checksum.</returns>
        Task<CloudUploadResult> UploadJsonAsync(
            string folder,
            string publicId,
            string jsonContent,
            CancellationToken ct = default);

        /// <summary>
        /// Generate a server-signed URL for a private raw resource.
        /// Use TTL = 5 min by default for medical record reads.
        /// </summary>
        /// <param name="publicId">Cloudinary public id of the resource.</param>
        /// <param name="expirySeconds">TTL in seconds.</param>
        /// <returns>Signed URL string.</returns>
        string GetSignedUrl(string publicId, int expirySeconds = 300);

        /// <summary>
        /// Delete a private raw resource.
        /// </summary>
        Task<bool> DeleteAsync(string publicId, CancellationToken ct = default);

        /// <summary>
        /// Fetch the JSON content of a resource and verify its SHA-256 checksum.
        /// </summary>
        Task<CloudFetchResult?> FetchAndVerifyAsync(
            string publicId,
            string expectedSha256,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Result of uploading a JSON payload.
    /// </summary>
    public record CloudUploadResult(
        string Url,
        string PublicId,
        long SizeBytes,
        string Sha256Checksum);

    /// <summary>
    /// Result of fetching a JSON payload.
    /// </summary>
    public record CloudFetchResult(
        string JsonContent,
        long SizeBytes,
        string Sha256Checksum,
        bool IntegrityValid);
}