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

        public void SignatureValidationV2(byte[] arquivoBytes)
        {
            using PdfReader pdfReader = new PdfReader(new MemoryStream(arquivoBytes));
            using PdfDocument pdfDocument = new PdfDocument(pdfReader);
            SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
            foreach (var signatureName in signatureUtil.GetSignatureNames())
            {
                var aaaa = signatureUtil.SignatureCoversWholeDocument(signatureName);
                PdfPKCS7 signatureData = signatureUtil.ReadSignatureData(signatureName);
                var bbbb = signatureData.VerifySignatureIntegrityAndAuthenticity();
            }
        }

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
                signatureFieldName
            );

            return documentoAssinado;
        }

        public async Task<bool> ValidarHashDocumento(Stream stream, string hashDoBancoString)
        {
            // obter hash do documento postado
            HashAlgorithm sha512 = SHA512.Create();
            byte[] document = new byte[stream.Length];
            await stream.ReadAsync(document, 0, (int)stream.Length);
            var Hash = sha512.ComputeHash(document);

            // converter representação hexadecimal em byte[]
            hashDoBancoString = hashDoBancoString.Substring(2, hashDoBancoString.Length - 2);
            var hashDoBancoBytes = Enumerable.Range(0, hashDoBancoString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hashDoBancoString.Substring(x, 2), 16))
                .ToArray();

            if (Hash.SequenceEqual(hashDoBancoBytes))
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

        #endregion
    }
}
