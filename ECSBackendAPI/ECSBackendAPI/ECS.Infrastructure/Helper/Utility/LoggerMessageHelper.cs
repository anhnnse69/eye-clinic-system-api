namespace ECS.Infrastructure.Helper.Utility
{
    /// <summary>
    /// Logger message helper methods for formatting log messages.
    /// </summary>
    public static class LoggerMessageHelper
    {
        /// <summary>
        /// Message formater for general logs.
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static string MessageAPIFormater(string messageCode, string details)
        {
            return $"API log\nSystem message: {messageCode}\nDetails: {details}";
        }
    }
}
