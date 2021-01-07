using Business.Core.ICore;
using Business.Shared;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        private string apiValidarCertificado = @"https://api.es.gov.br/certificado/api/validar-certificado";
        private string message = "\nConsidere capturar este documento como \"cópia\".";
        private readonly JsonData JsonData;
        public static readonly char[] PASSWORD = "kglZcWZ&yas95I$5".ToCharArray();
        public static readonly string KEYSTORE = @"C:\Users\prodest1\Desktop\e-docs.des.es.gov.br.pfx";
        public static readonly string SRC = @"C:\Users\prodest1\Desktop\Olá.pdf";
        public static readonly string DEST = @"C:\Users\prodest1\Desktop\TesteAssinaturas\word\";

        public AssinaturaDigitalCore(JsonData jsonData)
        {
            JsonData = jsonData;
        }

        #region Adicionar Assinatura Digital

        public async Task<byte[]> AdicionarAssinaturaDigital(string url)
        {
            byte[] documento = await JsonData.GetAndDownloadAsync(url);

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

            Sign(documento, chain, pk, iText.Signatures.DigestAlgorithms.SHA512, iText.Signatures.PdfSigner.CryptoStandard.CADES, "Motivo de teste", "Local de teste");

            return documento;
        }

        public void Sign(byte[] src, Org.BouncyCastle.X509.X509Certificate[] chain, ICipherParameters pk, 
            String digestAlgorithm, iText.Signatures.PdfSigner.CryptoStandard subfilter, String reason, String location
        ){
            using (MemoryStream outputMemoryStream = new MemoryStream())
            using (MemoryStream memoryStream = new MemoryStream(src))
            using (iText.Kernel.Pdf.PdfReader pdfReader = new iText.Kernel.Pdf.PdfReader(memoryStream))
            {
                iText.Signatures.PdfSigner signer = new iText.Signatures.PdfSigner(
                    pdfReader, new FileStream(DEST + Guid.NewGuid().ToString(), FileMode.Create), 
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
                src = outputMemoryStream.ToArray();
                outputMemoryStream.Close();
            }
        }

        #endregion
    }
}
