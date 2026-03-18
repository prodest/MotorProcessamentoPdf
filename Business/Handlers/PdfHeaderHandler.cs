using Infrastructure.Models;
using iText.Html2pdf;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;

namespace Business.Handlers
{
    public class PdfHeaderHandler : IEventHandler
    {
        private readonly string _html;
        private readonly PdfPageDefinition _page;

        public PdfHeaderHandler(string html, PdfPageDefinition page)
        {
            _html = html;
            _page = page;
        }

        public void HandleEvent(Event @event)
        {
            var docEvent = (PdfDocumentEvent)@event;
            var page = docEvent.GetPage();
            var pdf = docEvent.GetDocument();

            var pageSize = page.GetPageSize();

            var rectangle = new Rectangle(
                _page.MarginLeft,
                pageSize.GetTop() - _page.MarginTop,
                pageSize.GetWidth() - _page.MarginLeft - _page.MarginRight,
                _page.MarginTop
            );

            var canvas = new Canvas(
                new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf),
                rectangle
            );

            var elements = HtmlConverter.ConvertToElements(_html);

            foreach (var element in elements)
                canvas.Add((IBlockElement)element);

            canvas.Close();
        }
    }
}
