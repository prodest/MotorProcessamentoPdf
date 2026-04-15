using Infrastructure.Models;
using iText.Html2pdf;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;

namespace Business.Handlers
{
    public class PdfFooterHandler : IEventHandler
    {
        private readonly string _html;
        private readonly PdfPageDefinition _page;

        public PdfFooterHandler(
            string html,
            PdfPageDefinition page)
        {
            _html = html;
            _page = page;
        }

        public void HandleEvent(Event @event)
        {
            var docEvent = (PdfDocumentEvent)@event;
            var pdf = docEvent.GetDocument();
            var page = docEvent.GetPage();

            int pageNumber = pdf.GetPageNumber(page);

            var pageSize = page.GetPageSize();

            var width = pageSize.GetWidth() - _page.MarginLeft - _page.MarginRight;

            var footerRectangle = new Rectangle(
                _page.MarginLeft,
                10,
                width,
                _page.FooterHeight
            );


            var canvas = new Canvas(new PdfCanvas(page), footerRectangle);

            // HTML do footer
            if (!string.IsNullOrWhiteSpace(_html))
            {
                var elements = HtmlConverter.ConvertToElements(_html);

                foreach (var element in elements)
                {
                    if (element is IBlockElement blockElement)
                        canvas.Add(blockElement);
                }
            }

            //AddPageNumber(pdf, page, pageNumber, pageSize);

            canvas.Close();
        }

        //private void AddPageNumber(PdfDocument pdf, PdfPage page, int pageNumber, Rectangle pageSize)
        //{
        //    var pdfCanvas = new PdfCanvas(page);

        //    var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        //    float fontSize = 8f;

        //    string pageText = $"Página {pageNumber} de ";

        //    float textWidth = font.GetWidth(pageText, fontSize);
        //    float placeholderWidth = font.GetWidth("999", fontSize); // reserva segura

        //    float x = pageSize.GetWidth() - _page.MarginRight - textWidth - placeholderWidth;
        //    float y = 20f;

        //    var canvas = new Canvas(pdfCanvas, pageSize);
        //    canvas.SetFont(font);
        //    canvas.SetFontSize(fontSize);

        //    canvas.ShowTextAligned(pageText, x, y, TextAlignment.LEFT);

        //    pdfCanvas.AddXObject(
        //        _totalPagesPlaceholder,
        //        x + textWidth,
        //        y
        //    );

        //    canvas.Close();
        //}
    }
}
