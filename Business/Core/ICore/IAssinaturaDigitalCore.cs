using System.IO;

namespace Business.Core.ICore
{
    public interface IAssinaturaDigitalCore
    {
        MemoryStream Assinar(MemoryStream arquivo);
    }
}
