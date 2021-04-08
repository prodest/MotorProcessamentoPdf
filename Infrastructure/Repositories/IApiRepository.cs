using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public interface IApiRepository
    {
        Task<string> OnlineChainValidationAsync(byte[] certificateBytes, bool ignoreExpired);
        Task<byte[]> GetAndReadAsByteArrayAsync(string url);
    }
}
