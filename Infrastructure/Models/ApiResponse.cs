namespace Infrastructure.Models
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public ApiResponse()
        {

        }

        public ApiResponse(ApiResponse<object> errorResponse)
        {
            StatusCode = errorResponse.StatusCode;
            Message = errorResponse.Message;
        }

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
