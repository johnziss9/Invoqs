using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;

namespace Invoqs.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        private LoginModel loginModel = new();
        private bool isLoggingIn = false;
        private string errorMessage = "";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var token = await GetStoredTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    Navigation.NavigateTo("/dashboard", true);
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
                        await StoreTokenAsync(authResponse.Token);
                        await StoreUserInfoAsync(authResponse.User);
                        Navigation.NavigateTo("/dashboard", true);
                    }
                    else
                    {
                        errorMessage = "Invalid response from server. Please try again.";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    errorMessage = "Invalid email or password. Please check your credentials and try again.";
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Login failed - Status: {response.StatusCode}, Content: {responseContent}");
                    errorMessage = "Unable to connect to the server. Please try again later.";
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error during login: {ex.Message}");
                errorMessage = "Unable to connect to the server. Please check your internet connection and try again.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during login: {ex.Message}");
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
            errorMessage = "Please check the form for errors and try again.";
            StateHasChanged();
        }

        private async Task StoreTokenAsync(string token)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing auth token: {ex.Message}");
            }
        }

        private async Task<string?> GetStoredTokenAsync()
        {
            try
            {
                return await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving auth token: {ex.Message}");
                return null;
            }
        }

        private async Task StoreUserInfoAsync(UserInfo user)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", System.Text.Json.JsonSerializer.Serialize(user));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing user info: {ex.Message}");
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

        public class UserInfo
        {
            public int Id { get; set; }
            public string Email { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string FullName { get; set; } = "";
        }
    }
}