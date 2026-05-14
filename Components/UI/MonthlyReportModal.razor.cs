using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.UI
{
    public partial class MonthlyReportModal : ComponentBase
    {
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;

        [Parameter] public EventCallback OnClose { get; set; }

        protected int selectedMonth = DateTime.Now.Month;
        protected int selectedYear = DateTime.Now.Year;
        protected bool isLoading = false;
        protected bool hasLoaded = false;

        // Job data
        protected int totalJobs;
        protected Dictionary<JobType, int> jobsByType = new();
        protected int invoicedJobs;
        protected int uninvoicedJobs;

        // Invoice data
        protected Dictionary<InvoiceStatus, (int Count, decimal Total)> invoicesByStatus = new();
        protected decimal totalReceived;

        protected readonly string[] monthNames =
        {
            "Ιανουάριος", "Φεβρουάριος", "Μάρτιος", "Απρίλιος",
            "Μάιος", "Ιούνιος", "Ιούλιος", "Αύγουστος",
            "Σεπτέμβριος", "Οκτώβριος", "Νοέμβριος", "Δεκέμβριος"
        };

        protected int[] availableYears = Enumerable.Range(2023, DateTime.Now.Year - 2023 + 2).ToArray();

        protected async Task LoadReport()
        {
            isLoading = true;
            hasLoaded = false;
            StateHasChanged();

            try
            {
                var jobs = await JobService.GetAllJobsAsync();
                var invoices = await InvoiceService.GetAllInvoicesAsync();

                var monthJobs = jobs
                    .Where(j => j.JobDate.Month == selectedMonth && j.JobDate.Year == selectedYear)
                    .ToList();

                var monthInvoices = invoices
                    .Where(i => i.CreatedDate.Month == selectedMonth && i.CreatedDate.Year == selectedYear)
                    .ToList();

                // Jobs
                totalJobs = monthJobs.Count;
                jobsByType = Enum.GetValues<JobType>()
                    .ToDictionary(t => t, t => monthJobs.Count(j => j.Type == t));
                invoicedJobs = monthJobs.Count(j => j.IsInvoiced);
                uninvoicedJobs = monthJobs.Count(j => !j.IsInvoiced);

                // Invoices by status
                invoicesByStatus = Enum.GetValues<InvoiceStatus>()
                    .ToDictionary(
                        s => s,
                        s => (
                            monthInvoices.Count(i => i.Status == s),
                            monthInvoices.Where(i => i.Status == s).Sum(i => i.Total)
                        )
                    );

                // Total received = sum of AmountPaid across all invoices that month
                totalReceived = monthInvoices.Sum(i => i.AmountPaid);

                hasLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading monthly report: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected async Task Close()
        {
            hasLoaded = false;
            await OnClose.InvokeAsync();
        }

        protected static string GetJobTypeDisplay(JobType type) => type switch
        {
            JobType.SkipRental => "Ενοικίαση Skip",
            JobType.SandDelivery => "Παράδοση Άμμου",
            JobType.ForkLiftService => "Υπηρεσία Forklift",
            JobType.Transfer => "Μεταφορά",
            JobType.SellForklift => "Πώληση Forklift",
            _ => type.ToString()
        };

        protected static string GetStatusDisplay(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Draft => "Πρόχειρο",
            InvoiceStatus.Sent => "Απεσταλμένο",
            InvoiceStatus.Delivered => "Παραδομένο",
            InvoiceStatus.PartiallyPaid => "Μερικώς Πληρωμένο",
            InvoiceStatus.Paid => "Πληρωμένο",
            InvoiceStatus.Overdue => "Καθυστερημένο",
            InvoiceStatus.Cancelled => "Ακυρωμένο",
            _ => status.ToString()
        };

        protected static string GetStatusBadgeClass(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Draft => "bg-secondary",
            InvoiceStatus.Sent => "bg-primary",
            InvoiceStatus.Delivered => "bg-info",
            InvoiceStatus.PartiallyPaid => "bg-warning",
            InvoiceStatus.Paid => "bg-success",
            InvoiceStatus.Overdue => "bg-danger",
            InvoiceStatus.Cancelled => "bg-dark",
            _ => "bg-secondary"
        };
    }
}
