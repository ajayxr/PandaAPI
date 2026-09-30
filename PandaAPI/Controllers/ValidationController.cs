using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using PandaAPI.Services;

namespace PandaAPI.Controllers
{

    [EnableRateLimiting("validate")]
    [Authorize]
    [ApiController]
    [Route("validate")]
    [Tags("Validation")]
    public class ValidationController : ControllerBase
    {
        private readonly CpfService _cpfService;
        
        public ValidationController(CpfService cpfService)
        {
            _cpfService = cpfService;
        }

        [HttpGet("cpf/{cpf}")]
        public IActionResult ValidateCpf(string cpf)
        {
            var cpfNormalizado = _cpfService.Normalize(cpf);

            if (!_cpfService.IsNumeric(cpfNormalizado) || cpfNormalizado.Length != 11)
            {
                return BadRequest("CPF deve conter apenas números.");
            }
            return Ok(new
            {
                Message = "CPF válido",
                Cpf = cpfNormalizado
            });
        }
    }
}
