namespace ECS.Application.Services.AuthServices.UpdatePersonalProfileServices
{
    /// <summary>
    /// Response model returning data summary post execution.
    /// </summary>
    public class UpdatePersonalProfileResponse
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
    }
}
