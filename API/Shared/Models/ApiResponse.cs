namespace API.Shared.Models
{
    public class ApiResponse
    {
        public int StatusCode { get; }
        public string Message { get; }

        public ApiResponse(int statusCode, string message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
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
