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

        //void SignatureValidationV2(byte[] arquivoBytes);

        Task<byte[]> AdicionarAssinaturaDigital(InputFile inputFile, string registroDocumento);
        
        Task<bool> ValidarHashDocumento(InputFile inputFile, string hash);

        Task<ICollection<string>> ObterSignatureFieldName(InputFile inputFile);
        
        Task<byte[]> RemoverAssinaturasDigitais(InputFile inputFile);
    }
}
