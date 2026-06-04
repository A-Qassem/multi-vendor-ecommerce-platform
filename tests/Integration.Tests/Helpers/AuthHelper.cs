using System.Net.Http.Json;
using MultiVendor.Ecommerce.Application.DTOs.Auth;

namespace MultiVendor.Ecommerce.Integration.Tests.Helpers;

public static class AuthHelper
{
    public static async Task<string> GetTokenAsync(HttpClient client, string email, string password)
    {
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }

    public static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string name, string email, string password)
    {
        var registerRequest = new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
