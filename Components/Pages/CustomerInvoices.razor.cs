using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;
using Invoqs.Components.UI;

namespace Invoqs.Components.Pages
{
    public partial class CustomerInvoices : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; }

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        // Component state
        protected string currentUser = "John Doe";
        protected bool isLoading = true;
        protected string errorMessage = "";

        // Data
        protected CustomerModel? customer;
        protected string customerName = "Loading...";
        protected List<InvoiceModel> customerInvoices = new();
        protected decimal totalValue = 0;
        protected decimal totalOutstanding = 0;

        // Filter state
        protected string searchTerm = "";
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

                // Load customer and invoices concurrently
                var customerTask = CustomerService.GetCustomerByIdAsync(CustomerId);
                var invoicesTask = InvoiceService.GetInvoicesByCustomerIdAsync(CustomerId);

                await Task.WhenAll(customerTask, invoicesTask);

                customer = customerTask.Result;
                customerInvoices = invoicesTask.Result;

                if (customer != null)
                {
                    customerName = customer.Name;
                }
                else
                {
                    errorMessage = "Customer not found";
                    Navigation.NavigateTo("/customers");
                    return;
                }

                // Calculate totals
                totalValue = customerInvoices.Sum(i => i.Total);
                totalOutstanding = customerInvoices
                    .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                    .Sum(i => i.Total);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading data: {ex.Message}";
                customerInvoices = new List<InvoiceModel>();
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
                var filtered = customerInvoices.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(i =>
                        i.InvoiceNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
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

        protected string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        // Event handlers
        protected async Task HandleLogout()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                // Prerendering, ignore localStorage calls
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage during logout: {ex.Message}");
            }

            // Always redirect to login, even if localStorage clearing fails
            Navigation.NavigateTo("/login", true);
        }

        protected Task HandleViewInvoice(InvoiceModel invoice)
        {
            var returnUrl = Uri.EscapeDataString(Navigation.Uri);
            Navigation.NavigateTo($"/invoice/{invoice.Id}?returnUrl={returnUrl}");
            return Task.CompletedTask;
        }

        protected Task HandleEditInvoice(InvoiceModel invoice)
        {
            var returnUrl = Uri.EscapeDataString(Navigation.Uri);
            Navigation.NavigateTo($"/invoice/{invoice.Id}/edit?returnUrl={returnUrl}");
            return Task.CompletedTask;
        }

        protected async Task HandleMarkAsSent(InvoiceModel invoice)
        {
            try
            {
                var success = await InvoiceService.MarkInvoiceAsSentAsync(invoice.Id);
                if (success)
                {
                    await LoadData();
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

        protected async Task HandleMarkAsDelivered(InvoiceModel invoice)
        {
            try
            {
                var success = await InvoiceService.MarkInvoiceAsDeliveredAsync(invoice.Id);
                if (success)
                {
                    await LoadData();
                }
                else
                {
                    errorMessage = "Failed to mark invoice as delivered";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error updating invoice: {ex.Message}";
                StateHasChanged();
            }
        }

        protected async Task HandleMarkAsPaid(MarkAsPaidEventArgs args)
        {
            try
            {
                var success = await InvoiceService.MarkInvoiceAsPaidAsync(
                    args.Invoice.Id,
                    args.PaymentDate,
                    args.PaymentMethod,
                    args.PaymentReference
                );

                if (success)
                {
                    // Reload the invoice to get updated status
                    var updatedInvoice = await InvoiceService.GetInvoiceByIdAsync(args.Invoice.Id);
                    if (updatedInvoice != null)
                    {
                        var index = customerInvoices.FindIndex(i => i.Id == args.Invoice.Id);
                        if (index >= 0)
                        {
                            customerInvoices[index] = updatedInvoice;
                        }
                    }

                    // Recalculate totals
                    totalValue = customerInvoices.Sum(i => i.Total);
                    totalOutstanding = customerInvoices
                        .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                        .Sum(i => i.Total);

                    StateHasChanged();
                }
                else
                {
                    errorMessage = "Failed to mark invoice as paid";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error marking invoice as paid: {ex.Message}";
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
                    await LoadData();
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
    }
}