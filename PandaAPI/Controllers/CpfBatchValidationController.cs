using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.Services;

namespace PandaAPI.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("cpf-batch")]
[Route("cpf/validate")]
[Tags("Validation")]
public sealed class CpfBatchValidationController : ControllerBase
{
    private readonly CpfBatchValidationService _cpfBatchValidationService;

    public CpfBatchValidationController(CpfBatchValidationService cpfBatchValidationService)
    {
        _cpfBatchValidationService = cpfBatchValidationService;
    }

    [HttpPost("batch")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
    public async Task<IActionResult> ValidateBatch([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Arquivo não informado." });
        }

        try
        {
            var result = await _cpfBatchValidationService.ProcessAsync(file, cancellationToken);

            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (CpfBatchFileTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = exception.Message
            });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (NotSupportedException exception)
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new
            {
                message = exception.Message
            });
        }
    }
}
