using System;

namespace Business.Shared.Models
{
    public class InputFile
    {
        public string FileUrl { get; set; }
        public byte[] FileBytes { get; set; }

        public void IsValid()
        {
            BothFieldsFilled();
            if (FileBytes == null)
                IsUrlFilled();
            if (FileUrl == null)
                ArquivoValido();
        }

        #region Private Methods

        private void ArquivoValido()
        {
            if (FileBytes.Length <= 0)
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
                throw new ArgumentException("Você deve enviar à API o arquivo OU a url do arquivo.");
        }

        #endregion
    }
}