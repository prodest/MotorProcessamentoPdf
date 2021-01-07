using BusinessItextSharp.Model.CertificadoDigital;
using BusinessItextSharp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessItextSharp.Core
{
    public interface IAssinaturaDigitalCore
    {
        Task<bool> ValidateDigitalSignatures(InputFile inputFile);
        Task<bool> ValidateDigitalSignatures(string url);
        Task<bool> ValidateDigitalSignatures(byte[] file);

        Task<IEnumerable<CertificadoDigital>> SignatureValidation(InputFile inputFile);
        Task<IEnumerable<CertificadoDigital>> SignatureValidation(string url);
        Task<IEnumerable<CertificadoDigital>> SignatureValidation(byte[] file);

        Task<bool> HasDigitalSignature(InputFile inputFile);
        Task<bool> HasDigitalSignature(string url);
        bool HasDigitalSignature(byte[] file);
    }
}
