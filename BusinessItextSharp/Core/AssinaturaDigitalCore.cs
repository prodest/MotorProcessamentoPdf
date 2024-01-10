﻿using AutoMapper;
using BusinessItextSharp.Model.CertificadoDigital;
using BusinessItextSharp.Models;
using Infrastructure;
using Infrastructure.Models;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BusinessItextSharp.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        private static string message = "\nConsidere capturar este documento como \"cópia\".";
        private readonly JsonData JsonData;
        private readonly IMapper Mapper;
        private readonly IConfiguration Configuration;

        public AssinaturaDigitalCore(JsonData jsonData, IMapper mapper, IConfiguration configuration)
        {
            JsonData = jsonData;
            Mapper = mapper;
            Configuration = configuration;
        }

        #region Validate Digital Signatures

        public async Task<bool> ValidateDigitalSignatures(InputFile inputFile)
        {
            inputFile.IsValid();

            bool isValid = false;
            if (inputFile.FileBytes != null)
                isValid = await ValidateDigitalSignatures(inputFile.FileBytes);
            else
                isValid = await ValidateDigitalSignatures(inputFile.FileUrl);

            return isValid;
        }

        public async Task<bool> ValidateDigitalSignatures(string url)
        {
            byte[] file = null;
            try
            {
                file = await JsonData.GetAndReadByteArrayAsync(url);
            }
            catch (Exception)
            {
                throw new Exception("Documento indisponível");
            }

            var certificado = await ValidateDigitalSignatures(file);

            return certificado;
        }

        public async Task<bool> ValidateDigitalSignatures(byte[] file)
        {
            try
            {
                PdfReader reader = new PdfReader(file);

                // ordenar a lista de assinaturas
                var orderedSignatureNames = GetOrderedSignatureNames(reader);

                // ordernar as posições das tabelas de referência cruzada
                List<int> XrefByteOffsetOrdered = reader.XrefByteOffset.Cast<int>().ToList();
                XrefByteOffsetOrdered.Sort();

                var assinaramTodoDocumentoSN = reader.SignaturesCoverWholeDocument().Cast<string>().ToList();

                List<KeyValuePair<string, string>> naoAssinaramTodoDocumento = new List<KeyValuePair<string, string>>();
                List<KeyValuePair<string, string>> assinaramTodoDocumento = new List<KeyValuePair<string, string>>();
                foreach (string signatureName in orderedSignatureNames)
                {
                    PdfPkcs7 pkcs7 = reader.AcroFields.VerifySignature(signatureName);

                    var messages = await OnlineChainValidation(pkcs7.SigningCertificate.GetEncoded());
                    if (!string.IsNullOrWhiteSpace(messages))
                        throw new Exception(messages);

                    CertificadoDigital cert = new CertificadoDigital(pkcs7.SigningCertificate.GetEncoded());

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

                    if (!assinaramTodoDocumentoSN.Contains(signatureName))
                        naoAssinaramTodoDocumento.Add(new KeyValuePair<string, string>(pessoa, signatureName));
                    else
                        assinaramTodoDocumento.Add(new KeyValuePair<string, string>(pessoa, signatureName));
                }

                // Deixar apenas a última assinatura de cada pessoa/cnpj
                var distinctSignersList = RemoverAssinaturasDuplicadas(assinaramTodoDocumento);
                var distinctNaoAssinaramTodoDocumento = RemoverAssinaturasDuplicadas(naoAssinaramTodoDocumento);

                TodosAssinaramDocumentoPorInteiro(distinctNaoAssinaramTodoDocumento);

                return true;
            }
            catch (Exception e)
            {
                throw new Exception($"Ocorreu um erro: {e.Message}");
            }
        }

        #endregion

        #region Signature Validation

        public async Task<IEnumerable<CertificadoDigitalDto>> SignatureValidation(InputFile inputFile, bool ignoreExpired = false)
        {
            inputFile.IsValid();
            IEnumerable<CertificadoDigital> listaCertificados = new List<CertificadoDigital>();
            if (inputFile.FileBytes != null)
                listaCertificados = await SignatureValidation(inputFile.FileBytes, ignoreExpired);
            else
                listaCertificados = await SignatureValidation(inputFile.FileUrl, ignoreExpired);
            IEnumerable<CertificadoDigitalDto> result = Mapper.Map<IEnumerable<CertificadoDigitalDto>>(listaCertificados);
            return result;
        }

        private async Task<IEnumerable<CertificadoDigital>> SignatureValidation(string url, bool ignoreExpired = false)
        {
            byte[] file = null;
            try
            {
                file = await JsonData.GetAndReadByteArrayAsync(url);
            }
            catch (Exception)
            {
                throw new Exception("Documento indisponível");
            }

            var certificado = await SignatureValidation(file, ignoreExpired);

            return certificado;
        }

        private async Task<IEnumerable<CertificadoDigital>> SignatureValidation(byte[] file, bool ignoreExpired = false)
        {
            PdfReader reader = new PdfReader(file);

            if (!DocumentoPossuiAssinaturaDigital(reader))
                return new List<CertificadoDigital>();

            var digitalCertificateList = new List<CertificadoDigital>();
            var signatureNameList = reader.AcroFields.GetSignatureNames();
            foreach (string signatureName in signatureNameList)
            {
                PdfPkcs7 pkcs7 = reader.AcroFields.VerifySignature(signatureName);

                // validação do certificado via outbound
                var messages = await OnlineChainValidation(
                    pkcs7.SigningCertificate.GetEncoded(),
                    ignoreExpired
                );
                if (!string.IsNullOrWhiteSpace(messages))
                    throw new Exception(messages);

                CertificadoDigital cert = new CertificadoDigital(pkcs7);

                // validations
                ValidCertificateChain(cert);
                if(!ignoreExpired)
                    ValidDigitalCertificate(cert, pkcs7);
                ValidSignatureType(cert);
                ValidSignatureDate(pkcs7);
                IsDocumentUnadulterated(pkcs7);

                // cpf-cnpj, name, signature's date
                PessoaFisica pessoaFisica = cert.PessoaJuridica?.Responsavel ?? cert.PessoaFisica;
                string pessoa = $"{pessoaFisica.Nome.ToUpper()}";
                if (cert.PessoaJuridica != null)
                    pessoa += $" ({cert.PessoaJuridica.RazaoSocial.ToUpper()})";

                digitalCertificateList.Add(cert);
            }

            return digitalCertificateList;
        }

        #endregion

        #region Has Digital Signature

        public async Task<bool> HasDigitalSignature(InputFile inputFile)
        {
            inputFile.IsValid();

            bool isValid;
            if (inputFile.FileBytes != null)
                isValid = HasDigitalSignature(inputFile.FileBytes);
            else
                isValid = await HasDigitalSignature(inputFile.FileUrl);

            return isValid;
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
                PdfReader pdfReader = new PdfReader(memoryStream);

                var assinaturas = pdfReader.AcroFields.GetSignatureNames().Count;
                if (assinaturas >= 1)
                    return true;
                else
                    return false;
            }
        }

        #endregion

        public CertificadoDigitalDto ObterInformacoesCertificadoDigital()
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
            {
                chain[k] = ce[k].Certificate;
            }

            var certificadoDigital = new CertificadoDigital(new X509Certificate2(chain[0].GetEncoded()));

            var certificadoDigitalDto = Mapper.Map<CertificadoDigitalDto>(certificadoDigital);

            return certificadoDigitalDto;
        }

        #region Axiliares

        private static Dictionary<string, string> RemoverAssinaturasDuplicadas(List<KeyValuePair<string, string>> signersList)
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

        private static IEnumerable<string> GetOrderedSignatureNames(PdfReader reader)
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

        public static async Task<HttpResponseMessage> Upload(string url, byte[] pdf, bool ignoreExpired = false)
        {
            using MemoryStream memoryStream = new MemoryStream(pdf);

            using var MultipartFormDataContent = new MultipartFormDataContent();
            MultipartFormDataContent.Add(new StreamContent(memoryStream), "certificateFile", "certificateFile");

            using HttpClient httpClient = new HttpClient();
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(url, MultipartFormDataContent);

            return httpResponseMessage;
        }

        #endregion

        #region Validações

        private bool DocumentoPossuiAssinaturaDigital(PdfReader reader)
        {
            var qntAssinaturas = reader.AcroFields.GetSignatureNames().Count;
            if (qntAssinaturas <= 0)
                return false;
            return true;
        }

        private static void TodosAssinaramDocumentoPorInteiro(Dictionary<string, string> naoAssinaramTodoDocumento)
        {
            if (naoAssinaramTodoDocumento.Count == 0)
                return;
            string mensagem = "As assinaturas das pessoas físicas abaixo não cobrem todo o documento:\n";
            foreach (var assinante in naoAssinaramTodoDocumento)
                mensagem += $"- {assinante}\n";

            throw new Exception(mensagem);
        }

        private static void IsDocumentUnadulterated(PdfPkcs7 pkcs7)
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

        private static void ValidSignatureDate(PdfPkcs7 pkcs7)
        {
            if (pkcs7.SignDate == null && pkcs7.SignDate <= DateTime.Now)
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

        private static void ValidDigitalCertificate(CertificadoDigital cert, PdfPkcs7 pkcs7)
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

        private async Task<string> OnlineChainValidation(byte[] certificate, bool ignoreExpired = false)
        {
            string urlValidarCertificado;
            if (ignoreExpired)
                urlValidarCertificado = Configuration["OutboundValidacaoCertificado"] + "/api/validar-certificado-ignorar-expirados";
            else
                urlValidarCertificado = Configuration["OutboundValidacaoCertificado"] + "/api/validar-certificado"; ;

            HttpResponseMessage result = await Upload(urlValidarCertificado, certificate);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                return string.Empty;

            var messageArray = result.Content.ReadAsStringAsync().Result;
            
            return messageArray;
        }

        #endregion
    }
}
