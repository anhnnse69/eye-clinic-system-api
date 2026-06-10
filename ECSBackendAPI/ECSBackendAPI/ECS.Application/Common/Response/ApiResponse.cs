namespace ECS.Application.Common.Response
{
    public class ApiResponse<T>
    {
        // code_message
        public string CodeMessage { get; init; }

        // data
        public T? Data { get; init; }

        // meta
        public MetaResponse? Meta { get; set; }

        /// <summary>
        /// API response constructor
        /// </summary>
        /// <param name="codeMessage"></param>
        /// <param name="data"></param>
        /// <param name="meta"></param>
        private ApiResponse(string codeMessage, T? data, MetaResponse? meta)
        {
            CodeMessage = codeMessage;
            Data = data;
            Meta = meta;
        }

        /// <summary>
        /// API response success, with data no pagination
        /// </summary>
        /// <param name="codeMessage"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ApiResponse<T> Success(string codeMessage, T data)
            => new ApiResponse<T>(codeMessage, data, null);

        /// <summary>
        /// API response success, with data and pagination
        /// </summary>
        /// <param name="codeMessage"></param>
        /// <param name="data"></param>
        /// <param name="meta"></param>
        /// <returns></returns>
        public static ApiResponse<T> Success(
            string codeMessage,
            T data,
            MetaResponse meta)
            => new ApiResponse<T>(codeMessage, data, meta);

        /// <summary>
        /// API response fail
        /// </summary>
        /// <param name="codeMessage"></param>
        /// <returns></returns>
        public static ApiResponse<T> Fail(string codeMessage)
            => new ApiResponse<T>(codeMessage, default, null);

        /// <summary>
        /// API response fail, with data
        /// </summary>
        /// <param name="codeMessage"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ApiResponse<T> Fail(string codeMessage, T data)
            => new ApiResponse<T>(codeMessage, data, null);
    }
}
