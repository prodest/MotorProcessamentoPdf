using System;

namespace Infrastructure.Models
{
    public class PessoaFisicaDto
    {
        public string Nome { get; set; }
        public DateTime DataNascimento { get; set; }
        public string CPF { get; set; }
        public string NIS { get; set; }
        public string RG { get; set; }
        public string OrgaoExpedidor { get; set; }
        public string Inss { get; set; }
        public string TituloEleitor { get; set; }
        public string ZonaEleitoral { get; set; }
        public string Secao { get; set; }
        public string Municipio { get; set; }
    }
}
