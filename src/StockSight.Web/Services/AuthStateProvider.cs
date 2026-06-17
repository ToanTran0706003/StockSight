using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace StockSight.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _http;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private string? _token;

    public AuthStateProvider(HttpClient http) => _http = http;

    public AuthUser? CurrentUser { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(BuildPrincipal()));

    public async Task<AuthUser> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return await ApplyAuthResponseAsync(response);
    }

    public async Task<AuthUser> RegisterAsync(string email, string password, string displayName)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new { email, password, displayName });
        response.EnsureSuccessStatusCode();
        return await ApplyAuthResponseAsync(response);
    }

    public void Logout()
    {
        _token = null;
        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private async Task<AuthUser> ApplyAuthResponseAsync(HttpResponseMessage response)
    {
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Missing auth response.");

        _token = auth.AccessToken;
        CurrentUser = new AuthUser(auth.UserId, auth.Email, auth.DisplayName);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return CurrentUser;
    }

    private ClaimsPrincipal BuildPrincipal()
    {
        if (string.IsNullOrWhiteSpace(_token))
            return _anonymous;

        return new ClaimsPrincipal(new ClaimsIdentity(ParseClaims(_token), "jwt"));
    }

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = Convert.FromBase64String(PadBase64(payload.Replace('-', '+').Replace('_', '/')));
        using var document = JsonDocument.Parse(jsonBytes);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var type = property.Name switch
            {
                "nameid" => ClaimTypes.NameIdentifier,
                "email" => ClaimTypes.Email,
                "unique_name" => ClaimTypes.Name,
                _ => property.Name
            };
            yield return new Claim(type, property.Value.ToString());
        }
    }

    private static string PadBase64(string value)
    {
        var padding = value.Length % 4;
        return padding == 0 ? value : value + new string('=', 4 - padding);
    }

    private record AuthResponse(string AccessToken, int ExpiresIn, Guid UserId, string Email, string DisplayName);
}

public record AuthUser(Guid UserId, string Email, string DisplayName);
