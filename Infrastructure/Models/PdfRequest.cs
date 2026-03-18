namespace Infrastructure.Models
{
    public class PdfRequest
    {
        public string HtmlBody { get; set; } = string.Empty;

        public string? HtmlHeader { get; set; }

        public string? HtmlFooter { get; set; }

        public string? HtmlWatermark { get; set; }

        public PdfPageDefinition? Page { get; set; }
    }
}
