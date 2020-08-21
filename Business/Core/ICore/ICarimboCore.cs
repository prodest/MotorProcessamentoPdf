namespace Business.Core.ICore
{
    public interface ICarimboCore
    {
        byte[] CopiaProcesso(byte[] arquivo, string protocolo, string geradoPor, string dataHora, int totalPaginas, int paginaInicial = 1);
        byte[] ValorLegal(byte[] arquivo, string registro, string valorLegal, string dataHora);
    }
}
