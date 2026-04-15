namespace Infrastructure.Models
{
    public class PdfPageDefinition
    {
        public string Size { get; set; } = "A4";

        public string Orientation { get; set; } = "Portrait";

        public float MarginTop { get; set; } = 40;
        public float MarginBottom { get; set; } = 40;
        public float MarginLeft { get; set; } = 40;
        public float MarginRight { get; set; } = 40;

        public float HeaderHeight { get; set; } = 60;
        public float FooterHeight { get; set; } = 40;

        public static PdfPageDefinition Default()
        {
            return new PdfPageDefinition();
        }
    }
}
