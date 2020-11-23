using Business.Core.ICore;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System;
using System.IO;

namespace Business.Core
{
    public class AssinaturaDigitalCore : IAssinaturaDigitalCore
    {
        public static readonly string KEYSTORE = @"C:\Users\prodest1\Desktop\e-docs.des.es.gov.br.pfx";
        public static readonly char[] PASSWORD = "kglZcWZ&yas95I$5".ToCharArray();
        
        public MemoryStream Sign(
            MemoryStream arquivo, X509Certificate[] chain, ICipherParameters pk, String digestAlgorithm, 
            PdfSigner.CryptoStandard subfilter, String reason, String location
        ){
            using (MemoryStream outputStream = new MemoryStream())
            using (PdfReader reader = new PdfReader(arquivo))
            {
                PdfSigner signer = new PdfSigner(reader, outputStream, new StampingProperties());

                // Create the signature appearance
                Rectangle rect = new Rectangle(36, 648, 200, 100);
                PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
                appearance
                    .SetReason(reason)
                    .SetLocation(location)

                    // Specify if the appearance before field is signed will be used
                    // as a background for the signed field. The "false" value is the default value.
                    .SetReuseAppearance(false)
                    .SetPageRect(rect)
                    .SetPageNumber(1);
                signer.SetFieldName("sig");

                IExternalSignature pks = new PrivateKeySignature(pk, digestAlgorithm);

                // Sign the document using the detached mode, CMS or CAdES equivalent.
                signer.SignDetached(pks, chain, null, null, null, 0, subfilter);

                return outputStream;
            };
        }

        public MemoryStream Assinar(MemoryStream arquivo)
        {
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
            X509Certificate[] chain = new X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }

            AssinaturaDigitalCore app = new AssinaturaDigitalCore();
            arquivo.Seek(0, SeekOrigin.Begin);
            var arquivoAssinado = app.Sign(arquivo, chain, pk, DigestAlgorithms.SHA256, PdfSigner.CryptoStandard.CMS, "<REASON>", "<LOCATION>");

            return arquivoAssinado;
        }
    }
}
