namespace API.Shared.Models
{
    public class ApiResponse<T> where T : class
    {
        public int StatusCode { get; }
        public string Message { get; }
        public T Data { get; }

        public ApiResponse(int statusCode, string message = null, T data = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
            Data = data;
        }

        private string GetDefaultMessageForStatusCode(int statusCode)
        {
            switch (statusCode)
            {
                case 400:
                    return "Bad request";
                case 401:
                    return "Authorized";
                case 404:
                    return "Resource not found";
                case 500:
                    return "Internal server error";
                default:
                    return null;
            }
        }

    }
}
