﻿namespace Business.Shared.Models
{
    public class ValidationsResult
    {
        public bool? IsPdf { get; set; }
        public bool? PossuiRestricoesLeituraOuAlteracao { get; set; }
        public bool? PossuiAssinaturaDigital { get; set; }
        public bool? PossuiCarimboEdocs { get; set; }
        public PdfInfo PdfInfo { get; set; }
    }
}
