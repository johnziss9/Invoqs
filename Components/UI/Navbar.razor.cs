using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Invoqs.Components.UI
{
    public partial class Navbar : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        
        [Parameter] public string CurrentUser { get; set; } = "John Doe";
        [Parameter] public EventCallback OnLogout { get; set; }

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