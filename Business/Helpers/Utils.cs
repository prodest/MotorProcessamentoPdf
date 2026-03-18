using Infrastructure.Models;
using iText.Kernel.Geom;

namespace Business.Helpers
{
    static class Utils
    {
        public static PageSize GetPageSize(PdfPageDefinition page)
        {
            PageSize pageSize = PageSize.A4;

            switch (page.Size?.ToUpper())
            {
                case "A4":
                    pageSize = PageSize.A4;
                    break;

                case "A3":
                    pageSize = PageSize.A3;
                    break;

                case "LETTER":
                    pageSize = PageSize.LETTER;
                    break;
            }

            if (page.Orientation?.ToUpper() == "LANDSCAPE")
                pageSize = pageSize.Rotate();

            return pageSize;
        }

    }
}
