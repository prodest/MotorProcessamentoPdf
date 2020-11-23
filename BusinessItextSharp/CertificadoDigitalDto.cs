using Business.Helpers.AssinaturaDigital;
using System;

namespace BusinessItextSharp
{
    public class CertificadoDigitalDto
    {
        public DateTime ValidoAPartir { get; private set; }
        public DateTime ValidoAte { get; private set; }
        public DateTime Pkcs7SignDate { get; private set; }
        public bool IcpBrasil { get; private set; }
        public bool CadeiaValida { get; private set; }
        public bool PeriodoValido { get; private set; }
        public PessoaFisica PessoaFisica { get; private set; }
        public PessoaJuridica PessoaJuridica { get; private set; }
    }
}
