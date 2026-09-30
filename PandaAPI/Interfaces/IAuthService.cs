using PandaAPI.DTOs.Auth;

namespace PandaAPI.Interfaces
{
    public interface IAuthService
    {
        Task Register(RegisterDto dto);
        Task<LoginResponseDto?> Login(LoginDto dto);
    }
}
