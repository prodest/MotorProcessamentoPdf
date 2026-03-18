using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;

namespace Business.Handlers
{
    public class PdfWatermarkHandler : IEventHandler
    {
        private readonly string _text;

        public PdfWatermarkHandler(string text)
        {
            _text = text;
        }

        public void HandleEvent(Event @event)
        {
            var docEvent = (PdfDocumentEvent)@event;

            var page = docEvent.GetPage();
            var pdf = docEvent.GetDocument();

            var pageSize = page.GetPageSize();

            var canvas = new Canvas(new PdfCanvas(page), pageSize);

            var paragraph = new Paragraph(_text)
                .SetFontSize(60)
                .SetFontColor(ColorConstants.GRAY)
                .SetOpacity(0.2f);

            canvas.ShowTextAligned(
                paragraph,
                pageSize.GetWidth() / 2,
                pageSize.GetHeight() / 2,
                pdf.GetPageNumber(page),
                TextAlignment.CENTER,
                VerticalAlignment.MIDDLE,
                (float)(Math.PI / 6)
            );

            canvas.Close();
        }
    }
}
