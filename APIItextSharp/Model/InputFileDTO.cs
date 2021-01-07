using Microsoft.AspNetCore.Http;

namespace APIItextSharp.Model
{
    public class InputFileDTO
    {
        public string FileUrl { get; set; }
        public IFormFile FileBytes { get; set; }
    }
}
