using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Business.Shared.Models
{
    public class InputFile
    {
        public string FileUrl { get; set; }
        public IFormFile FileBytes { get; set; }

        public async Task<byte[]> GetByteArray()
        {
            byte[] arquivoBytes = null;

            // Copia o IFormFile para byte array (referencia: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.0 )
            using (var memoryStream = new MemoryStream())
            {
                await FileBytes.CopyToAsync(memoryStream);
                arquivoBytes = memoryStream.ToArray();
                memoryStream.Close();
            }

            return arquivoBytes;
        }

        #region Validations

        public async Task IsValidAsync()
        {
            BothFieldsFilled();
            if(FileBytes == null)
                IsUrlFilled();
            if(FileUrl == null)
                await ArquivoValidoAsync();
        }

        private async Task ArquivoValidoAsync()
        {
            byte[] arquivoBytes = await GetByteArray();

            if (arquivoBytes.Length <= 0)
                throw new Exception("Arquivo vazio ou corrompido.");
        }

        private void IsUrlFilled()
        {
            if (string.IsNullOrWhiteSpace(FileUrl))
                throw new ArgumentException("A Url informada está vazia ou nula");
        }

        private void BothFieldsFilled()
        {
            if (FileBytes != null && FileUrl != null)
                throw new ArgumentException("Você deve enviar à API o arquivo OU o url do arquivo.");
        }
        
        #endregion
    }
}
