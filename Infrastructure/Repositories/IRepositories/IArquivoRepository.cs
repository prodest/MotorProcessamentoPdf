using System.Threading.Tasks;

namespace Infrastructure.Repositories.IRepositories
{
    public interface IArquivoRepository
    {
        Task<byte[]> GetDocumentoCapturadoAsync(string objectName);
    }
}
