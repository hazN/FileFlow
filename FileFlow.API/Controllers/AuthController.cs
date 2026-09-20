using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileFlow.API.Data;
using FileFlow.API.Models;
using FileFlow.API.DTOs;
using FileFlow.API.Services;

namespace FileFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FileFlowDbContext _context;
        private readonly JwtTokenService _tokenService;
        private readonly PasswordHasher<User> _passwordHasher = new();
        public AuthController(FileFlowDbContext context, JwtTokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            if (request.Password.Length < 8)
            {
                return BadRequest("Password must be at least 8 characters long.");
            }

            bool emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailTaken)
            {
                return BadRequest("Email already in use.");
            }

            string passwordHash = _passwordHasher.HashPassword(null!, request.Password);

            User user;
            try
            {
                user = new User(request.Email, passwordHash);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);
            return Ok(new AuthResponse { Token = token, Email = user.Email });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid email or password.");
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new AuthResponse { Token = token, Email = user.Email });
        }
    }
}