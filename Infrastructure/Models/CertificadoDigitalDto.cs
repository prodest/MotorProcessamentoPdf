using System;

namespace Infrastructure.Models
{
    public class CertificadoDigitalDto
    {
        public DateTime ValidoAPartir { get; set; }
        public DateTime ValidoAte { get; set; }
        public DateTime Pkcs7SignDate { get; set; }
        public PessoaFisicaDto PessoaFisica { get; set; }
        public PessoaJuridicaDto PessoaJuridica { get; set; }
    }
}
