using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.DTOs.Auth;
using PandaAPI.Interfaces;

namespace PandaAPI.Controllers
{

    [EnableRateLimiting("register")]
    [ApiController]
    [Route("auth")]
    [Tags("Authentication")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            await _authService.Register(dto);

            return Ok("Usuário criado com sucesso");
        }
    }
}
