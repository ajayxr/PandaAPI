using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using PandaAPI.Services;
using PandaAPI.DTOs.Validation;

namespace PandaAPI.Controllers
{

    [EnableRateLimiting("validate")]
    [Authorize]
    [ApiController]
    [Route("validate")]
    [Tags("Validation")]
    public class ValidationController : ControllerBase
    {
        [HttpPost("cpf")]
        public IActionResult ValidateCpf([FromBody] ValidateCpfDto dto)
        {
            var cpf = dto.Cpf;
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

        [HttpPost("cnpj")]
        public IActionResult ValidateCnpj([FromBody] ValidateCnpjDto dto)
        {
            var cnpj = dto.Cnpj;
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
