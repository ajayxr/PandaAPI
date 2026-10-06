using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.DTOs.Auth;
using PandaAPI.Interfaces;

namespace PandaAPI.Controllers
{

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
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            await _authService.Register(dto);

            return Ok("Usuário criado com sucesso");
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.Login(dto);

            if (result is null)
            {
                return Unauthorized(new { message = "Email ou senha inválidos." });
            }

            return Ok(result);
        }

        [HttpPost("guest")]
        [EnableRateLimiting("guest")]
        public async Task<IActionResult> Guest()
        {
            var result = await _authService.Guest();

            return Ok(result);
        }
    }
}
