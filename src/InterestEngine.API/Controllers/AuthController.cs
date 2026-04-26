using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InterestEngine.API.DTOs;

namespace InterestEngine.API.Controllers;

/// <summary>
/// Issues JWT tokens for API authentication.
/// Demo credentials: admin / Admin@123 or viewer / Viewer@123
/// In production: replace with a proper identity provider.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    // Hardcoded demo users — replace with DB lookup in production
    private static readonly Dictionary<string, (string Password, string Role)> _users = new()
    {
        { "admin",  ("Admin@123",  "Admin")  },
        { "viewer", ("Viewer@123", "Viewer") }
    };

    public AuthController(IConfiguration config) => _config = config;

    /// <summary>
    /// Authenticates a user and returns a JWT bearer token.
    /// Use the returned token as: Authorization: Bearer {token}
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!_users.TryGetValue(request.Username.ToLower(), out var user)
            || user.Password != request.Password)
        {
            return Unauthorized(new ApiErrorResponse("Unauthorized", "Invalid username or password."));
        }

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(2);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims:   claims,
            expires:  expires,
            signingCredentials: creds);

        return Ok(new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            request.Username,
            expires));
    }
}
