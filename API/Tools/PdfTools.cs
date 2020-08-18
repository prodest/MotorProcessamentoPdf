using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace API.Tools
{
    public static class PdfTools
    {
        public static async Task<byte[]> ObterArquivo(IFormFile arquivo)
        {
            byte[] arquivoBytes = null;

            //copia o IFormFile para byte array (referencia: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.0 )
            using (var memoryStream = new MemoryStream())
            {
                await arquivo.CopyToAsync(memoryStream);
                arquivoBytes = memoryStream.ToArray();
            }

            return arquivoBytes;
        }

        public static async Task<ICollection<byte[]>> ObterArquivos(IFormFile[] arquivos)
        {
            var arquivosBytes = new List<byte[]>();

            foreach (var arquivo in arquivos)
            {
                if(arquivo.Length > 0)
                {
                    //copia o IFormFile para byte array (referencia: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.0 )
                    using (var memoryStream = new MemoryStream())
                    {
                        await arquivo.CopyToAsync(memoryStream);
                        arquivosBytes.Add(memoryStream.ToArray());
                    }
                }
            }

            return arquivosBytes;
        }

    }
}
