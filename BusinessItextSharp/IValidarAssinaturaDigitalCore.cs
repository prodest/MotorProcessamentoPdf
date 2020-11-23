using Business.Helpers.AssinaturaDigital;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessItextSharp
{
    public interface IValidarAssinaturaDigitalCore
    {
        Task<IEnumerable<CertificadoDigital>> SignatureValidation(string url);
        Task<IEnumerable<CertificadoDigital>> SignatureValidation(byte[] file);
        bool HasDigitalSignature(byte[] file);
        Task<bool> HasDigitalSignature(string url);
    }
}
