using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using PandaAPI.Data;
using PandaAPI.DTOs.Auth;
using PandaAPI.Interfaces;
using PandaAPI.Models;


namespace PandaAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Register(RegisterDto dto)
        {
            var userExists = await _context.Users
            .AnyAsync(x => x.Email == dto.Email);

            if (userExists)
            {
                throw new Exception("Email já cadastrado.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();
        }
    }
}
