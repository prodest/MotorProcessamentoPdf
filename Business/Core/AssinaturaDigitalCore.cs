using Business.Core.ICore;
using Infrastructure;
using Infrastructure.Models;
using Infrastructure.Repositories;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        private readonly JsonData JsonData;
        private readonly IApiRepository ApiRepository;

        public AssinaturaDigitalCore(IApiRepository apiRepository, JsonData jsonData)
        {
            ApiRepository = apiRepository;
            JsonData = jsonData;
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
            using (PdfReader pdfReader = new PdfReader(memoryStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                SignatureUtil signUtil = new SignatureUtil(pdfDocument);
                var assinaturas = signUtil.GetSignatureNames();

                pdfDocument.Close();
                pdfReader.Close();
                memoryStream.Close();

                if (assinaturas?.Count >= 1)
                    return true;
                else 
                    return false;
            }
        }

        #endregion

        #region Signature Validation

        public async Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> SignatureValidation(string url)
        {
            var response = await ApiRepository.ValidarAssinaturaDigitalAsync(url);
            return response;
        }

        public async Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> SignatureValidation(byte[] file)
        {
            var response = await ApiRepository.ValidarAssinaturaDigitalAsync(file);
            return response;
        }

        #endregion

        public async Task<byte[]> AdicionarAssinaturaDigital(InputFile inputFile)
        {
            inputFile.IsValid();

            byte[] dococumentoAssinado = null;
            if (inputFile.FileUrl != null)
                dococumentoAssinado = await AdicionarAssinaturaDigital(inputFile.FileUrl);
            else
                dococumentoAssinado = AdicionarAssinaturaDigital(inputFile.FileBytes);

            return dococumentoAssinado;
        }

        private async Task<byte[]> AdicionarAssinaturaDigital(string url)
        {
            byte[] documento = await JsonData.GetAndReadByteArrayAsync(url);
            var documentoCarimbado = AdicionarAssinaturaDigital(documento);
            return documentoCarimbado;
        }

        private byte[] AdicionarAssinaturaDigital(byte[] fileBytes)
        {
            string KEYSTORE = @"../Infrastructure/Resources/teste-e-docs.des.es.gov.br.pfx";
            char[] PASSWORD = "kglZcWZ&yas95I$5".ToCharArray();

            Pkcs12Store pk12 = new Pkcs12Store(new FileStream(KEYSTORE, FileMode.Open, FileAccess.Read), PASSWORD);
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
            {
                chain[k] = ce[k].Certificate;
            }

            var documentoAssinado = Sign(fileBytes, chain, pk, iText.Signatures.DigestAlgorithms.SHA512, 
                iText.Signatures.PdfSigner.CryptoStandard.CADES,
                "Motivo de teste", 
                "Local de teste");

            return documentoAssinado;
        }

        #region Métodos privados

        private byte[] Sign(byte[] src, Org.BouncyCastle.X509.X509Certificate[] chain, ICipherParameters pk,
            String digestAlgorithm, iText.Signatures.PdfSigner.CryptoStandard subfilter, String reason, String location)
        {
            using (MemoryStream outputMemoryStream = new MemoryStream())
            using (MemoryStream memoryStream = new MemoryStream(src))
            using (iText.Kernel.Pdf.PdfReader pdfReader = new iText.Kernel.Pdf.PdfReader(memoryStream))
            {
                iText.Signatures.PdfSigner signer = new iText.Signatures.PdfSigner(
                    pdfReader, outputMemoryStream,
                    new iText.Kernel.Pdf.StampingProperties());

                // Create the signature appearance
                iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(36, 648, 200, 100);
                iText.Signatures.PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
                appearance.SetReason(reason)
                    .SetLocation(location)
                    // Specify if the appearance before field is signed will be used
                    // as a background for the signed field. The "false" value is the default value.
                    .SetReuseAppearance(false)
                    .SetPageRect(rect)
                    .SetPageNumber(1);
                signer.SetFieldName("sig");

                iText.Signatures.IExternalSignature pks = new iText.Signatures.PrivateKeySignature(pk, digestAlgorithm);

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
    }
}
