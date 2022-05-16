using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface IExtracaoCore
    {
        string ExtrairTextoConcatenado(byte[] arquivoBytes, IEnumerable<int> paginas);
        Task<string> ExtrairTextoConcatenadoLink(string url, IEnumerable<int> paginas);
        List<KeyValuePair<int, string>> ExtrairTextoPorPaginas(byte[] arquivoBytes, IEnumerable<int> paginas);
        Task<List<KeyValuePair<int, string>>> ExtrairTextoPorPaginasLink(string url, IEnumerable<int> paginas);
    }
}
