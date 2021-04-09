using Business.Shared.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface ICarimboCore
    {
        Task<byte[]> CarimboLateral(InputFile inputFile, string texto, float tamanhoFonte, Margem margem, string cor, int? paginaInicial, int? totalPaginas);
        byte[] AdicionarMarcaDagua(byte[] arquivo, string[] texto, int tamanhoFonte = 40, string corHexa = "ff0000", int anguloTextoGraus = 30, float opacidade = 0.1f, int repeticoes = 3);
        
        Task<string> BuscarExpressoesRegulares(string url, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas);
        string BuscarExpressoesRegulares(byte[] arquivo, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas);
        string BuscarExpressoesRegulares(MemoryStream memoryStream, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas);
    }
}
