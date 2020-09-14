using System;

namespace Business.Core.ICore
{
    public interface ICarimboCore
    {
        byte[] CopiaProcesso(byte[] arquivo, string protocolo, string geradoPor, DateTime dataHora, int totalPaginas, int paginaInicial = 1);
        byte[] Documento(byte[] arquivo, string registro, int natureza, int valorLegal, DateTime dataHora);
        byte[] AdicionarTokenEdocs(byte[] arquivo, string registro);
        bool ValidarDocumentoDuplicado(byte[] arquivo);
    }
}
