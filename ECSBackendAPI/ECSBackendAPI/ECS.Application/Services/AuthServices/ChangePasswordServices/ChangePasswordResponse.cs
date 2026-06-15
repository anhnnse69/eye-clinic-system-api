namespace ECS.Application.Services.AuthServices.ChangePasswordServices
{
    /// <summary>
    /// Response object for change password request.
    /// </summary>
    public class ChangePasswordResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
