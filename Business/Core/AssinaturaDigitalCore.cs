using Business.Core.ICore;
using Business.Shared.Models;
using Business.Shared.Models.CertificadoDigital;
using Infrastructure.Repositories;
using iText.Forms;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        private static string message = "\nConsidere capturar este documento como \"cópia\".";
        private readonly IConfiguration Configuration;
        private readonly IApiRepository ApiRepository;

        public AssinaturaDigitalCore(IConfiguration configuration, IApiRepository apiRepository)
        {
            Configuration = configuration;
            ApiRepository = apiRepository;
        }

        #region Has Digital Signature

        public async Task<bool> HasDigitalSignature(InputFile inputFile)
        {
            inputFile.IsValid();

            bool result;
            if (!string.IsNullOrWhiteSpace(inputFile.FileUrl))
                result = await HasDigitalSignature(inputFile.FileUrl);
            else
                result = HasDigitalSignature(inputFile.FileBytes);

            return result;
        }

        public async Task<bool> HasDigitalSignature(string url)
        {
            byte[] arquivo = await ApiRepository.GetAndReadAsByteArrayAsync(url);
            var response = HasDigitalSignature(arquivo);
            return response;
        }

        public bool HasDigitalSignature(byte[] file)
        {
            using (MemoryStream memoryStream = new MemoryStream(file))
            {
                var result = HasDigitalSignature(memoryStream);
                
                memoryStream.Close();
                
                return result;
            }
        }

        public bool HasDigitalSignature(MemoryStream memoryStream)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);

            using (PdfReader pdfReader = new PdfReader(memoryStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                SignatureUtil signUtil = new SignatureUtil(pdfDocument);
                var assinaturas = signUtil.GetSignatureNames();

                pdfDocument.Close();
                pdfReader.Close();

                if (assinaturas?.Count >= 1)
                    return true;
                else
                    return false;
            }
        }

        #endregion

        #region AdicionarAssinaturaDigital

        public async Task<byte[]> AdicionarAssinaturaDigital(InputFile inputFile, string signatureFieldName)
        {
            inputFile.IsValid();

            byte[] dococumentoAssinado;
            if (inputFile.FileUrl != null)
                dococumentoAssinado = await AdicionarAssinaturaDigital(inputFile.FileUrl, signatureFieldName);
            else
                dococumentoAssinado = AdicionarAssinaturaDigital(inputFile.FileBytes, signatureFieldName);

            return dococumentoAssinado;
        }

        private async Task<byte[]> AdicionarAssinaturaDigital(string url, string signatureFieldName)
        {
            byte[] documento = await ApiRepository.GetAndReadAsByteArrayAsync(url);
            var documentoCarimbado = AdicionarAssinaturaDigital(documento, signatureFieldName);
            return documentoCarimbado;
        }

        private byte[] AdicionarAssinaturaDigital(byte[] fileBytes, string signatureFieldName)
        {
            string keystoreRoot = $@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)}".Replace(@"file:\", "");
            string keystorePath = Configuration["DigitalCertificate:Keystore"];
            string keystore = $@"{keystoreRoot}\{keystorePath}";

            string passwordString = Configuration["DigitalCertificate:Password"];
            char[] password = passwordString.ToCharArray();
            var certificado = new FileStream(keystore, FileMode.Open, FileAccess.Read);
            Pkcs12Store pk12 = new Pkcs12Store(certificado, password);

            string alias = null;
            foreach (var a in pk12.Aliases)
            {
                alias = ((string)a);
                if (pk12.IsKeyEntry(alias))
                    break;
            }

            ICipherParameters pk = pk12.GetKey(alias).Key;
            X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
                chain[k] = ce[k].Certificate;

            var documentoAssinado = Sign(
                fileBytes, chain, pk,
                DigestAlgorithms.SHA512,
                PdfSigner.CryptoStandard.CADES,
                signatureFieldName
            );

            return documentoAssinado;
        }

        #region Auxiliares

        private byte[] Sign(byte[] src, Org.BouncyCastle.X509.X509Certificate[] chain, ICipherParameters pk,
            string digestAlgorithm, PdfSigner.CryptoStandard subfilter, string signatureFieldName
        )
        {
            using (MemoryStream outputMemoryStream = new MemoryStream())
            using (MemoryStream memoryStream = new MemoryStream(src))
            using (PdfReader pdfReader = new PdfReader(memoryStream))
            {
                PdfSigner signer = new PdfSigner(
                    pdfReader, outputMemoryStream,
                    new StampingProperties().UseAppendMode()
                );

                signer.SetFieldName(signatureFieldName);

                IExternalSignature pks = new PrivateKeySignature(pk, digestAlgorithm);

                try
                {
                    // Sign the document using the detached mode, CMS or CAdES equivalent.
                    signer.SignDetached(pks, chain, null, null, null, 0, subfilter);
                }
                catch (Exception ex)
                {
                    throw;
                }

                pdfReader.Close();
                memoryStream.Close();
                var documentoAssinado = outputMemoryStream.ToArray();
                outputMemoryStream.Close();

                return documentoAssinado;
            }
        }

        #endregion

        #endregion

        #region ValidarHashDocumento

        public async Task<bool> ValidarHashDocumento(InputFile inputFile, string hash)
        {
            inputFile.IsValid();

            bool documentoAutentico;
            if (!string.IsNullOrWhiteSpace(inputFile.FileUrl))
                documentoAutentico = await ValidarHashDocumento(inputFile.FileUrl, hash);
            else
                documentoAutentico = ValidarHashDocumento(inputFile.FileBytes, hash);

            return documentoAutentico;
        }

        private async Task<bool> ValidarHashDocumento(string fileUrl, string hash)
        {
            byte[] file = await ApiRepository.GetAndReadAsByteArrayAsync(fileUrl);
            bool documentoAutentico = ValidarHashDocumento(file, hash);
            return documentoAutentico;
        }

        private bool ValidarHashDocumento(byte[] fileBytes, string hash)
        {
            // converter representação hexadecimal em byte[]
            hash = hash.Substring(2, hash.Length - 2);
            byte[] hashArray = Enumerable.Range(0, hash.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hash.Substring(x, 2), 16))
                .ToArray();

            // obter hash do documento postado
            HashAlgorithm sha512 = SHA512.Create();
            byte[] hashCalculado = sha512.ComputeHash(fileBytes);

            if (hashCalculado.SequenceEqual(hashArray))
                return true;
            else
                return false;
        }

        #endregion

        #region ObterSignatureFieldName

        public async Task<ICollection<string>> ObterSignatureFieldName(InputFile inputFile)
        {
            inputFile.IsValid();

            ICollection<string> dococumentoAssinado;
            if (inputFile.FileUrl != null)
                dococumentoAssinado = await ObterSignatureFieldName(inputFile.FileUrl);
            else
                dococumentoAssinado = ObterSignatureFieldName(inputFile.FileBytes);

            return dococumentoAssinado;
        }

        private async Task<ICollection<string>> ObterSignatureFieldName(string url)
        {
            byte[] documento = await ApiRepository.GetAndReadAsByteArrayAsync(url);
            var documentoCarimbado = ObterSignatureFieldName(documento);
            return documentoCarimbado;
        }

        private ICollection<string> ObterSignatureFieldName(byte[] fileBytes)
        {
            using PdfReader pdfReader = new PdfReader(new MemoryStream(fileBytes));
            using PdfDocument pdfDocument = new PdfDocument(pdfReader);

            SignatureUtil signUtil = new SignatureUtil(pdfDocument);
            var assinaturas = signUtil.GetSignatureNames();

            return assinaturas;
        }

        #endregion

        #region RemoverAssinaturasDigitais

        public async Task<byte[]> RemoverAssinaturasDigitais(InputFile inputFile)
        {
            inputFile.IsValid();

            byte[] arquivoSemAssinatura;
            if (!string.IsNullOrWhiteSpace(inputFile.FileUrl))
                arquivoSemAssinatura = await RemoverAssinaturasDigitais(inputFile.FileUrl);
            else
                arquivoSemAssinatura = RemoverAssinaturasDigitais(inputFile.FileBytes);

            return arquivoSemAssinatura;
        }

        private async Task<byte[]> RemoverAssinaturasDigitais(string fileUrl)
        {
            byte[] arquivo = await ApiRepository.GetAndReadAsByteArrayAsync(fileUrl);
            var response = RemoverAssinaturasDigitais(arquivo);
            return response;
        }

        private byte[] RemoverAssinaturasDigitais(byte[] fileBytes)
        {
            using PdfReader pdfReader = new PdfReader(new MemoryStream(fileBytes));

            using MemoryStream outputStream = new MemoryStream();
            using PdfWriter pdfWriter = new PdfWriter(outputStream);

            using PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter);
            PdfAcroForm form = PdfAcroForm.GetAcroForm(pdfDocument, true);
            form.FlattenFields();

            pdfDocument.Close();
            pdfWriter.Close();
            outputStream.Close();
            pdfReader.Close();

            byte[] outputArray = outputStream.ToArray();
            return outputArray;
        }

        #endregion

        #region ValidarAssinaturaDigital

        public async Task<IEnumerable<CertificadoDigital>> ValidarAssinaturaDigital(InputFile inputFile, bool ignorarExpiradas)
        {
            inputFile.IsValid();

            IEnumerable<CertificadoDigital> result;
            if (!string.IsNullOrWhiteSpace(inputFile.FileUrl))
                result = await ValidarAssinaturaDigital(inputFile.FileUrl, ignorarExpiradas);
            else
                result = await ValidarAssinaturaDigitalAsync(inputFile.FileBytes, ignorarExpiradas);

            return result;
        }

        private async Task<IEnumerable<CertificadoDigital>> ValidarAssinaturaDigital(string fileUrl, bool ignorarExpiradas)
        {
            byte[] fileBytes = await ApiRepository.GetAndReadAsByteArrayAsync(fileUrl);
            IEnumerable<CertificadoDigital> certificados = await ValidarAssinaturaDigitalAsync(fileBytes, ignorarExpiradas);
            return certificados;
        }

        private async Task<IEnumerable<CertificadoDigital>> ValidarAssinaturaDigitalAsync(byte[] fileBytes, bool ignorarExpiradas)
        {
            if (!HasDigitalSignature(fileBytes))
                throw new Exception("Este documento não possui Assinaturas Digitais");

            using MemoryStream memoryStream = new MemoryStream(fileBytes);
            using PdfReader pdfReader = new PdfReader(memoryStream);
            using PdfDocument pdfDocument = new PdfDocument(pdfReader);
            SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);

            var digitalCertificateList = new List<CertificadoDigital>();
            foreach (var signatureName in signatureUtil.GetSignatureNames())
            {
                PdfPKCS7 pdfPKCS7 = signatureUtil.ReadSignatureData(signatureName);
                byte[] signingCertificate = pdfPKCS7.GetSigningCertificate().GetEncoded();
                CertificadoDigital certificadoDigital = new CertificadoDigital(signingCertificate);

                // validações online (outbound)
                await ApiRepository.OnlineChainValidationAsync(signingCertificate, ignorarExpiradas);

                // validações locais
                ValidCertificateChain(certificadoDigital);
                if (!ignorarExpiradas)
                    ValidDigitalCertificate(certificadoDigital, pdfPKCS7);
                ValidSignatureType(certificadoDigital);
                ValidSignatureDate(pdfPKCS7);
                IsDocumentUnadulterated(pdfPKCS7);

                // cpf-cnpj, name, signature's date
                PessoaFisica pessoaFisica = certificadoDigital.PessoaJuridica?.Responsavel ?? certificadoDigital.PessoaFisica;
                string pessoa = $"{pessoaFisica.Nome.ToUpper()}";
                if (certificadoDigital.PessoaJuridica != null)
                    pessoa += $" ({certificadoDigital.PessoaJuridica.RazaoSocial.ToUpper()})";

                digitalCertificateList.Add(certificadoDigital);
            }

            return digitalCertificateList;
        }

        #region Axiliares

        private static void IsDocumentUnadulterated(PdfPKCS7 pdfPKCS7)
        {
            if(!pdfPKCS7.VerifySignatureIntegrityAndAuthenticity())
                throw new Exception("A integridade deste documento está comprometida.");
        }

        private static void ValidSignatureDate(PdfPKCS7 pkcs7)
        {
            if (pkcs7.GetSignDate() == null && pkcs7.GetSignDate() <= DateTime.Now)
                throw new Exception("A assinatura digital deste documento possui uma data de assinatura inválida." + message);
        }

        private static void ValidSignatureType(CertificadoDigital cert)
        {
            if (cert.TipoCertificado != TipoCertificadoEnum.eCPF && cert.TipoCertificado != TipoCertificadoEnum.eCNPJ)
                throw new Exception("A assinatura digital deste documento possui um tipo desconhecido.");

            if ((cert.PessoaFisica == null || string.IsNullOrWhiteSpace(cert.PessoaFisica.CPF)) && (cert.PessoaJuridica == null || cert.PessoaJuridica.Responsavel == null || string.IsNullOrWhiteSpace(cert.PessoaJuridica.Responsavel.CPF)))
                throw new Exception("A assinatura digital deste documento não está associada a um CPF ou CNPJ.");

            if ((cert.PessoaFisica == null || string.IsNullOrWhiteSpace(cert.PessoaFisica.CPF)) && (cert.PessoaJuridica == null || cert.PessoaJuridica.Responsavel == null || string.IsNullOrWhiteSpace(cert.PessoaJuridica.Responsavel.Nome)))
                throw new Exception("A assinatura digital deste documento não possui a informação do nome do assinante.");
        }

        private static void ValidDigitalCertificate(CertificadoDigital cert, PdfPKCS7 pkcs7)
        {
            bool timestampImprint = pkcs7.VerifyTimestampImprint();
            if (!timestampImprint && !cert.PeriodoValido)
                throw new Exception("Este documento possui uma assinatura ICPBrasil inválida.");
        }

        private static void ValidCertificateChain(CertificadoDigital cert)
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

        #endregion
        
        #endregion
    }
}
