using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("cars")]
    [Authorize(Roles = "Admin")]
    public class CarController : Controller
    {
        private static readonly string[] Cars =
        {
            "Polestar 2", "VW ID4", "Audi A6 E-tron", "Tesla Model S", "Skoda Enyaq"
        };

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return Cars.ToArray();
        }
    }
}