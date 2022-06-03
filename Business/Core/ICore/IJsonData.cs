using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface IJsonData
    {
        Task<byte[]> GetAndReadByteArrayAsync(string url);
        Task<T> GetAndReadObjectAsync<T>(string url);
    }
}
