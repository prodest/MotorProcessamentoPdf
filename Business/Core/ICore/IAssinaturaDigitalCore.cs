using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface IAssinaturaDigitalCore
    {
        Task<bool> HasDigitalSignature(InputFile inputFile);
        Task<bool> HasDigitalSignature(string url);
        bool HasDigitalSignature(byte[] file);

        Task<byte[]> AdicionarAssinaturaDigital(string url);
    }
}
