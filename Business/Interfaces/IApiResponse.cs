namespace Business.Interfaces
{
    public interface IApiResponse<T>
    {
        T Data { get; set; }
        string Message { get; set; }
        string StackTrace { get; set; }
        int StatusCode { get; set; }
    }
}