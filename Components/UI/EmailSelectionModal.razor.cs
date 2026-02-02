using Microsoft.AspNetCore.Components;

namespace Invoqs.Components.UI;

public partial class EmailSelectionModal
{
    // --- Parameters ---

    [Parameter] public List<string> AvailableEmails { get; set; } = new();
    [Parameter] public string CustomerName { get; set; } = string.Empty;
    /// <summary>
    /// Document type in Greek accusative: "το τιμολόγιο" or "την απόδειξη"
    /// </summary>
    [Parameter] public string DocumentType { get; set; } = "το έγγραφο";
    [Parameter] public EventCallback<List<string>> OnEmailsSelected { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    // --- State ---

    private List<string> SelectedEmails { get; set; } = new();

    // --- Lifecycle ---

    protected override void OnInitialized()
    {
        // All emails selected by default
        SelectedEmails = new List<string>(AvailableEmails);
    }

    // --- Email toggle logic ---

    private bool IsEmailSelected(string email)
    {
        return SelectedEmails.Contains(email);
    }

    private void ToggleEmail(string email)
    {
        if (SelectedEmails.Contains(email))
            SelectedEmails.Remove(email);
        else
            SelectedEmails.Add(email);

        StateHasChanged();
    }

    private void SelectAll()
    {
        SelectedEmails = new List<string>(AvailableEmails);
        StateHasChanged();
    }

    private void DeselectAll()
    {
        SelectedEmails.Clear();
        StateHasChanged();
    }

    // --- Modal actions ---

    private async Task OnConfirm()
    {
        if (SelectedEmails.Count > 0)
            await OnEmailsSelected.InvokeAsync(SelectedEmails);
    }

    private async Task OnCancel()
    {
        await OnCancelled.InvokeAsync();
    }
}