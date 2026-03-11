using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Interfaces;
using Invoqs.Models;
using static Invoqs.Models.InvoiceModel;

namespace Invoqs.Components.Pages
{
    public partial class StatementDetails
    {
        [Parameter] public int StatementId { get; set; }

        [Inject] public IStatementService StatementService { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;

        private StatementModel? statement;
        private bool isLoading = true;
        private bool isDownloading = false;
        private bool isSending = false;
        private bool isDeleting = false;
        private bool isProcessing = false;
        private bool showEmailModal = false;
        private bool showDeleteModal = false;
        private bool showDeliveredConfirmation = false;
        private string emailRecipient = string.Empty;
        private string? emailError;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Console.WriteLine($"OnInitializedAsync - Loading statement {StatementId}");
                await LoadStatement();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnInitializedAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                Console.WriteLine($"OnParametersSetAsync - Loading statement {StatementId}");
                await LoadStatement();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnParametersSetAsync: {ex.Message}");
            }
        }

        private async Task LoadStatement()
        {
            try
            {
                isLoading = true;
                Console.WriteLine($"LoadStatement - Fetching statement {StatementId}");
                statement = await StatementService.GetStatementByIdAsync(StatementId);
                Console.WriteLine($"LoadStatement - Statement loaded: {statement?.StatementNumber ?? "null"}");
                isLoading = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadStatement: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                isLoading = false;
            }
        }

        private async Task DownloadPdf()
        {
            if (statement == null) return;

            try
            {
                isDownloading = true;
                StateHasChanged();

                var pdfBytes = await StatementService.DownloadStatementPdfAsync(statement.Id);

                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    var fileName = $"{statement.StatementNumber}.pdf";
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
                isDownloading = false;
                StateHasChanged();
            }
        }

        private void ShowEmailModal()
        {
            showEmailModal = true;
            emailError = null;
        }

        private void HideEmailModal()
        {
            showEmailModal = false;
            emailRecipient = string.Empty;
            emailError = null;
        }

        private async Task SendEmail()
        {
            if (statement == null) return;

            if (string.IsNullOrWhiteSpace(emailRecipient))
            {
                emailError = "Παρακαλώ εισαγάγετε ένα email.";
                return;
            }

            try
            {
                isSending = true;
                emailError = null;
                StateHasChanged();

                var success = await StatementService.SendStatementAsync(statement.Id, new List<string> { emailRecipient });

                if (success)
                {
                    HideEmailModal();
                    await LoadStatement();
                }
                else
                {
                    emailError = "Αποτυχία αποστολής. Παρακαλώ δοκιμάστε ξανά.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                emailError = "Παρουσιάστηκε σφάλμα κατά την αποστολή.";
            }
            finally
            {
                isSending = false;
                StateHasChanged();
            }
        }

        private void ShowDeleteModal()
        {
            showDeleteModal = true;
        }

        private void HideDeleteModal()
        {
            showDeleteModal = false;
        }

        private void ShowDeliveredConfirmation()
        {
            showDeliveredConfirmation = true;
        }

        private void HideDeliveredConfirmation()
        {
            showDeliveredConfirmation = false;
        }

        private async Task ConfirmMarkAsDelivered()
        {
            showDeliveredConfirmation = false;
            await MarkAsDelivered();
        }

        private async Task ConfirmDelete()
        {
            if (statement == null) return;

            isDeleting = true;
            StateHasChanged();

            try
            {
                var success = await StatementService.DeleteStatementAsync(statement.Id);

                if (success)
                {
                    NavigationManager.NavigateTo("/accounting", true);
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
                HideDeleteModal();
                StateHasChanged();
            }
        }

        private async Task MarkAsDelivered()
        {
            if (statement == null) return;

            try
            {
                isProcessing = true;
                StateHasChanged();

                var success = await StatementService.MarkStatementAsDeliveredAsync(statement.Id);

                if (success)
                {
                    await LoadStatement();
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Αποτυχία σημείωσης. Παρακαλώ δοκιμάστε ξανά.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking statement as delivered: {ex.Message}");
                await JSRuntime.InvokeVoidAsync("alert", "Παρουσιάστηκε σφάλμα κατά τη σημείωση.");
            }
            finally
            {
                isProcessing = false;
                StateHasChanged();
            }
        }

        private string TranslateStatus(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Draft => "Πρόχειρο",
                InvoiceStatus.Sent => "Απεσταλμένο",
                InvoiceStatus.Delivered => "Παραδοθέν",
                InvoiceStatus.PartiallyPaid => "Μερικώς Πληρωμένο",
                InvoiceStatus.Paid => "Πληρωμένο",
                InvoiceStatus.Overdue => "Καθυστερημένο",
                InvoiceStatus.Cancelled => "Ακυρωμένο",
                _ => status.ToString()
            };
        }
    }
}
