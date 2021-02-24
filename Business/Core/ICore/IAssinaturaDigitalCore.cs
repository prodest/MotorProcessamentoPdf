using Infrastructure.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface IAssinaturaDigitalCore
    {
        Task<bool> HasDigitalSignature(InputFile inputFile);
        Task<bool> HasDigitalSignature(string url);
        bool HasDigitalSignature(byte[] file);
        bool HasDigitalSignature(MemoryStream memoryStream);

        Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> SignatureValidation(string url);
        Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> SignatureValidation(byte[] file);

        Task<byte[]> AdicionarAssinaturaDigital(InputFile inputFile);
    }
}
