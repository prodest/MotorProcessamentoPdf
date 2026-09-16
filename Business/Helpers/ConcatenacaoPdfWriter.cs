using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
    /// A limpeza é feita em FlushObject porque é o único ponto por onde todo objeto passa
    /// no momento da gravação. Varrer o documento antes do Close() não resolve, porque
    /// objetos entram na tabela de referências durante o próprio fechamento.
    /// </para>
    /// </summary>
    public class ConcatenacaoPdfWriter : PdfWriter
    {
        /// <summary>
        /// Profundidade máxima do percurso. Itens nulos aparecem em arrays que são filhos
        /// diretos de um dicionário, como /Filter, então poucos níveis bastam. O limite
        /// também protege contra ciclos entre objetos diretos no grafo em memória.
        /// </summary>
        private const int ProfundidadeMaxima = 6;

        public ConcatenacaoPdfWriter(Stream outputStream) : base(outputStream) { }

        /// <summary>
        /// Versão tolerante da verificação de filtro Flate do iText.
        /// <para>
        /// O original faz PdfArray.Contains no array de /Filter. Se esse array for um
        /// objeto indireto já gravado, seu conteúdo interno foi liberado e Contains lança
        /// NullReferenceException, derrubando o Close(). O mesmo ocorre se o array tiver
        /// item nulo.
        /// </para>
        /// <para>
        /// Quando não é possível inspecionar o array, a resposta é "já tem Flate". Isso faz
        /// o iText gravar o stream como está, sem recomprimir, que é o correto para um
        /// stream copiado de outro documento.
        /// </para>
        /// </summary>
        protected override bool ContainsFlateFilter(PdfStream pdfStream)
        {
            PdfObject filtro;
            try { filtro = pdfStream.Get(PdfName.Filter); }
            catch { return true; }

            if (filtro == null) return false;
            if (filtro.IsName()) return PdfName.FlateDecode.Equals(filtro);

            if (!(filtro is PdfArray array)) return true;

            // array já gravado: conteúdo liberado, não há o que inspecionar
            if (array.IsFlushed()) return true;

            int tamanho;
            try { tamanho = array.Size(); }
            catch { return true; }

            for (int indice = 0; indice < tamanho; indice++)
            {
                PdfObject item;
                try { item = array.Get(indice, false); }
                catch { continue; }

                if (item == null) continue;

                if (item.IsIndirectReference())
                {
                    try { item = ((PdfIndirectReference)item).GetRefersTo(true); }
                    catch { item = null; }
                }

                if (item != null && PdfName.FlateDecode.Equals(item)) return true;
            }

            return false;
        }

        protected override void FlushObject(PdfObject pdfObject, bool canBeInObjStm)
        {
            RemoverItensNulos(pdfObject, 0, null);
            base.FlushObject(pdfObject, canBeInObjStm);
        }

        /// <summary>
        /// Remove itens nulos dos arrays diretos do objeto. Não atravessa referências
        /// indiretas: cada objeto indireto é limpo quando chega a sua vez de ser gravado.
        /// </summary>
        /// <param name="visitados">
        /// Objetos já percorridos nesta chamada, comparados por identidade. Criado apenas
        /// quando o percurso desce de nível, para não alocar no caso comum.
        /// </param>
        private static void RemoverItensNulos(PdfObject pdfObject, int profundidade, HashSet<PdfObject> visitados)
        {
            if (pdfObject == null || profundidade > ProfundidadeMaxima) return;

            // só array e dicionário têm filhos; o resto é folha
            var ehArray = pdfObject is PdfArray;
            if (!ehArray && !(pdfObject is PdfDictionary)) return;

            // objeto já gravado tem o conteúdo interno liberado: acessá-lo lança
            if (pdfObject.IsFlushed()) return;

            if (visitados != null && !visitados.Add(pdfObject)) return;

            if (ehArray)
            {
                var array = (PdfArray)pdfObject;

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

                    if (item.IsIndirectReference()) continue;
                    if (!(item is PdfArray) && !(item is PdfDictionary)) continue;

                    RemoverItensNulos(item, profundidade + 1, GarantirConjunto(ref visitados, pdfObject));
                }

                return;
            }

            // PdfStream herda de PdfDictionary, então os dois caem aqui
            var dicionario = (PdfDictionary)pdfObject;

            foreach (var chave in dicionario.KeySet())
            {
                PdfObject valor;
                try { valor = dicionario.Get(chave, false); }
                catch { continue; }

                if (valor == null || valor.IsIndirectReference()) continue;
                if (!(valor is PdfArray) && !(valor is PdfDictionary)) continue;

                RemoverItensNulos(valor, profundidade + 1, GarantirConjunto(ref visitados, pdfObject));
            }
        }

        private static HashSet<PdfObject> GarantirConjunto(ref HashSet<PdfObject> visitados, PdfObject raiz)
        {
            if (visitados == null)
            {
                visitados = new HashSet<PdfObject>(ComparadorPorReferencia.Instancia) { raiz };
            }

            return visitados;
        }

        /// <summary>
        /// Compara PdfObject por identidade. ReferenceEqualityComparer não existe no
        /// netcoreapp3.1, alvo deste projeto.
        /// </summary>
        private sealed class ComparadorPorReferencia : IEqualityComparer<PdfObject>
        {
            public static readonly ComparadorPorReferencia Instancia = new ComparadorPorReferencia();

            public bool Equals(PdfObject x, PdfObject y) => ReferenceEquals(x, y);

            public int GetHashCode(PdfObject obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
