using System;
using System.Collections.Generic;

namespace Business.Core.ICore
{
    public interface ICarimboCore
    {
        byte[] AdicionarMarcaDagua(byte[] arquivo, string[] texto, int tamanhoFonte = 40, string corHexa = "ff0000", int anguloTextoGraus = 30, float opacidade = 0.1f, int repeticoes = 3);
        byte[] CopiaProcesso(byte[] arquivo, string protocolo, string geradoPor, DateTime dataHora, int totalPaginas, int paginaInicial = 1);
        byte[] Documento(byte[] arquivo, string registro, int natureza, int valorLegal, DateTime dataHora);
        byte[] AdicionarTokenEdocs(byte[] arquivo, string registro);
        string ValidarDocumentoDuplicado(byte[] arquivo, IEnumerable<string> regex, IEnumerable<int> paginas);
        string ValidarOcorrenciaExpressaoRegular(byte[] arquivo, IEnumerable<string> regex, IEnumerable<int> paginas);
    }
}
