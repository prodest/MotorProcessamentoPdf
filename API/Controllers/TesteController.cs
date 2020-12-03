using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TesteController : ControllerBase
    {
        [HttpGet("{time}/[action]")]
        public void Sleep(int time)
        {
            Thread.Sleep(time);
        }
    }
}
