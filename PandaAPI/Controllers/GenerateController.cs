using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.Services;

namespace PandaAPI.Controllers;

[EnableRateLimiting("validate")]
[Authorize]
[ApiController]
[Route("generate")]
[Tags("Generation")]
public class GenerateController : ControllerBase
{
    [HttpGet("cpf")]
    public IActionResult GenerateCpf()
    {
        return Ok(new { Cpf = CpfValidator.Generate() });
    }

    [HttpGet("cnpj/numeric")]
    public IActionResult GenerateNumericCnpj()
    {
        return Ok(new { Cnpj = CnpjValidator.GenerateNumeric() });
    }

    [HttpGet("cnpj/alphanumeric")]
    public IActionResult GenerateAlphanumericCnpj()
    {
        return Ok(new { Cnpj = CnpjValidator.GenerateAlphanumeric() });
    }
}