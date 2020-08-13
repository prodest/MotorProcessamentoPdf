using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using System.Collections.Generic;
using System.IO;

namespace Business.Core
{
    public class TransformaPdfCore
    {
        //public ICollection<byte[]> SeccionarPDF(byte[] arquivo)
        //{
        //    //PdfDocument pdfDocument = new PdfDocument(new PdfReader(RESOURCE));
        //    PdfDocument pdfDocument = new PdfDocument(new PdfReader(new MemoryStream(arquivo)));
        //    IList<PdfDocument> splitDocuments = new CustomPdfSplitter(pdfDocument, DEST).SplitByPageCount(2);
        //    foreach (PdfDocument doc in splitDocuments)
        //        doc.Close();
        //    pdfDocument.Close();
        //}

        //private class CustomPdfSplitter : PdfSplitter
        //{
        //    private string dest;
        //    private int partNumber = 1;

        //    public CustomPdfSplitter(PdfDocument pdfDocument, string dest) : base(pdfDocument)
        //    {
        //        this.dest = dest;
        //    }

        //    protected override PdfWriter GetNextPdfWriter(PageRange documentPageRange)
        //    {
        //        return new PdfWriter(string.Format(dest, partNumber++));
        //    }
        //}
    }
}
