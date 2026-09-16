using System.Collections.Generic;
using System.IO;
using iText.Kernel.Pdf;

namespace Business.Helpers
{
    /// <summary>
    /// PdfWriter tolerante a arrays com itens nulos, usado na concatenação de PDFs.
    /// <para>
    /// Ao concatenar, alguns documentos produzem no arquivo de saída arrays com item
    /// nulo, tipicamente na entrada /Filter de um stream. O iText 7.2.0 não trata esse
    /// caso: PdfArrayDirectIterator.MoveNext chama IsIndirectReference no item e lança
    /// NullReferenceException. A falha só aparece no Close(), quando os objetos copiados
    /// são gravados, e derruba a concatenação inteira.
    /// </para>
    /// <para>
    /// A limpeza é feita aqui, em FlushObject, porque é o único ponto por onde todo
    /// objeto passa no momento da gravação. Varrer o documento antes do Close() não
    /// resolve: objetos entram na tabela de referências durante o próprio fechamento.
    /// </para>
    /// </summary>
    public class ConcatenacaoPdfWriter : PdfWriter
    {
        public ConcatenacaoPdfWriter(Stream outputStream) : base(outputStream) { }

        protected override void FlushObject(PdfObject pdfObject, bool canBeInObjStm)
        {
            RemoverItensNulos(pdfObject, 0);
            base.FlushObject(pdfObject, canBeInObjStm);
        }

        /// <summary>
        /// Remove itens nulos de arrays diretos do objeto. Não atravessa referências
        /// indiretas: cada objeto indireto é limpo quando chega a sua vez de ser gravado.
        /// </summary>
        private static void RemoverItensNulos(PdfObject pdfObject, int profundidade)
        {
            // profundidade limitada por segurança contra estruturas patológicas
            if (pdfObject == null || profundidade > 32) return;

            // objeto já gravado tem o conteúdo interno liberado: acessá-lo lança
            if (pdfObject.IsFlushed()) return;

            if (pdfObject is PdfArray array)
            {
                for (int indice = array.Size() - 1; indice >= 0; indice--)
                {
                    PdfObject item;
                    try { item = array.Get(indice, false); }
                    catch { continue; }

                    if (item == null)
                    {
                        try { array.Remove(indice); } catch { /* array em estado inesperado */ }
                        continue;
                    }

                    if (!item.IsIndirectReference()) RemoverItensNulos(item, profundidade + 1);
                }

                return;
            }

            // PdfStream herda de PdfDictionary, então os dois caem aqui
            if (pdfObject is PdfDictionary dicionario)
            {
                foreach (var chave in new List<PdfName>(dicionario.KeySet()))
                {
                    PdfObject valor;
                    try { valor = dicionario.Get(chave, false); }
                    catch { continue; }

                    if (valor == null || valor.IsIndirectReference()) continue;

                    RemoverItensNulos(valor, profundidade + 1);
                }
            }
        }
    }
}
