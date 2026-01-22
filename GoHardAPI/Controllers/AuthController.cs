using Asp.Versioning;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Repositories;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")]  // Backwards compatibility for clients not using versioned URLs
    [ApiController]
    [EnableRateLimiting("auth")]  // SECURITY: Rate limit auth endpoints to prevent brute force
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthService _authService;

        public AuthController(IUserRepository userRepository, AuthService authService)
        {
            _userRepository = userRepository;
            _authService = authService;
        }

        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponse>> Signup(SignupRequest request)
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                return BadRequest(new { message = "Email already registered" });
            }

            // Hash the password
            var passwordHash = _authService.HashPassword(request.Password);

            // Create new user
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                DateCreated = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Generate JWT token
            var token = _authService.GenerateJwtToken(user);

            return Ok(new AuthResponse(token, user.Id, user.Name, user.Email));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Verify password
            if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Account is deactivated" });
            }

            // Update last login date
            user.LastLoginDate = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();

            // Generate JWT token
            var token = _authService.GenerateJwtToken(user);

            return Ok(new AuthResponse(token, user.Id, user.Name, user.Email));
        }
    }
}
