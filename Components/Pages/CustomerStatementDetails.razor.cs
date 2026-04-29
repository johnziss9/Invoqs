using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class CustomerStatementDetails : ComponentBase
    {
        [Parameter] public int StatementId { get; set; }

        [Inject] private ICustomerStatementService CustomerStatementService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        private CustomerStatementModel? statement;
        private bool isLoading = true;
        private string? errorMessage;

        private bool showEmailModal = false;
        private bool showDeleteModal = false;
        private bool showDeliveredConfirmation = false;

        private string emailRecipient = string.Empty;
        private string? emailError;

        private bool isSending = false;
        private bool isDownloading = false;
        private bool isDeleting = false;
        private bool isProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadStatement();
        }

        private async Task LoadStatement()
        {
            try
            {
                isLoading = true;
                statement = await CustomerStatementService.GetCustomerStatementByIdAsync(StatementId);

                if (statement != null && statement.CustomerEmails.Any())
                {
                    emailRecipient = statement.CustomerEmails.First();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα φόρτωσης: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task DownloadPdf()
        {
            try
            {
                isDownloading = true;
                StateHasChanged();

                var pdfBytes = await CustomerStatementService.DownloadCustomerStatementPdfAsync(StatementId);

                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    var fileName = $"{statement?.StatementNumber ?? $"CS-{StatementId}"}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "application/pdf", pdfBytes);
                }
                else
                {
                    errorMessage = "Σφάλμα δημιουργίας PDF.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα: {ex.Message}";
            }
            finally
            {
                isDownloading = false;
                StateHasChanged();
            }
        }

        private void ShowEmailModal()
        {
            emailError = null;
            showEmailModal = true;
            StateHasChanged();
        }

        private void HideEmailModal()
        {
            showEmailModal = false;
            StateHasChanged();
        }

        private async Task SendEmail()
        {
            if (string.IsNullOrWhiteSpace(emailRecipient))
            {
                emailError = "Παρακαλώ εισάγετε email παραλήπτη.";
                return;
            }

            try
            {
                isSending = true;
                emailError = null;
                StateHasChanged();

                var result = await CustomerStatementService.SendCustomerStatementAsync(StatementId, new List<string> { emailRecipient });

                if (result)
                {
                    showEmailModal = false;
                    await LoadStatement();
                }
                else
                {
                    emailError = "Η αποστολή απέτυχε. Παρακαλώ δοκιμάστε ξανά.";
                }
            }
            catch (Exception ex)
            {
                emailError = $"Σφάλμα: {ex.Message}";
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
            StateHasChanged();
        }

        private void HideDeleteModal()
        {
            showDeleteModal = false;
            StateHasChanged();
        }

        private async Task ConfirmDelete()
        {
            try
            {
                isDeleting = true;
                StateHasChanged();

                var result = await CustomerStatementService.DeleteCustomerStatementAsync(StatementId);

                if (result)
                {
                    Navigation.NavigateTo("/customer-statements", forceLoad: true);
                }
                else
                {
                    errorMessage = "Σφάλμα διαγραφής.";
                    showDeleteModal = false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα: {ex.Message}";
                showDeleteModal = false;
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        private void ShowDeliveredConfirmation()
        {
            showDeliveredConfirmation = true;
            StateHasChanged();
        }

        private void HideDeliveredConfirmation()
        {
            showDeliveredConfirmation = false;
            StateHasChanged();
        }

        private async Task ConfirmMarkAsDelivered()
        {
            try
            {
                isProcessing = true;
                StateHasChanged();

                var result = await CustomerStatementService.MarkCustomerStatementAsDeliveredAsync(StatementId);

                if (result)
                {
                    showDeliveredConfirmation = false;
                    await LoadStatement();
                }
                else
                {
                    errorMessage = "Σφάλμα ενημέρωσης κατάστασης.";
                    showDeliveredConfirmation = false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα: {ex.Message}";
                showDeliveredConfirmation = false;
            }
            finally
            {
                isProcessing = false;
                StateHasChanged();
            }
        }

        private static string TranslateStatus(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Sent => "Απεσταλμένο",
            InvoiceStatus.Delivered => "Παραδοθέν",
            InvoiceStatus.PartiallyPaid => "Μερικώς Πληρ.",
            InvoiceStatus.Paid => "Πληρωμένο",
            InvoiceStatus.Overdue => "Ληξιπρόθεσμο",
            InvoiceStatus.Cancelled => "Ακυρωμένο",
            _ => status.ToString()
        };

        private static string TranslatePaymentMethod(string? method) => method switch
        {
            "Bank Transfer" => "Τραπεζική Μεταφορά",
            "Cash" => "Μετρητά",
            "Cheque" => "Επιταγή",
            "Credit Card" => "Πιστωτική Κάρτα",
            "Other" => "Άλλο",
            null => "—",
            "" => "—",
            _ => method
        };

        private static string GetStatusBadgeClass(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Paid => "bg-success",
            InvoiceStatus.PartiallyPaid => "bg-warning text-dark",
            InvoiceStatus.Overdue => "bg-danger",
            InvoiceStatus.Sent => "bg-info",
            InvoiceStatus.Delivered => "bg-primary",
            InvoiceStatus.Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };
    }
}
