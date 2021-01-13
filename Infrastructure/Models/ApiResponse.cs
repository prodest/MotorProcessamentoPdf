namespace Infrastructure.Models
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; }
        public string Message { get; }
        public T Data { get; }

        public ApiResponse(int statusCode, string message = default, T data = default)
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
