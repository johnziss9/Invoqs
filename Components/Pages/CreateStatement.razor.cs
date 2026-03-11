using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Interfaces;
using Invoqs.Models;

namespace Invoqs.Components.Pages
{
    public partial class CreateStatement
    {
        [Inject] public IStatementService StatementService { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;

        private CreateStatementModel model = new()
        {
            StartDate = DateTime.Now.AddMonths(-1).Date,
            EndDate = DateTime.Now.Date
        };

        private bool isSubmitting = false;
        private string? errorMessage;

        private async Task HandleValidSubmit()
        {
            if (!model.StartDate.HasValue || !model.EndDate.HasValue)
            {
                errorMessage = "Παρακαλώ επιλέξτε και τις δύο ημερομηνίες.";
                return;
            }

            if (model.EndDate.Value < model.StartDate.Value)
            {
                errorMessage = "Η ημερομηνία λήξης πρέπει να είναι μετά ή ίση με την ημερομηνία έναρξης.";
                return;
            }

            isSubmitting = true;
            errorMessage = null;
            StateHasChanged();

            try
            {
                Console.WriteLine($"Creating statement from {model.StartDate} to {model.EndDate}");
                var statement = await StatementService.CreateStatementAsync(model);

                if (statement != null)
                {
                    NavigationManager.NavigateTo($"/accounting/{statement.Id}", forceLoad: true);
                }
                else
                {
                    Console.WriteLine("Statement creation returned null");
                    errorMessage = "Αποτυχία δημιουργίας κατάστασης. Βεβαιωθείτε ότι υπάρχουν τιμολόγια στην επιλεγμένη περίοδο.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating statement: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                errorMessage = $"Παρουσιάστηκε σφάλμα κατά τη δημιουργία της κατάστασης: {ex.Message}";
            }
            finally
            {
                isSubmitting = false;
                StateHasChanged();
            }
        }
    }
}
