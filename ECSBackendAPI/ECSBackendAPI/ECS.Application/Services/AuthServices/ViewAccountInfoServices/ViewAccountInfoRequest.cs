namespace ECS.Application.Services.AuthServices.ViewAccountInfoServices
{
    /// <summary>
    /// Request object for viewing account information.
    /// The user identifier is extracted from the authenticated JWT.
    /// </summary>
    public class ViewAccountInfoRequest
    {
        public Guid UserId { get; set; }
    }
}
