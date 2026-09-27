using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PandaAPI.Controllers
{
    [EnableRateLimiting("default")]
    [ApiController]
    [Route("validate")]
    public class ValidateController : ControllerBase
    {
        [HttpGet("cpf/{cpf}")]
        public IActionResult ValidateCpf(string cpf)
        {
            return Ok(new
            {
                Message = "Li seu CPF",
                Cpf = cpf
            });
        }
    }
}
