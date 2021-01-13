namespace Infrastructure.Models
{
    public class PessoaJuridicaDto
    {
        public string CNPJ { get; set; }
        public string RazaoSocial { get; set; }
        public string INSS { get; set; }
        public PessoaFisicaDto Responsavel { get; set; }
    }
}
