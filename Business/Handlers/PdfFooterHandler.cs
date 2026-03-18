using Infrastructure.Models;
using iText.Html2pdf;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Element;

namespace Business.Handlers
{
    public class PdfFooterHandler : IEventHandler
    {
        private readonly string _html;
        private readonly PdfPageDefinition _page;
        private readonly PdfFormXObject _totalPagesPlaceholder;

        public PdfFooterHandler(
            string html,
            PdfPageDefinition page,
            PdfFormXObject totalPagesPlaceholder)
        {
            _html = html;
            _page = page;
            _totalPagesPlaceholder = totalPagesPlaceholder;
        }

        public void HandleEvent(Event @event)
        {
            var docEvent = (PdfDocumentEvent)@event;
            var pdf = docEvent.GetDocument();
            var page = docEvent.GetPage();

            int pageNumber = pdf.GetPageNumber(page);

            var pageSize = page.GetPageSize();

            var rectangle = new Rectangle(
                _page.MarginLeft,
                10,
                pageSize.GetWidth() - _page.MarginLeft - _page.MarginRight,
                _page.MarginBottom
            );

            var canvas = new Canvas(new PdfCanvas(page), rectangle);

            // HTML do footer
            if (!string.IsNullOrWhiteSpace(_html))
            {
                var elements = HtmlConverter.ConvertToElements(_html);

                foreach (var element in elements)
                    canvas.Add((IBlockElement)element);
            }

            // PAGINAÇÃO
            var paragraph = new Paragraph()
                .Add("Página ")
                .Add(pageNumber.ToString())
                .Add(" de ");

            canvas.Add(paragraph);

            var pdfCanvas = new PdfCanvas(page);

            pdfCanvas.AddXObject(
                _totalPagesPlaceholder,
                rectangle.GetRight() - 30,
                rectangle.GetBottom() + 2
            );

            canvas.Close();
        }
    }
}
