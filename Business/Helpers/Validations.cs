using iText.Kernel.Pdf;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Business.Helpers
{
    static class Validations
    {
        public static void ArquivoValido(byte[] arquivo)
        {
            if (arquivo.Length <= 0)
                throw new Exception("Arquivo vazio ou corrompido.");

            IsPdf(arquivo);
        }

        public static void IsPdf(byte[] arquivo)
        {
            try
            {
                using (var readStream = new MemoryStream(arquivo))
                using (var reader = new PdfReader(readStream))
                {
                    // ok, o arquivo é um documento pdf
                    return;
                }
            }
            catch (Exception)
            {
                throw new Exception("Este arquivo não é um documento PDF válido.");
            }
        }

        public static void IsPdfa1b(byte[] arquivo)
        {
            try
            {
                using (MemoryStream readStream = new MemoryStream(arquivo))
                using (PdfReader reader = new PdfReader(readStream))
                using (PdfDocument pdfDocument = new PdfDocument(reader))
                {
                    var conformanceLevel = reader.GetPdfAConformanceLevel();
                    if(conformanceLevel == null || (conformanceLevel.GetPart() != "1" || conformanceLevel.GetConformance() != "B"))
                        throw new Exception("Este arquivo não é um documento PDF/A-1B válido.");
                }
            }
            catch (Exception)
            {
                throw new Exception("Este arquivo não é um documento PDF/A-1B válido.");
            }
        }

        public static void ProtocoloValido(string protocolo)
        {
            if (string.IsNullOrWhiteSpace(protocolo))
                throw new Exception("O protocolo informado está vazio.");

            if (!Regex.IsMatch(protocolo, "^[0-9]{4}-[A-Z0-9]{5}"))
                throw new Exception("O protocolo informado está fora do padrão.");
        }

        public static void RegistroValido(string registro)
        {
            if (string.IsNullOrWhiteSpace(registro))
                throw new Exception("O registro informado está vazio.");

            if (!Regex.IsMatch(registro, "^[0-9]{4}-[A-Z0-9]{6}"))
                throw new Exception("O registro informado está fora do padrão.");
        }

        internal static void dataHoraValida(DateTime dataHora)
        {
            if(dataHora == null)
                throw new Exception("O conjunto de data e hora informado está vazio.");
            
            if (dataHora == default(DateTime))
                throw new Exception("O conjunto de data e hora informado é inválido.");
        }

    }
}
