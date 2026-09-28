using PandaAPI.DTOs.Auth;

namespace PandaAPI.Interfaces
{
    public interface IAuthService
    {
        Task Register(RegisterDto dto);
    }
}
