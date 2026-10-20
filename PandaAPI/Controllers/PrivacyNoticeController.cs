using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PandaAPI.Controllers;

[ApiController]
[Route("privacy")]
[AllowAnonymous]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class PrivacyNoticeController : ControllerBase
{
    private const string ResourceName = "PandaAPI.Privacy.privacy-notice.md";

    [HttpGet]
    [Produces("text/markdown")]
    public IActionResult Get()
    {
        using var stream = typeof(PrivacyNoticeController).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return Problem("O aviso de privacidade não está disponível.");
        }

        using var reader = new StreamReader(stream);
        return Content(reader.ReadToEnd(), "text/markdown; charset=utf-8");
    }
}
