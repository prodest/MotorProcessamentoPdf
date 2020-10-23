using System.Collections.Generic;

namespace Business.Core.ICore
{
    public interface IExtracaoCore
    {
        string ExtrairTextoConcatenado(byte[] arquivoBytes, IEnumerable<int> paginas);
        List<KeyValuePair<int, string>> ExtrairTextoPorPaginas(byte[] arquivoBytes, IEnumerable<int> paginas);
    }
}
