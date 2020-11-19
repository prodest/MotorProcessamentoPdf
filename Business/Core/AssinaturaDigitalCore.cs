using Business.Core.ICore;
using Business.Helpers.AssinaturaDigital;
using Business.Shared;
using iTextSharp.text.pdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        private string ApiValidarCertificado = @"https://api.es.gov.br/certificado/api/validar-certificado";
        private string message = "\nConsidere capturar este documento como \"cópia\".";
        private readonly JsonData JsonData;

        public AssinaturaDigitalCore(JsonData jsonData)
        {
            JsonData = jsonData;
        }

        #region Signature Validation

        public async Task<IEnumerable<CertificadoDigital>> SignatureValidation(byte[] file)
        {
            // validações
            ValidationHelper.ArquivoValido(file);

            PdfReader reader = new PdfReader(file);

            var orderedSignatureNames = GetOrderedSignatureNames(reader);

            // ordernar as posições das tabelas de referência cruzada
            List<int> XrefByteOffsetOrdered = reader.XrefByteOffset.Cast<int>().ToList();
            XrefByteOffsetOrdered.Sort();

            List<string> assinaramTodoDocumentoSN = null;
            try
            {
                assinaramTodoDocumentoSN = reader.SignaturesCoverWholeDocument().Cast<string>().ToList();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("No signatures found"))
                    throw new Exception("Nenhuma assinatura foi encontrada.");
                else
                    throw;
            }

            List<KeyValuePair<string, string>> naoAssinaramTodoDocumento = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> assinaramTodoDocumento = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, CertificadoDigital>> listaCertificados = new  List<KeyValuePair<string, CertificadoDigital>>();
            foreach (string signatureName in orderedSignatureNames)
            {
                PdfPkcs7 pkcs7 = reader.AcroFields.VerifySignature(signatureName);

                var messages = await OnlineChainValidation(pkcs7.SigningCertificate.GetEncoded());
                if (!string.IsNullOrWhiteSpace(messages))
                    throw new Exception(messages);

                CertificadoDigital cert = new CertificadoDigital(pkcs7);
                listaCertificados.Add(new KeyValuePair<string, CertificadoDigital>(signatureName, cert));

                // validations
                ValidCertificateChain(cert);
                ValidDigitalCertificate(cert, pkcs7);
                ValidSignatureType(cert);
                ValidSignatureDate(pkcs7);
                IsDocumentUnadulterated(pkcs7);

                // cpf-cnpj, name, signature's date
                PessoaFisica pessoaFisica = cert.PessoaJuridica?.Responsavel ?? cert.PessoaFisica;
                string pessoa = $"{pessoaFisica.Nome.ToUpper()}";
                if (cert.PessoaJuridica != null)
                    pessoa += $" ({cert.PessoaJuridica.RazaoSocial.ToUpper()})";

                if (assinaramTodoDocumentoSN == null || !assinaramTodoDocumentoSN.Contains(signatureName))
                    naoAssinaramTodoDocumento.Add(new KeyValuePair<string, string>(pessoa, signatureName));
                else
                    assinaramTodoDocumento.Add(new KeyValuePair<string, string>(pessoa, signatureName));
            }

            // Deixar apenas a última assinatura de cada pessoa/cnpj
            var distinctSignersList = RemoverAssinaturasDuplicadas(assinaramTodoDocumento);
            var distinctNaoAssinaramTodoDocumento = RemoverAssinaturasDuplicadas(naoAssinaramTodoDocumento);

            TodosAssinaramDocumentoPorInteiro(distinctNaoAssinaramTodoDocumento);

            var distinctCert = listaCertificados
                .Where(x => distinctSignersList.Select(y => y.Value).Contains(x.Key))
                .Select(x => x.Value);
            
            return distinctCert;
        }

        public async Task<IEnumerable<CertificadoDigital>> SignatureValidation(string url)
        {
            ValidationHelper.UrlIsNotNullOrEmpty(url);

            byte[] file = await JsonData.GetAndDownloadAsync(url);
            var certificado = await SignatureValidation(file);
            
            return certificado;
        }

        #endregion

        #region Has Digital Signature

        public bool HasDigitalSignature(byte[] file)
        {
            ValidationHelper.ArquivoValido(file);

            PdfReader pdfReader = new PdfReader(file);
            if (pdfReader.AcroFields.GetSignatureNames().Count >= 1)
                return true;
            else
                return false;
        }

        public async Task<bool> HasDigitalSignature(string url)
        {
            ValidationHelper.UrlIsNotNullOrEmpty(url);

            byte[] arquivo = await JsonData.GetAndDownloadAsync(url);

            var response = HasDigitalSignature(arquivo);

            return response;
        }

        #endregion

        #region Axiliares

        private Dictionary<string, string> RemoverAssinaturasDuplicadas(List<KeyValuePair<string, string>> signersList)
        {
            var groupedSignedDistinc = signersList
                .GroupBy(x => x.Key)
                .Select(x => new { x.Key, Count = x.Count() })
                .Where(x => x.Count == 1)
                .ToList();

            var groupedSigned = signersList
                .GroupBy(x => x.Key)
                .Select(x => new { x.Key, Count = x.Count() })
                .Where(x => x.Count > 1)
                .ToList();

            Dictionary<string, string> signersListDistinc = new Dictionary<string, string>();
            foreach (var item in groupedSignedDistinc)
            {
                var distinct = signersList.FirstOrDefault(x =>
                    x.Key == item.Key.ToString()
                );

                if (!string.IsNullOrWhiteSpace(distinct.Value))
                    signersListDistinc.Add(distinct.Key, distinct.Value);
            }

            foreach (var item in groupedSigned)
            {
                var result = signersList.FirstOrDefault(x =>
                    x.Key == item.Key.ToString()
                );

                if (!string.IsNullOrWhiteSpace(result.Key))
                    signersListDistinc.Add(result.Key, result.Value);
            }

            return signersListDistinc;
        }

        private IEnumerable<string> GetOrderedSignatureNames(PdfReader reader)
        {
            ArrayList signatureNames = reader.AcroFields.GetSignatureNames();

            Dictionary<string, long> signatureNamesDict = new Dictionary<string, long>();
            foreach (string signatureName in signatureNames)
            {
                PdfDictionary dict = reader.AcroFields.GetSignatureDictionary(signatureName);
                PdfArray byteRange = dict.GetAsArray(PdfName.Byterange);
                var offset = ((PdfNumber)byteRange[2]).IntValue;
                var end = offset + ((PdfNumber)byteRange[3]).IntValue;
                signatureNamesDict.Add(signatureName, end);
            }
            var orderedSignatures = signatureNamesDict.OrderBy(x => x.Value).Select(x => x.Key);
            return orderedSignatures;
        }

        #endregion

        #region Validações

        private void TodosAssinaramDocumentoPorInteiro(Dictionary<string, string> naoAssinaramTodoDocumento)
        {
            if (naoAssinaramTodoDocumento.Count == 0)
                return;
            string mensagem = "As assinaturas das pessoas físicas abaixo não cobrem todo o documento:\n";
            foreach (var assinante in naoAssinaramTodoDocumento)
                mensagem += $"- {assinante}\n";

            throw new Exception(mensagem);
        }

        private void IsDocumentUnadulterated(PdfPkcs7 pkcs7)
        {
            try
            {
                pkcs7.Verify();
            }
            catch
            {
                throw new Exception("Este documento teve seu conteúdo alterado, portanto sua assinatura tornou-se inválida." + message);
            }
        }

        private void ValidSignatureDate(PdfPkcs7 pkcs7)
        {
            if (pkcs7.SignDate == null && pkcs7.SignDate <= DateTime.Now)
                throw new Exception("A assinatura digital deste documento possui uma data de assinatura inválida." + message);
        }

        private void ValidSignatureType(CertificadoDigital cert)
        {
            if (cert.TipoCertificado != TipoCertificadoEnum.eCPF && cert.TipoCertificado != TipoCertificadoEnum.eCNPJ)
                throw new Exception("A assinatura digital deste documento possui um tipo desconhecido.");

            if ((cert.PessoaFisica == null || string.IsNullOrWhiteSpace(cert.PessoaFisica.CPF)) && (cert.PessoaJuridica == null || cert.PessoaJuridica.Responsavel == null || string.IsNullOrWhiteSpace(cert.PessoaJuridica.Responsavel.CPF)))
                throw new Exception("A assinatura digital deste documento não está associada a um CPF ou CNPJ.");

            if ((cert.PessoaFisica == null || string.IsNullOrWhiteSpace(cert.PessoaFisica.CPF)) && (cert.PessoaJuridica == null || cert.PessoaJuridica.Responsavel == null || string.IsNullOrWhiteSpace(cert.PessoaJuridica.Responsavel.Nome)))
                throw new Exception("A assinatura digital deste documento não possui a informação do nome do assinante.");
        }

        private void ValidDigitalCertificate(CertificadoDigital cert, PdfPkcs7 pkcs7)
        {
            bool timestampImprint = pkcs7.VerifyTimestampImprint();
            if (!timestampImprint && !cert.PeriodoValido)
                throw new Exception("Este documento possui uma assinatura ICPBrasil inválida.");
        }

        private void ValidCertificateChain(CertificadoDigital cert)
        {
            if (cert.ErrosValidacaoCadeia != null && cert.ErrosValidacaoCadeia.Any())
            {
                StringBuilder erroMessage = new StringBuilder();

                bool exception = cert.ErrosValidacaoCadeia.Any(e =>
                    e.Key == (int)X509ChainStatusFlags.PartialChain ||
                    e.Key == (int)X509ChainStatusFlags.RevocationStatusUnknown ||
                    e.Key == (int)X509ChainStatusFlags.OfflineRevocation);

                foreach (var erro in cert.ErrosValidacaoCadeia)
                {
                    erroMessage.AppendLine($"Status: {erro.Key} | Status Information: {erro.Value}");
                }

                if (exception)
                {
                    erroMessage.Insert(0, $"Não foi possível validar a cadeia do certificado digital.\n");
                    throw new Exception(erroMessage.ToString());
                }
                else
                {
                    erroMessage.Insert(0, $"Certificado digital com cadeia inválida. {message}\n");
                    throw new Exception(erroMessage.ToString());
                }
            }
        }

        private async Task<string> OnlineChainValidation(byte[] certificate)
        {
            using (MemoryStream memoryStream = new MemoryStream(certificate))
            using (StreamContent streamContent = new StreamContent(memoryStream))
            using (MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent())
            {
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "certificateFile",
                    FileName = "certificateFile"
                };

                multipartFormDataContent.Add(streamContent, "certificateFile", "certificateFile");

                var result = await JsonData.PostAndReadStreamContentAsync(ApiValidarCertificado, multipartFormDataContent);
                return result;
            }
        }

        #endregion
    }
}
