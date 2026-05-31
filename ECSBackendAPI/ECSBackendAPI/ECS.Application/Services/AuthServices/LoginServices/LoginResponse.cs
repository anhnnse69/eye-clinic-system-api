namespace ECS.Application.Services.AuthServices.LoginServices
{
    /// <summary>
    /// Response object for user login.
    /// </summary>
    public class LoginResponse
    {
        // token
        public string? Token { get; set; }

        /// <summary>
        /// Login response constructor
        /// </summary>
        /// <param name="token"></param>
        public LoginResponse(string token)
        {
            Token = token;
        }
    }
}
