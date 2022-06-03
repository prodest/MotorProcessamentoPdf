using Business.Models;
using Infrastructure.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public interface IApiRepository
    {
        Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> ValidarAssinaturaDigitalAsync(byte[] arquivo);
        Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> ValidarAssinaturaDigitalAsync(string url);
    }
}
