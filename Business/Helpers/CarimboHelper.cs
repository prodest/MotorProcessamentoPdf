using System;
using System.Linq;

namespace Business.Helpers
{
    public static class CarimboDocumentoHelper
    {
        private enum DocumentoNatureza
        {
            Natodigital = 1,
            Digitalizado = 2
        }

        private enum DocumentoValorLegal
        {
            Original = 1,
            CopiaAutenticadaCartorio = 2,
            CopiaAutenticadaAdministrativamente = 3,
            CopiaSimples = 4
        }

        public static string CarimboDocumento(int natureza, int valorLegal)
        {
            if (!Enum.GetValues(typeof(DocumentoNatureza)).Cast<int>(). ToList().Contains(natureza))
                throw new Exception("A natureza do documento informado não existe.");

            if (!Enum.GetValues(typeof(DocumentoValorLegal)).Cast<int>().ToList().Contains(valorLegal))
                throw new Exception("O valor legal do documento informado não existe.");

            if (natureza == (int)DocumentoNatureza.Natodigital && valorLegal == (int)DocumentoValorLegal.Original)
                return "documento original";
            else if (natureza == (int)DocumentoNatureza.Digitalizado && valorLegal == (int)DocumentoValorLegal.Original)
                return "cópia autenticada administrativamente";
            else
                return "cópia simples";
        }
    }
}
