using ECS.Application.Common.Response;
using ECS.Domain.Enums;
using ECS.Infrastructure.CloudStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.CustomController
{
    /// <summary>
    /// Upload controller for image files (OCT scans, paraclinical attachments, patient avatars) to Cloudinary.
    /// Uses Cloudinary credentials configured in appsettings.json.
    /// </summary>
    [ApiController]
    [Route("api/v1/upload")]
    [Authorize]
    public class MediaUploadController : ControllerBase
    {
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ILogger<MediaUploadController> _logger;

        public MediaUploadController(
            ICloudStorageService cloudStorageService,
            ILogger<MediaUploadController> logger)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Multipart upload endpoint: <c>file</c> (IFormFile), optional <c>folder</c>.
        /// Uploads the image directly to Cloudinary and returns the secure image URL.
        /// </summary>
        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UploadImageResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UploadImageResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadImage(
            [FromForm] IFormFile? file,
            [FromForm] string? folder,
            CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<UploadImageResponse>.Fail("APP_MESSAGE_4003"));
            }

            // Validate image extension / mime type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                return BadRequest(ApiResponse<UploadImageResponse>.Fail("APP_MESSAGE_4002"));
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var targetFolder = !string.IsNullOrWhiteSpace(folder) ? folder : "ecs-medical-records";
                var uploadResult = await _cloudStorageService.UploadImageAsync(stream, file.FileName, targetFolder, ct);

                var response = new UploadImageResponse
                {
                    Url = uploadResult.Url,
                    PublicId = uploadResult.PublicId,
                    SizeBytes = uploadResult.SizeBytes,
                    FileName = file.FileName
                };

                return Ok(ApiResponse<UploadImageResponse>.Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(),
                    response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image {FileName} to Cloudinary", file.FileName);
                return BadRequest(ApiResponse<string>.Fail(
                    GeneralCode.APP_MESSAGE_5001.ToString(),
                    $"Cloudinary upload error: {ex.Message}"));
            }
        }
    }

    public class UploadImageResponse
    {
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
