using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface ICarimboCore
    {
        byte[] AdicionarMarcaDagua(byte[] arquivo, string[] texto, int tamanhoFonte = 40, string corHexa = "ff0000", int anguloTextoGraus = 30, float opacidade = 0.1f, int repeticoes = 3);
        byte[] CopiaProcesso(byte[] arquivo, string protocolo, string geradoPor, DateTime dataHora, int totalDocumentos, int documentoIncial, int totalPaginas, int paginaInicial);
        byte[] Documento(byte[] arquivo, string registro, int natureza, int valorLegal, DateTime dataHora);
        byte[] AdicionarTokenEdocs(byte[] arquivo, string registro);
        
        Task<string> BuscarExpressoesRegulares(string url, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas);
        string BuscarExpressoesRegulares(byte[] arquivo, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas);
        string BuscarExpressoesRegulares(MemoryStream memoryStream, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas);
    }
}
