using FinancialChat.Infrastructure.Services;
using FinancialChat.Core.Interfaces;
using FinancialChat.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinancialChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation($"Registration attempt for user: {request.Username}");

            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "All fields are required" });
            }

            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Username already exists" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                var token = _jwtService.GenerateToken(user);
                _logger.LogInformation($"User {request.Username} registered successfully");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    Username = user.UserName,
                    Email = user.Email
                });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning($"Registration failed for {request.Username}: {errors}");

            return BadRequest(new { message = "Registration failed", errors = result.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during registration for {request.Username}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation($"Login attempt for user: {request.Username}");

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning($"Login failed: User {request.Username} not found");
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (result.Succeeded)
            {
                var token = _jwtService.GenerateToken(user);
                _logger.LogInformation($"User {request.Username} logged in successfully");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    Username = user.UserName,
                    Email = user.Email
                });
            }

            _logger.LogWarning($"Login failed for {request.Username}: Invalid password");
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during login for {request.Username}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Token refreshed",
                Token = token,
                Username = user.UserName,
                Email = user.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                createdAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

// Request/Response Models
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
}