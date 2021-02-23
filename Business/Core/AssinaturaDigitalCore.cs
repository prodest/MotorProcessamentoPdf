﻿using Business.Core.ICore;
using Infrastructure;
using Infrastructure.Models;
using Infrastructure.Repositories;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        private readonly JsonData JsonData;
        private readonly IConfiguration Configuration;
        private readonly IApiRepository ApiRepository;

        public AssinaturaDigitalCore(IApiRepository apiRepository, JsonData jsonData, IConfiguration configuration)
        {
            ApiRepository = apiRepository;
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
            string keystoreRoot = $@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)}".Replace(@"file:\", "");
            string keystorePath = Configuration.GetSection("CertificadoDigitalEdocs").Value;
            string keystore = $@"{keystoreRoot}\{keystorePath}";

            var certificado = new FileStream(keystore, FileMode.Open, FileAccess.Read);
            char[] password = "kglZcWZ&yas95I$5".ToCharArray();
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
                "Motivo de teste", 
                "Local de teste");

            return documentoAssinado;
        }

        #region Métodos privados

        private byte[] Sign(byte[] src, Org.BouncyCastle.X509.X509Certificate[] chain, ICipherParameters pk,
            string digestAlgorithm, PdfSigner.CryptoStandard subfilter, string reason, string location)
        {
            using (MemoryStream outputMemoryStream = new MemoryStream())
            using (MemoryStream memoryStream = new MemoryStream(src))
            using (PdfReader pdfReader = new PdfReader(memoryStream))
            {
                PdfSigner signer = new PdfSigner(
                    pdfReader, outputMemoryStream,
                    new StampingProperties());

                // Create the signature appearance
                iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(36, 648, 200, 100);
                PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
                appearance.SetReason(reason)
                    .SetLocation(location)
                    // Specify if the appearance before field is signed will be used
                    // as a background for the signed field. The "false" value is the default value.
                    .SetReuseAppearance(false)
                    .SetPageRect(rect)
                    .SetPageNumber(1);
                signer.SetFieldName("sig");

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
    }
}
