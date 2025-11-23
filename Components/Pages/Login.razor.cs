using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using Invoqs.Models;
using System.Text.Json;
using System.Net;

namespace Invoqs.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ILogger<Login> Logger { get; set; } = default!;

        private LoginModel loginModel = new();
        private bool isLoggingIn = false;
        private string errorMessage = "";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    Logger.LogInformation("Login page initialized");
                    
                    var token = await GetStoredTokenAsync();
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        Logger.LogInformation("Existing authentication token found, redirecting to dashboard");
                        Navigation.NavigateTo("/dashboard", true);
                    }
                    else
                    {
                        Logger.LogDebug("No existing authentication token found");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error during login page initialization");
                }
            }
        }

        private async Task HandleLogin()
        {
            try
            {
                isLoggingIn = true;
                errorMessage = "";
                StateHasChanged();

                Logger.LogInformation("Login attempt started for email: {Email}", loginModel.Email);

                var response = await HttpClient.PostAsJsonAsync("auth/login", new
                {
                    Email = loginModel.Email,
                    Password = loginModel.Password
                });

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                    if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                    {
                        Logger.LogInformation("Login successful for email: {Email}, User: {UserId}", 
                            loginModel.Email, 
                            authResponse.User?.Id ?? 0);

                        await StoreTokenAsync(authResponse.Token);
                        await StoreUserInfoAsync(authResponse.User!);
                        
                        Logger.LogInformation("Authentication token and user info stored, redirecting to dashboard");
                        Navigation.NavigateTo("/dashboard", true);
                    }
                    else
                    {
                        Logger.LogWarning("Login response was successful but contained invalid data for email: {Email}", 
                            loginModel.Email);
                        errorMessage = "Invalid response from server. Please try again.";
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Logger.LogWarning("Login failed - Invalid credentials for email: {Email}", loginModel.Email);
                    errorMessage = "Invalid email or password. Please check your credentials and try again.";
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Logger.LogError("Login failed for email: {Email} - Status: {StatusCode}, Response: {Response}", 
                        loginModel.Email, 
                        response.StatusCode, 
                        responseContent);
                    errorMessage = "Unable to connect to the server. Please try again later.";
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "HTTP error during login attempt for email: {Email}", loginModel.Email);
                errorMessage = "Unable to connect to the server. Please check your internet connection and try again.";
            }
            catch (JSException ex)
            {
                Logger.LogError(ex, "JavaScript interop error during login for email: {Email}", loginModel.Email);
                errorMessage = "An error occurred while saving your session. Please try again.";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during login attempt for email: {Email}", loginModel.Email);
                errorMessage = "An unexpected error occurred. Please try again.";
            }
            finally
            {
                isLoggingIn = false;
                StateHasChanged();
            }
        }

        private void HandleInvalidSubmit()
        {
            Logger.LogDebug("Login form validation failed");
            errorMessage = "Please check the form for errors and try again.";
            StateHasChanged();
        }

        private async Task StoreTokenAsync(string token)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
                Logger.LogDebug("Authentication token stored successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error storing authentication token in localStorage");
                throw; // Re-throw to be handled by calling method
            }
        }

        private async Task<string?> GetStoredTokenAsync()
        {
            try
            {
                var token = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
                Logger.LogDebug("Retrieved stored authentication token: {TokenExists}", !string.IsNullOrEmpty(token));
                return token;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving authentication token from localStorage");
                return null;
            }
        }

        private async Task StoreUserInfoAsync(UserInfo user)
        {
            try
            {
                var userJson = JsonSerializer.Serialize(user);
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", userJson);
                Logger.LogDebug("User info stored successfully for user: {UserId}", user?.Id ?? 0);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error storing user info in localStorage for user: {UserId}", user?.Id ?? 0);
                throw; // Re-throw to be handled by calling method
            }
        }

        // Models for the login process
        public class LoginModel
        {
            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Password is required")]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
            public string Password { get; set; } = "";
        }

        public class AuthResponse
        {
            public string Token { get; set; } = "";
            public UserInfo User { get; set; } = new();
        }
    }
}