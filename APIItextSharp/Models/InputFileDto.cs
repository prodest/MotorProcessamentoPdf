using Microsoft.AspNetCore.Http;

namespace APIItextSharp.Models
{
    public class InputFileDto
    {
        public string FileUrl { get; set; }
        public IFormFile FileBytes { get; set; }
    }
}
