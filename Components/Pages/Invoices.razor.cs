using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class Invoices : ComponentBase
    {
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Component state
        protected string currentUser = "John Doe";
        protected bool isLoading = true;
        protected string errorMessage = "";

        // Data
        protected List<InvoiceModel> invoices = new();
        protected List<CustomerModel> customers = new();
        protected decimal totalOutstanding = 0;

        // Filter state
        protected string searchTerm = "";
        protected string customerFilter = "";
        protected string statusFilter = "";
        protected string dateFilter = "all";
        protected string sortBy = "created";

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                // Load invoices and customers concurrently
                var invoicesTask = InvoiceService.GetAllInvoicesAsync();
                var customersTask = CustomerService.GetAllCustomersAsync();

                await Task.WhenAll(invoicesTask, customersTask);

                invoices = invoicesTask.Result;
                customers = customersTask.Result;

                // Calculate total outstanding
                totalOutstanding = invoices
                    .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                    .Sum(i => i.Total);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading data: {ex.Message}";
                invoices = new List<InvoiceModel>();
                customers = new List<CustomerModel>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected IEnumerable<InvoiceModel> filteredInvoices
        {
            get
            {
                var filtered = invoices.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(i =>
                        i.InvoiceNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (i.Customer?.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (i.Customer?.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                // Apply customer filter
                if (!string.IsNullOrWhiteSpace(customerFilter) && int.TryParse(customerFilter, out var customerId))
                {
                    filtered = filtered.Where(i => i.CustomerId == customerId);
                }

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<InvoiceStatus>(statusFilter, out var status))
                {
                    filtered = filtered.Where(i => i.Status == status);
                }

                // Apply date range filter
                filtered = dateFilter switch
                {
                    "thisWeek" => filtered.Where(i => i.CreatedDate >= DateTime.Now.AddDays(-7)),
                    "thisMonth" => filtered.Where(i => i.CreatedDate >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)),
                    "lastMonth" => filtered.Where(i => i.CreatedDate >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1) &&
                                                      i.CreatedDate < new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)),
                    "thisYear" => filtered.Where(i => i.CreatedDate >= new DateTime(DateTime.Now.Year, 1, 1)),
                    _ => filtered
                };

                // Apply sorting
                filtered = sortBy switch
                {
                    "due" => filtered.OrderBy(i => i.DueDate),
                    "number" => filtered.OrderBy(i => i.InvoiceNumber),
                    "customer" => filtered.OrderBy(i => i.Customer?.Name ?? ""),
                    "total" => filtered.OrderByDescending(i => i.Total),
                    _ => filtered.OrderByDescending(i => i.CreatedDate)
                };

                return filtered;
            }
        }

        protected void FilterInvoices()
        {
            StateHasChanged();
        }

        // Event handlers
        protected Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        protected Task HandleViewInvoice(InvoiceModel invoice)
        {
            Navigation.NavigateTo($"/invoice/{invoice.Id}", true);
            return Task.CompletedTask;
        }

        protected Task HandleEditInvoice(InvoiceModel invoice)
        {
            Navigation.NavigateTo($"/invoice/{invoice.Id}/edit", true);
            return Task.CompletedTask;
        }

        protected async Task HandleMarkAsSent(InvoiceModel invoice)
        {
            try
            {
                var success = await InvoiceService.MarkInvoiceAsSentAsync(invoice.Id);
                if (success)
                {
                    await LoadData(); // Refresh data
                }
                else
                {
                    errorMessage = "Failed to mark invoice as sent";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error updating invoice: {ex.Message}";
                StateHasChanged();
            }
        }

        protected async Task HandleMarkAsPaid(InvoiceModel invoice)
        {
            try
            {
                // In a real app, you'd show a dialog to collect payment details
                var success = await InvoiceService.MarkInvoiceAsPaidAsync(
                    invoice.Id,
                    DateTime.Now,
                    "Manual Entry",
                    $"PAYMENT-{DateTime.Now:yyyyMMdd}-{invoice.Id}"
                );

                if (success)
                {
                    await LoadData(); // Refresh data
                }
                else
                {
                    errorMessage = "Failed to mark invoice as paid";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error updating invoice: {ex.Message}";
                StateHasChanged();
            }
        }

        protected async Task HandleCancelInvoice(InvoiceModel invoice)
        {
            try
            {
                // In a real app, you'd show a confirmation dialog
                var success = await InvoiceService.CancelInvoiceAsync(invoice.Id, "Cancelled by user");

                if (success)
                {
                    await LoadData(); // Refresh data
                }
                else
                {
                    errorMessage = "Failed to cancel invoice";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error cancelling invoice: {ex.Message}";
                StateHasChanged();
            }
        }

        protected async Task HandleDeleteInvoice(InvoiceModel invoice)
        {
            try
            {
                if (invoice.Status != InvoiceStatus.Draft)
                {
                    errorMessage = "Only draft invoices can be deleted";
                    StateHasChanged();
                    return;
                }

                var success = await InvoiceService.DeleteInvoiceAsync(invoice.Id);

                if (success)
                {
                    invoices.Remove(invoice);
                    
                    // Recalculate outstanding total
                    totalOutstanding = invoices
                        .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                        .Sum(i => i.Total);
                        
                    StateHasChanged();
                }
                else
                {
                    errorMessage = "Failed to delete invoice";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting invoice: {ex.Message}";
                StateHasChanged();
            }
        }
    }
}