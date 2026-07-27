using ECS.Application.Services.AuthServices.ViewAccountInfoServices;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable <see cref="ViewAccountInfoRequest"/> instances for unit tests.
    /// </summary>
    public static class ViewAccountInfoRequestMockData
    {
        /// <summary>
        /// Request whose UserId matches the seeded valid active user in <see cref="UserMockData.GetValidActiveUser"/>.
        /// </summary>
        public static ViewAccountInfoRequest GetValidRequest() => new ViewAccountInfoRequest
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        /// <summary>
        /// Request whose UserId is <see cref="Guid.Empty"/> and therefore fails the
        /// <see cref="ViewAccountInfoRequestValidator"/> NotEmpty rule.
        /// </summary>
        public static ViewAccountInfoRequest GetEmptyUserIdRequest() => new ViewAccountInfoRequest
        {
            UserId = Guid.Empty
        };

        /// <summary>
        /// Request whose UserId is non-empty but does not correspond to any user in the
        /// mocked repository. Used to drive the "user not found" response branch.
        /// </summary>
        public static ViewAccountInfoRequest GetUnmatchedUserIdRequest() => new ViewAccountInfoRequest
        {
            UserId = Guid.Parse("99999999-9999-9999-9999-999999999999")
        };
    }
}
