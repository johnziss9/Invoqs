using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Invoqs.Services
{
    public class AuthService : IAuthService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigation;

        public AuthService(IJSRuntime jsRuntime, NavigationManager navigation)
        {
            _jsRuntime = jsRuntime;
            _navigation = navigation;
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving auth token: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage: {ex.Message}");
            }

            _navigation.NavigateTo("/login", true);
        }

        public void AddAuthorizationHeader(HttpClient httpClient, string? token)
        {
            // Clear any existing authorization header
            httpClient.DefaultRequestHeaders.Authorization = null;
            
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}