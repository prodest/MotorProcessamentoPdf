using Business.Core.ICore;
using Infrastructure;
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
using System.Threading.Tasks;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        private readonly JsonData JsonData;
        private readonly IConfiguration Configuration;

        public AssinaturaDigitalCore(JsonData jsonData, IConfiguration configuration)
        {
            JsonData = jsonData;
            Configuration = configuration;
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
            byte[] arquivo = await JsonData.GetAndReadByteArrayAsync(url);
            
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

        #region Signature Validation

        //public void SignatureValidationV2(byte[] arquivoBytes)
        //{
        //    using PdfReader pdfReader = new PdfReader(new MemoryStream(arquivoBytes));
        //    using PdfDocument pdfDocument = new PdfDocument(pdfReader);
        //    SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
        //    foreach (var signatureName in signatureUtil.GetSignatureNames())
        //    {
        //        var aaaa = signatureUtil.SignatureCoversWholeDocument(signatureName);
        //        PdfPKCS7 signatureData = signatureUtil.ReadSignatureData(signatureName);
        //        var bbbb = signatureData.VerifySignatureIntegrityAndAuthenticity();
        //    }
        //}

        #endregion

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
            byte[] documento = await JsonData.GetAndReadByteArrayAsync(url);
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
            byte[] file = await JsonData.GetAndReadByteArrayAsync(fileUrl);
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
            byte[] documento = await JsonData.GetAndReadByteArrayAsync(url);
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

        #region Métodos privados

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

        private async Task<byte[]> AdicionarMetadados(byte[] input, KeyValuePair<string, string> customMetadaData)
        {
            byte[] output;
            using (PdfReader pdfReader = new PdfReader(new MemoryStream(input)))
            using (MemoryStream outputStream = new MemoryStream())
            using (PdfWriter pdfWriter = new PdfWriter(outputStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
            {
                pdfDocument.GetDocumentInfo().SetMoreInfo(customMetadaData.Key, customMetadaData.Value);
                pdfDocument.Close();

                output = outputStream.ToArray();

                pdfWriter.Close();
                outputStream.Close();
                pdfReader.Close();
            }

            return output;
        }

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
            byte[] arquivo = await JsonData.GetAndReadByteArrayAsync(fileUrl);
            var response = RemoverAssinaturasDigitais(arquivo);
            return response;
        }

        private byte[] RemoverAssinaturasDigitais(byte[] fileBytes)
        {
            using PdfReader pdfReader = new PdfReader(new MemoryStream(fileBytes));
            pdfReader.SetUnethicalReading(true);

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

        #endregion
    }
}
