using Business.Core.ICore;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CarimboController : ControllerBase
    {
        private readonly ICarimboCore CarimboCore;

        public CarimboController(ICarimboCore carimboCore)
        {
            CarimboCore = carimboCore;
        }

        public void Copia()
        {
            CarimboCore.Teste();
            //CarimboCore.CarimboLateralDocumento("xxxx-xxxxxx", "documento original", DateTime.Now.ToString("dd/MM/yyyy hh:mm"));
            //CarimboCore.Copia("xxxx-xxxxxx", "Marcelo Lopes Rodrigues", DateTime.Now.ToString("dd/MM/yyyy hh:mm"), 5);
        }
    }
}
