using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.Services;

namespace PandaAPI.Controllers
{
    [EnableRateLimiting("validate")]
    [Authorize]
    [ApiController]
    [Route("cnpj")]
    [Tags("CNPJ")]
    public class GetCnpjController(GetCnpjService getCnpjService, CnpjPdfService cnpjPdfService) : ControllerBase
    {
        [HttpGet("{cnpj}")]
        public async Task<IActionResult> GetCnpj(string cnpj, CancellationToken cancellationToken)
        {
            if (!CnpjValidator.IsValid(cnpj))
            {
                return BadRequest(new { message = "CNPJ inválido." });
            }

            try
            {
                var result = await getCnpjService.GetCnpjAsync(cnpj, cancellationToken);
                return Ok(result);
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new { message = "Estabelecimento não encontrado no cnpj.ai." });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "O cnpj.ai não conseguiu atender à consulta." });
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    new { message = "O cnpj.ai demorou para responder." });
            }
            catch (JsonException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "O cnpj.ai retornou uma resposta inválida." });
            }
        }

        [HttpGet("{cnpj}/pdf")]
        public async Task<IActionResult> GetCnpjPdf(string cnpj, CancellationToken cancellationToken)
        {
            if (!CnpjValidator.IsValid(cnpj))
            {
                return BadRequest(new { message = "CNPJ inválido." });
            }

            try
            {
                var result = await getCnpjService.GetCnpjAsync(cnpj, cancellationToken);
                var pdf = cnpjPdfService.GenerateReport(result, cnpj);

                return Ok(new
                {
                    fileName = $"cnpj-{CnpjValidator.Normalize(cnpj)}.pdf",
                    contentType = "application/pdf",
                    base64 = Convert.ToBase64String(pdf)
                });
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new { message = "Estabelecimento não encontrado no cnpj.ai." });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "O cnpj.ai não conseguiu atender à consulta." });
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    new { message = "O cnpj.ai demorou para responder." });
            }
            catch (JsonException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "O cnpj.ai retornou uma resposta inválida." });
            }
        }
    }
}
