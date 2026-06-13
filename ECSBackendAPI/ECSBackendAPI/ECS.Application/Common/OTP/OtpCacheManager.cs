namespace ECS.Application.Common.OTP
{
    /// <summary>
    /// Shared OTP data record.
    /// </summary>
    public record OtpData(string Email, string Otp, DateTime ExpiresAt);

    /// <summary>
    /// Shared in-memory OTP cache for both ForgotPassword and ResetPassword services.
    /// </summary>
    public static class OtpCacheManager
    {
        private static readonly Dictionary<string, OtpData> _otpCache = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Stores an OTP with reset token (5-minute expiration).
        /// </summary>
        public static void Store(string resetToken, string email, string otp)
        {
            lock (_lock)
            {
                _otpCache[resetToken] = new OtpData(email, otp, DateTime.UtcNow.AddMinutes(5));
            }
        }

        /// <summary>
        /// Validates the OTP using reset token and returns associated email.
        /// </summary>
        public static (bool IsValid, string? Email, string? ErrorCode) Validate(string resetToken, string otp)
        {
            lock (_lock)
            {
                if (!_otpCache.TryGetValue(resetToken, out var otpData))
                {
                    return (false, null, "4031");
                }
                if (otpData.ExpiresAt < DateTime.UtcNow)
                {
                    _otpCache.Remove(resetToken);
                    return (false, null, "4032");
                }
                if (otpData.Otp != otp)
                {
                    return (false, null, "4031");
                }
                return (true, otpData.Email, null);
            }
        }

        /// <summary>
        /// Removes the OTP entry using reset token.
        /// </summary>
        public static void Remove(string resetToken)
        {
            lock (_lock)
            {
                _otpCache.Remove(resetToken);
            }
        }

        /// <summary>
        /// Clears all OTPs from the cache.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _otpCache.Clear();
            }
        }
    }
}
