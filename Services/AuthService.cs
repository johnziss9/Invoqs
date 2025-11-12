using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

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
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, return null
                return null;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                // Prerendering or component being disposed, return null
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving auth token: {ex.Message}");
                return null;
            }
        }

        public async Task<(string FirstName, string LastName)?> GetCurrentUserAsync()
        {
            try
            {
                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "currentUser");
                
                if (string.IsNullOrEmpty(userJson))
                    return null;

                var userDoc = JsonDocument.Parse(userJson);
                var firstName = userDoc.RootElement.GetProperty("FirstName").GetString() ?? "";
                var lastName = userDoc.RootElement.GetProperty("LastName").GetString() ?? "";

                return (firstName, lastName);
            }
            catch (JSDisconnectedException)
            {
                return null;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving current user: {ex.Message}");
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
            catch (JSDisconnectedException)
            {
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage: {ex.Message}");
            }

            _navigation.NavigateTo("/login", true);
        }

        public void AddAuthorizationHeader(HttpClient httpClient, string? token)
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}