using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class CustomerStatements : ComponentBase
    {
        [Inject] private ICustomerStatementService CustomerStatementService { get; set; } = default!;

        private List<CustomerStatementModel> statements = new();
        private bool isLoading = true;
        private string? errorMessage;

        protected override async Task OnInitializedAsync()
        {
            await LoadStatements();
        }

        private async Task LoadStatements()
        {
            try
            {
                isLoading = true;
                errorMessage = null;
                statements = await CustomerStatementService.GetAllCustomerStatementsAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα κατά τη φόρτωση: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }
    }
}
