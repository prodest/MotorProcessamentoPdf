using System.Collections.Generic;

namespace Business.Shared.Models
{
    public class ValidationsSelector
    {
        public bool IsPdf { get; set; }
        public bool PossuiRestricoesLeituraOuAlteracao { get; set; }
        public bool PossuiAssinaturaDigital { get; set; }
        public RegularExpressionsParameters RegularExpressionsParameters { get; set; }
        public bool PdfInfo { get; set; }
    }

    public class RegularExpressionsParameters
    {
        public IEnumerable<string> ExpressoesRegulares { get; set; }
        public IList<int> Paginas { get; set; }
    }
}
