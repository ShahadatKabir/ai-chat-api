using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("api/auth")]
public class TokenController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;

    public TokenController(IConfiguration configuration, IUserService userService)
    {
        _configuration = configuration;
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "No valid token found." });
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { error = "User not found." });
        }

        var token = GenerateJwtToken(user);
        return Ok(new { token, expiresIn = 3600 });
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken()
    {
        await Task.CompletedTask;
        return Ok(new { message = "Token revoked. Please login again." });
    }

    [AllowAnonymous]
    [HttpPost("validate")]
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
        {
            return BadRequest(new { error = "Token is required." });
        }

        try
        {
            var key = _configuration["Jwt:Key"] ?? "THIS_IS_SECRET_KEY_123";
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes(key);

            tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(tokenKey),
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Ok(new { valid = true });
        }
        catch
        {
            return Ok(new { valid = false });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? "THIS_IS_SECRET_KEY_123";
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenKey = Encoding.UTF8.GetBytes(key);
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(tokenKey),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}