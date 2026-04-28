using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class BulkEmail : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IEmailService EmailService { get; set; } = default!;

        private List<CustomerModel> customers = new();
        private HashSet<int> selectedIds = new();
        private string searchTerm = "";
        private string subject = "";
        private string body = "";
        private string language = "el";
        private bool isLoading = true;
        private bool showConfirmModal = false;
        private bool isSending = false;
        private string errorMessage = "";
        private BulkEmailResult? sendResult;

        private bool IsGreek => language != "en";
        private string Greeting => IsGreek ? "Αγαπητέ/ή [Όνομα Πελάτη]," : "Dear [Customer Name],";
        private string SignOff => IsGreek ? "Με εκτίμηση," : "Kind regards,";
        private string BodyPlaceholder => IsGreek ? "Γράψτε το μήνυμά σας..." : "Write your message...";

        private IEnumerable<CustomerModel> filteredCustomers =>
            string.IsNullOrWhiteSpace(searchTerm)
                ? customers
                : customers.Where(c =>
                    c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        private IEnumerable<CustomerModel> customersWithEmail =>
            customers.Where(c => c.Emails.Any());

        private bool CanSend =>
            selectedIds.Count > 0 &&
            !string.IsNullOrWhiteSpace(subject) &&
            !string.IsNullOrWhiteSpace(body);

        protected override async Task OnInitializedAsync()
        {
            try
            {
                customers = await CustomerService.GetAllCustomersAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα φόρτωσης πελατών: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ToggleCustomer(int id, bool selected)
        {
            if (selected) selectedIds.Add(id);
            else selectedIds.Remove(id);
        }

        private void SelectAll()
        {
            foreach (var c in customersWithEmail)
                selectedIds.Add(c.Id);
        }

        private void DeselectAll()
        {
            selectedIds.Clear();
        }

        private void OpenConfirmModal()
        {
            if (!CanSend) return;
            showConfirmModal = true;
        }

        private void CloseModal()
        {
            if (!isSending)
                showConfirmModal = false;
        }

        private async Task ConfirmSend()
        {
            isSending = true;
            try
            {
                var request = new BulkEmailRequest
                {
                    CustomerIds = selectedIds.ToList(),
                    Subject = subject,
                    Body = body,
                    Language = language
                };

                sendResult = await EmailService.SendBulkEmailAsync(request);
                showConfirmModal = false;

                if (sendResult.SentCount > 0)
                {
                    subject = "";
                    body = "";
                    selectedIds.Clear();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα αποστολής: {ex.Message}";
                showConfirmModal = false;
            }
            finally
            {
                isSending = false;
            }
        }
    }
}
