using Business.Core.ICore;
using Business.Helpers.AssinaturaDigital;
using System.Threading.Tasks;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        public async Task<byte[]> Signaturevalidation(byte[] file)
        {
            await AssinaturaDigitalHelper.ValidateDigitalSignatures(file);
            return file;
        }
    }
}
