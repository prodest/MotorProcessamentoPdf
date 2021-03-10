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
        void SignatureValidationV2(byte[] arquivoBytes);

        Task<byte[]> AdicionarAssinaturaDigital(InputFile inputFile, string registroDocumento);
        
        Task<bool> ValidarHashDocumento(Stream stream, string hashDoBanco);

        Task<ICollection<string>> ObterSignatureFieldName(InputFile inputFile);
    }
}
