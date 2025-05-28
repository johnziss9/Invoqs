using Microsoft.AspNetCore.Components;

namespace Invoqs.Components.UI
{
    public partial class Navbar : ComponentBase
    {
        [Parameter] public string CurrentUser { get; set; } = "John Doe";
        [Parameter] public EventCallback OnLogout { get; set; }

        private async Task HandleLogout()
        {
            // TODO Add logout logic here
            await OnLogout.InvokeAsync();
        }
    }
}