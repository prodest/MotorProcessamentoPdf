using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface IAssinaturaDigitalCore
    {
        Task<byte[]> AdicionarAssinaturaDigital(string url);
    }
}
