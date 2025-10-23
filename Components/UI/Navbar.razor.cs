using Invoqs.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Invoqs.Components.UI
{
    public partial class Navbar : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter] public EventCallback<string> OnUserLoaded { get; set; }
        
        private string currentUser = "User";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LoadCurrentUserAsync();
                StateHasChanged();
            }
        }

        private async Task LoadCurrentUserAsync()
        {
            try
            {
                var storedUserJson = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "currentUser");
                
                if (!string.IsNullOrEmpty(storedUserJson))
                {
                    var userInfo = System.Text.Json.JsonSerializer.Deserialize<UserInfo>(storedUserJson);
                    
                    if (userInfo != null && !string.IsNullOrEmpty(userInfo.FirstName))
                    {
                        currentUser = userInfo.FirstName;
                        await OnUserLoaded.InvokeAsync(currentUser);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading current user: {ex.Message}");
                currentUser = "User";
            }
        }

        protected async Task HandleLogout()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                // Prerendering, ignore localStorage calls
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage during logout: {ex.Message}");
            }

            // Always redirect to login, even if localStorage clearing fails
            Navigation.NavigateTo("/login", true);
        }
    }
}