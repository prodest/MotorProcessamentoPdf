using BusinessItextSharp.Models;
using Infrastructure.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessItextSharp.Core
{
    public interface IAssinaturaDigitalCore
    {
        Task<bool> ValidateDigitalSignatures(InputFile inputFile);
        Task<bool> ValidateDigitalSignatures(string url);
        Task<bool> ValidateDigitalSignatures(byte[] file);

        Task<IEnumerable<CertificadoDigitalDto>> SignatureValidation(InputFile inputFile, bool ignoreExpired = false);

        Task<bool> HasDigitalSignature(InputFile inputFile);
        Task<bool> HasDigitalSignature(string url);
        bool HasDigitalSignature(byte[] file);

        CertificadoDigitalDto ObterInformacoesCertificadoDigital();
    }
}
