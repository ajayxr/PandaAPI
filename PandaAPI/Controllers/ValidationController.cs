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
        [HttpGet("cpf/{cpf}")]
        public IActionResult ValidateCpf(string cpf)
        {
            if (!CpfValidator.IsValid(cpf))
            {
                return BadRequest("CPF inválido.");
            }

            var cpfNormalizado = CpfValidator.Normalize(cpf);

            return Ok(new
            {
                Message = "CPF válido.",
                Cpf = cpfNormalizado
            });
        }

        [HttpGet("cnpj/{cnpj}")]
        public IActionResult ValidateCnpj(string cnpj)
        {
            if (!CnpjValidator.IsValid(cnpj))
            {
                return BadRequest("CNPJ inválido.");
            }

            var cnpjNormalizado = CnpjValidator.Normalize(cnpj);

            return Ok(new
            {
                Message = "CNPJ válido.",
                Cnpj = cnpjNormalizado
            });
        }
    }
}
