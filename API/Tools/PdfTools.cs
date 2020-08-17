using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace API.Tools
{
    public static class PdfTools
    {
        public static async Task<byte[]> ObterArquivo(IFormFile ArquivoAnexo)
        {
            byte[] arquivoDados = null;

            //copia o IFormFile para byte array (referencia: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.0 )
            using (var memoryStream = new MemoryStream())
            {
                await ArquivoAnexo.CopyToAsync(memoryStream);
                arquivoDados = memoryStream.ToArray();
            }

            return arquivoDados;
        }

    }
}
