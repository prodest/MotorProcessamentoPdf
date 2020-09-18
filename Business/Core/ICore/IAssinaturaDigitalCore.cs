using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface IAssinaturaDigitalCore
    {
        Task<object> SignatureValidation(byte[] file);
    }
}
