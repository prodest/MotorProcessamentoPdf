using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace API.Controllers
{
    public class TesteController : BaseApiController
    {
        [HttpGet("{time}/[action]")]
        public void Sleep(int time)
        {
            Thread.Sleep(time);
        }
    }
}
