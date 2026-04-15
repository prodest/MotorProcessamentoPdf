using Infrastructure.Models;
using iText.Html2pdf;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Business.Handlers
{
    public class PdfHeaderHandler : IEventHandler
    {
        private readonly string _html;
        private readonly PdfPageDefinition _page;
        private readonly PdfFormXObject _totalPagesPlaceholder;

        public PdfHeaderHandler(string html, PdfPageDefinition page, PdfFormXObject totalPagesPlaceholder)
        {
            _html = html;
            _page = page;
            _totalPagesPlaceholder = totalPagesPlaceholder;
        }

        public void HandleEvent(Event @event)
        {
            var docEvent = (PdfDocumentEvent)@event;
            var page = docEvent.GetPage();
            var pdf = docEvent.GetDocument();

            int pageNumber = pdf.GetPageNumber(page);
            var pageSize = page.GetPageSize();

            var totalWidth = pageSize.GetWidth() - _page.MarginLeft - _page.MarginRight;

            var headerRectangle = new Rectangle(
                _page.MarginLeft,
                pageSize.GetTop() - _page.HeaderHeight,
                totalWidth,
                _page.HeaderHeight
            );

            var canvas = new Canvas(new PdfCanvas(page), headerRectangle);

            if (!string.IsNullOrWhiteSpace(_html))
            {
                var elements = HtmlConverter.ConvertToElements(_html);

                foreach (var element in elements)
                {
                    if (element is IBlockElement blockElement)
                        canvas.Add(blockElement);
                }
            }

            canvas.Close();

            AddPageNumber(pdf, page, pageNumber, pageSize);
        }

        private void AddPageNumber(PdfDocument pdf, PdfPage page, int pageNumber, Rectangle pageSize)
        {
            var pdfCanvas = new PdfCanvas(page);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            float fontSize = 7f;

            string pageText = $"Página {pageNumber} de ";

            float textWidth = font.GetWidth(pageText, fontSize);
            float placeholderWidth = font.GetWidth("999", fontSize);

            float x = pageSize.GetWidth() - _page.MarginRight - textWidth - placeholderWidth;
            float y = pageSize.GetTop() - 14f;

            var canvas = new Canvas(pdfCanvas, pageSize);
            canvas.SetFont(font);
            canvas.SetFontSize(fontSize);
            canvas.SetFontColor(new DeviceRgb(107, 114, 128)); // equivalente o #6b7280 - cinza
            canvas.ShowTextAligned(pageText, x, y, TextAlignment.LEFT);

            pdfCanvas.AddXObject(
                _totalPagesPlaceholder,
                x + textWidth,
                y
            );

            canvas.Close();
        }
    }
}
