using System.Net;
using System.Security.Cryptography;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECS.Infrastructure.CloudStorage
{
    /// <summary>
    /// Cloudinary-backed implementation of <see cref="ICloudStorageService"/>.
    /// Stores medical record JSON as private raw resources; reads use server-signed URLs.
    /// </summary>
    public class CloudinaryStorageService : ICloudStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;
        private readonly ILogger<CloudinaryStorageService> _logger;
        private readonly HttpClient _httpClient;

        public CloudinaryStorageService(
            IOptions<CloudinaryOptions> options,
            ILogger<CloudinaryStorageService> logger,
            IHttpClientFactory? httpClientFactory = null)
        {
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.CloudName) ||
                string.IsNullOrWhiteSpace(_options.ApiKey) ||
                string.IsNullOrWhiteSpace(_options.ApiSecret))
            {
                throw new InvalidOperationException(
                    "Cloudinary configuration is missing. Set Cloudinary:CloudName, Cloudinary:ApiKey, Cloudinary:ApiSecret in appsettings.");
            }

            var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;

            _httpClient = httpClientFactory?.CreateClient(nameof(CloudinaryStorageService)) ?? new HttpClient();
        }

        public async Task<CloudUploadResult> UploadJsonAsync(
            string folder,
            string publicId,
            string jsonContent,
            CancellationToken ct = default)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonContent);
            var checksum = ComputeSha256(bytes);

            using var stream = new MemoryStream(bytes);
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription($"{publicId}.json", stream),
                PublicId = publicId,
                Folder = folder,
                Type = "private",
                Overwrite = false,
                UniqueFilename = false,
                Tags = "medical-record,ecs-v1"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, "private", ct);
            if (uploadResult.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Cloudinary upload failed for {PublicId}: {Error}",
                    publicId, uploadResult.Error?.Message);
                throw new CloudStorageException(
                    $"Cloudinary upload failed: {uploadResult.Error?.Message ?? "unknown error"}");
            }

            _logger.LogInformation(
                "Cloudinary uploaded {PublicId} to {Folder}, size={Size}B",
                publicId, folder, uploadResult.Bytes);

            return new CloudUploadResult(
                Url: uploadResult.SecureUrl?.ToString() ?? string.Empty,
                PublicId: uploadResult.PublicId,
                SizeBytes: uploadResult.Bytes,
                Sha256Checksum: checksum);
        }

        public string GetSignedUrl(string publicId, int expirySeconds = 300)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("publicId is required", nameof(publicId));

            var expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expirySeconds;
            var url = _cloudinary.DownloadPrivate(
                publicId,
                attachment: false,
                format: "json",
                type: "private",
                expiresAt: expiresAt,
                resourceType: "raw");
            return url;
        }

        public async Task<bool> DeleteAsync(string publicId, CancellationToken ct = default)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId) { Type = "private" };
                var result = await _cloudinary.DestroyAsync(deleteParams);
                var success = result.Result == "ok";
                if (!success)
                {
                    _logger.LogWarning("Cloudinary delete {PublicId} returned: {Result}",
                        publicId, result.Result);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary delete failed for {PublicId}", publicId);
                return false;
            }
        }

        public async Task<CloudUploadResult> UploadImageAsync(
            Stream stream,
            string fileName,
            string? folder = null,
            CancellationToken ct = default)
        {
            var targetFolder = !string.IsNullOrEmpty(folder) ? folder : _options.Folder;
            var cleanFileName = Path.GetFileNameWithoutExtension(fileName);
            var publicId = $"{Guid.NewGuid():N}_{cleanFileName}";

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = targetFolder,
                PublicId = publicId,
                Overwrite = true,
                UniqueFilename = false,
                Tags = "ecs-image,paraclinical"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);
            if (uploadResult.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Cloudinary image upload failed for {PublicId}: {Error}",
                    publicId, uploadResult.Error?.Message);
                throw new CloudStorageException(
                    $"Cloudinary image upload failed: {uploadResult.Error?.Message ?? "unknown error"}");
            }

            _logger.LogInformation(
                "Cloudinary uploaded image {PublicId} to {Folder}, size={Size}B",
                publicId, targetFolder, uploadResult.Bytes);

            return new CloudUploadResult(
                Url: uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
                PublicId: uploadResult.PublicId,
                SizeBytes: uploadResult.Bytes,
                Sha256Checksum: string.Empty);
        }

        public async Task<CloudFetchResult?> FetchAndVerifyAsync(
            string publicId,
            string expectedSha256,
            CancellationToken ct = default)
        {
            try
            {
                var signedUrl = GetSignedUrl(publicId, _options.SignedUrlTtlSeconds);
                var bytes = await _httpClient.GetByteArrayAsync(signedUrl, ct);
                var checksum = ComputeSha256(bytes);
                var valid = string.Equals(checksum, expectedSha256, StringComparison.OrdinalIgnoreCase);
                var content = Encoding.UTF8.GetString(bytes);

                if (!valid)
                {
                    _logger.LogError(
                        "Cloudinary integrity check FAILED for {PublicId}: expected={Expected}, actual={Actual}",
                        publicId, expectedSha256, checksum);
                }

                return new CloudFetchResult(content, bytes.LongLength, checksum, valid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary fetch failed for {PublicId}", publicId);
                return null;
            }
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    public class CloudStorageException : Exception
    {
        public CloudStorageException(string message) : base(message) { }
        public CloudStorageException(string message, Exception inner) : base(message, inner) { }
    }
}