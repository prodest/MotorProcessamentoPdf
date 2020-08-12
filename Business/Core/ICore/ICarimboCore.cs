using System;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface ICarimboCore
    {
        byte[] Copia(byte[] arquivo, string codigoProcesso, string geradoPor, string dataHora, int paginaInicial = 1);
        Task Teste();
    }
}
