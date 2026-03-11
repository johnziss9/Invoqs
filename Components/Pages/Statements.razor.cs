using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Interfaces;
using Invoqs.Models;

namespace Invoqs.Components.Pages
{
    public partial class Statements
    {
        [Inject] public IStatementService StatementService { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;

        private List<StatementModel> statements = new();
        private bool isLoading = true;
        private bool showDeleteModal = false;
        private bool isDeleting = false;
        private int? downloadingPdf = null;
        private StatementModel? statementToDelete;

        // Filter properties
        protected string searchTerm = "";
        protected string sortBy = "date";
        protected string filterBy = "all";

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                statements = await StatementService.GetAllStatementsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading statements: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        protected IEnumerable<StatementModel> filteredStatements
        {
            get
            {
                var filtered = statements.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(s =>
                        s.StatementNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                // Apply status filter
                filtered = filterBy switch
                {
                    "sent" => filtered.Where(s => s.IsSent),
                    "notSent" => filtered.Where(s => !s.IsSent),
                    _ => filtered
                };

                // Apply sorting
                filtered = sortBy switch
                {
                    "number" => filtered.OrderBy(s => s.StatementNumber),
                    "amount" => filtered.OrderByDescending(s => s.TotalAmount),
                    _ => filtered.OrderByDescending(s => s.CreatedDate)
                };

                return filtered;
            }
        }

        protected void ViewStatement(int statementId)
        {
            NavigationManager.NavigateTo($"/accounting/{statementId}", forceLoad: true);
        }

        private async Task DownloadPdf(int statementId, string statementNumber)
        {
            try
            {
                downloadingPdf = statementId;
                StateHasChanged();

                var pdfBytes = await StatementService.DownloadStatementPdfAsync(statementId);

                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    var fileName = $"{statementNumber}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "application/pdf", pdfBytes);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Αποτυχία λήψης PDF. Παρακαλώ δοκιμάστε ξανά.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading PDF: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("alert", "Παρουσιάστηκε σφάλμα κατά τη λήψη του PDF.");
            }
            finally
            {
                downloadingPdf = null;
                StateHasChanged();
            }
        }

        private void ShowDeleteModal(StatementModel statement)
        {
            statementToDelete = statement;
            showDeleteModal = true;
        }

        private void HideDeleteModal()
        {
            statementToDelete = null;
            showDeleteModal = false;
        }

        private async Task ConfirmDelete()
        {
            if (statementToDelete == null) return;

            isDeleting = true;
            StateHasChanged();

            try
            {
                var success = await StatementService.DeleteStatementAsync(statementToDelete.Id);

                if (success)
                {
                    // Reload statements
                    isLoading = true;
                    statements = await StatementService.GetAllStatementsAsync();
                    isLoading = false;

                    HideDeleteModal();
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Αποτυχία διαγραφής. Παρακαλώ δοκιμάστε ξανά.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting statement: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("alert", "Παρουσιάστηκε σφάλμα κατά τη διαγραφή.");
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        protected int GetMonthStatementCount()
        {
            var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return statements.Count(s =>
                s.CreatedDate >= startOfThisMonth &&
                s.CreatedDate < startOfThisMonth.AddMonths(1));
        }
    }
}
