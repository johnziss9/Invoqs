using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Invoqs.Components.UI
{
    public partial class EmailTagInput : ComponentBase
    {
        [Parameter] public List<string> Emails { get; set; } = new();
        [Parameter] public EventCallback<List<string>> EmailsChanged { get; set; }
        [Parameter] public string Placeholder { get; set; } = "Προσθήκη διευθύνσεων email...";
        [Parameter] public bool ReadOnly { get; set; } = false;

        private string currentInput = string.Empty;
        private string errorMessage = string.Empty;
        private bool hasError = false;

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == "," || e.Key == "Tab")
            {
                await AddCurrentEmail();
            }
            else if (e.Key == "Backspace" && string.IsNullOrEmpty(currentInput) && Emails.Any())
            {
                RemoveEmail(Emails.Last());
            }
        }

        private async Task HandleBlur()
        {
            if (!string.IsNullOrWhiteSpace(currentInput))
            {
                await AddCurrentEmail();
            }
        }

        private async Task AddCurrentEmail()
        {
            var email = currentInput.Trim().TrimEnd(',');

            if (string.IsNullOrWhiteSpace(email))
            {
                currentInput = string.Empty;
                return;
            }

            // Validate email format
            if (!IsValidEmail(email))
            {
                errorMessage = "Invalid email format";
                hasError = true;
                return;
            }

            // Check for duplicates
            if (Emails.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "Email already added";
                hasError = true;
                return;
            }

            // Add email
            Emails.Add(email);
            currentInput = string.Empty;
            errorMessage = string.Empty;
            hasError = false;

            await EmailsChanged.InvokeAsync(Emails);
        }

        private async void RemoveEmail(string email)
        {
            Emails.Remove(email);
            errorMessage = string.Empty;
            hasError = false;
            await EmailsChanged.InvokeAsync(Emails);
            StateHasChanged();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}