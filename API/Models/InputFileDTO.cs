using Microsoft.AspNetCore.Http;

namespace API.Models
{
    public class InputFileDto
    {
        public string FileUrl { get; set; }
        public IFormFile FileBytes { get; set; }
    }
}
