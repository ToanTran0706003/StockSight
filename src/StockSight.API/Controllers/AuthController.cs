using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockSight.Core.Models;
using StockSight.Infrastructure.Data;

namespace StockSight.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly StockSightDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(StockSightDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (email.Length == 0 || request.Password.Length < 6)
            return BadRequest("Email and a 6+ character password are required.");

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict("Email already exists.");

        var user = new User
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        _db.Portfolios.Add(new Portfolio
        {
            UserId = user.Id,
            OwnerId = user.Id.ToString(),
            Name = "Growth Portfolio",
            InitialCash = 10_000m,
            CashBalance = 10_000m,
            CreatedUtc = DateTime.UtcNow
        });

        foreach (var symbol in new[] { "AAPL", "MSFT", "GOOGL" })
            _db.WatchlistItems.Add(new WatchlistItem { UserId = user.Id, Symbol = symbol, AddedUtc = DateTime.UtcNow });

        await _db.SaveChangesAsync(ct);
        return Created(string.Empty, CreateResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return Unauthorized("Invalid email or password.");

        var valid = string.IsNullOrWhiteSpace(user.PasswordHash)
            ? request.Password == "demo"
            : BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!valid)
            return Unauthorized("Invalid email or password.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(CreateResponse(user));
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync([userId], ct);
        return user is null ? Unauthorized() : Ok(CreateResponse(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    private AuthResponse CreateResponse(User user)
    {
        var expires = DateTime.UtcNow.AddHours(4);
        var token = CreateToken(user, expires);
        return new AuthResponse(token, (int)(expires - DateTime.UtcNow).TotalSeconds, user.Id, user.Email, user.DisplayName);
    }

    private string CreateToken(User user, DateTime expires)
    {
        var secret = _configuration["Jwt:Secret"] ?? "stocksight-local-dev-secret-key-change-me";
        var issuer = _configuration["Jwt:Issuer"] ?? "StockSight";
        var audience = _configuration["Jwt:Audience"] ?? "StockSight.Web";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public record RegisterRequest(string Email, string Password, string DisplayName);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string AccessToken, int ExpiresIn, Guid UserId, string Email, string DisplayName);
}
