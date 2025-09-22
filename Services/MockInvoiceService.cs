using Invoqs.Models;

namespace Invoqs.Services
{
    public class MockInvoiceService : IInvoiceService
    {
        private static List<InvoiceModel> _invoices = new();
        private static List<InvoiceLineItemModel> _lineItems = new();
        private static int _nextId = 1;
        private static int _nextLineItemId = 1;

        private readonly IJobService _jobService;
        private readonly ICustomerService _customerService;

        public MockInvoiceService(IJobService jobService, ICustomerService customerService)
        {
            _jobService = jobService;
            _customerService = customerService;
            
            if (!_invoices.Any())
            {
                InitializeSampleData();
            }
        }

        private void InitializeSampleData()
        {
            // Create some sample invoices with realistic data
            var invoices = new List<InvoiceModel>
            {
                new InvoiceModel
                {
                    Id = _nextId++,
                    InvoiceNumber = "INV-2025-0001",
                    CustomerId = 1, // ABC Construction
                    CreatedDate = DateTime.Now.AddDays(-15),
                    DueDate = DateTime.Now.AddDays(15),
                    Status = InvoiceStatus.Sent,
                    Subtotal = 450.00m,
                    VatRate = 5m, // Skip rental
                    PaymentTermsDays = 30,
                    Notes = "Skip rental for construction site"
                },
                new InvoiceModel
                {
                    Id = _nextId++,
                    InvoiceNumber = "INV-2025-0002", 
                    CustomerId = 2, // Green Gardens Ltd
                    CreatedDate = DateTime.Now.AddDays(-8),
                    DueDate = DateTime.Now.AddDays(22),
                    Status = InvoiceStatus.Paid,
                    PaidDate = DateTime.Now.AddDays(-2),
                    PaymentMethod = "Bank Transfer",
                    PaymentReference = "PAY-2025-002",
                    Subtotal = 840.34m,
                    VatRate = 19m, // Sand delivery
                    PaymentTermsDays = 30,
                    Notes = "Sand delivery for garden project"
                },
                new InvoiceModel
                {
                    Id = _nextId++,
                    InvoiceNumber = "INV-2025-0003",
                    CustomerId = 3, // Manchester Motors
                    CreatedDate = DateTime.Now.AddDays(-25),
                    DueDate = DateTime.Now.AddDays(-5),
                    Status = InvoiceStatus.Overdue,
                    Subtotal = 320.00m,
                    VatRate = 5m, // Skip rental
                    PaymentTermsDays = 30,
                    Notes = "Skip rental for garage clear-out"
                },
                new InvoiceModel
                {
                    Id = _nextId++,
                    InvoiceNumber = "INV-2025-0004",
                    CustomerId = 1, // ABC Construction
                    CreatedDate = DateTime.Now.AddDays(-3),
                    DueDate = DateTime.Now.AddDays(27),
                    Status = InvoiceStatus.Draft,
                    Subtotal = 1250.00m,
                    VatRate = 19m, // Fork lift service
                    PaymentTermsDays = 30,
                    Notes = "Fork lift service - multiple locations"
                },
                new InvoiceModel
                {
                    Id = _nextId++,
                    InvoiceNumber = "INV-2025-0005",
                    CustomerId = 4, // Quick Clean Services
                    CreatedDate = DateTime.Now.AddDays(-12),
                    DueDate = DateTime.Now.AddDays(18),
                    Status = InvoiceStatus.Sent,
                    Subtotal = 680.00m,
                    VatRate = 5m, // Skip rental
                    PaymentTermsDays = 30
                },
                new InvoiceModel
                {
                    Id = _nextId++,
                    InvoiceNumber = "INV-2025-0006",
                    CustomerId = 5, // London Builders
                    CreatedDate = DateTime.Now.AddDays(-30),
                    DueDate = DateTime.Now.AddDays(-10),
                    Status = InvoiceStatus.Overdue,
                    Subtotal = 1540.00m,
                    VatRate = 19m, // Mixed services
                    PaymentTermsDays = 30,
                    Notes = "Multiple services - sand delivery and fork lift"
                }
            };

            // Create corresponding line items (simplified for mock data)
            var lineItems = new List<InvoiceLineItemModel>
            {
                new InvoiceLineItemModel
                {
                    Id = _nextLineItemId++,
                    InvoiceId = 1,
                    JobId = 1, // Should link to actual job
                    Description = "Skip Rental - 123 Business Park, London (15/01/2025)",
                    Quantity = 1,
                    UnitPrice = 450.00m
                },
                new InvoiceLineItemModel
                {
                    Id = _nextLineItemId++,
                    InvoiceId = 2,
                    JobId = 2,
                    Description = "Sand Delivery - 45 Garden Street, Manchester (18/01/2025)",
                    Quantity = 1,
                    UnitPrice = 840.34m
                },
                new InvoiceLineItemModel
                {
                    Id = _nextLineItemId++,
                    InvoiceId = 3,
                    JobId = 5,
                    Description = "Skip Rental - 78 Garage Lane, Liverpool (02/01/2025)",
                    Quantity = 1,
                    UnitPrice = 320.00m
                }
            };

            _invoices.AddRange(invoices);
            _lineItems.AddRange(lineItems);

            // Link line items to invoices
            foreach (var invoice in _invoices)
            {
                invoice.LineItems = _lineItems.Where(li => li.InvoiceId == invoice.Id).ToList();
            }
        }

        // Standard CRUD operations
        public async Task<List<InvoiceModel>> GetAllInvoicesAsync()
        {
            await Task.Delay(200); // Simulate API delay
            
            // Load customer data for each invoice
            foreach (var invoice in _invoices)
            {
                invoice.Customer = await _customerService.GetCustomerByIdAsync(invoice.CustomerId);
            }
            
            return _invoices.OrderByDescending(i => i.CreatedDate).ToList();
        }

        public async Task<InvoiceModel?> GetInvoiceByIdAsync(int id)
        {
            await Task.Delay(100);
            
            var invoice = _invoices.FirstOrDefault(i => i.Id == id);
            if (invoice != null)
            {
                invoice.Customer = await _customerService.GetCustomerByIdAsync(invoice.CustomerId);
                invoice.LineItems = _lineItems.Where(li => li.InvoiceId == invoice.Id).ToList();
                
                // Load job data for line items
                foreach (var lineItem in invoice.LineItems)
                {
                    lineItem.Job = await _jobService.GetJobByIdAsync(lineItem.JobId);
                }
            }
            
            return invoice;
        }

        public async Task<InvoiceModel> CreateInvoiceAsync(InvoiceModel invoice)
        {
            await Task.Delay(300);
            
            invoice.Id = _nextId++;
            invoice.CreatedDate = DateTime.Now;
            invoice.InvoiceNumber = await GenerateNextInvoiceNumberAsync();
            
            _invoices.Add(invoice);
            
            // Add line items
            foreach (var lineItem in invoice.LineItems)
            {
                lineItem.Id = _nextLineItemId++;
                lineItem.InvoiceId = invoice.Id;
                _lineItems.Add(lineItem);
            }
            
            return invoice;
        }

        public async Task<InvoiceModel> UpdateInvoiceAsync(InvoiceModel invoice)
        {
            await Task.Delay(200);
            
            var existingInvoice = _invoices.FirstOrDefault(i => i.Id == invoice.Id);
            if (existingInvoice != null)
            {
                existingInvoice.InvoiceNumber = invoice.InvoiceNumber;
                existingInvoice.CustomerId = invoice.CustomerId;
                existingInvoice.DueDate = invoice.DueDate;
                existingInvoice.Status = invoice.Status;
                existingInvoice.Subtotal = invoice.Subtotal;
                existingInvoice.VatRate = invoice.VatRate;
                existingInvoice.PaymentTermsDays = invoice.PaymentTermsDays;
                existingInvoice.Notes = invoice.Notes;
                existingInvoice.PaymentMethod = invoice.PaymentMethod;
                existingInvoice.PaymentReference = invoice.PaymentReference;
                existingInvoice.PaidDate = invoice.PaidDate;
                existingInvoice.UpdatedDate = DateTime.Now;
                
                return existingInvoice;
            }
            
            throw new ArgumentException("Invoice not found");
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            await Task.Delay(200);
            
            var invoice = _invoices.FirstOrDefault(i => i.Id == id);
            if (invoice != null)
            {
                _invoices.Remove(invoice);
                
                // Remove associated line items
                var lineItems = _lineItems.Where(li => li.InvoiceId == id).ToList();
                foreach (var lineItem in lineItems)
                {
                    _lineItems.Remove(lineItem);
                }
                
                return true;
            }
            
            return false;
        }

        // Business logic operations
        public async Task<InvoiceModel> CreateInvoiceFromJobsAsync(int customerId, List<int> jobIds, DateTime? dueDate = null)
        {
            await Task.Delay(300);
            
            var jobs = new List<JobModel>();
            foreach (var jobId in jobIds)
            {
                var job = await _jobService.GetJobByIdAsync(jobId);
                if (job != null && job.CanBeInvoiced)
                {
                    jobs.Add(job);
                }
            }
            
            if (!jobs.Any())
                throw new ArgumentException("No valid jobs found for invoicing");
            
            var invoice = new InvoiceModel
            {
                CustomerId = customerId,
                DueDate = dueDate ?? DateTime.Now.AddDays(30),
                Status = InvoiceStatus.Draft,
                PaymentTermsDays = 30
            };
            
            // Create line items from jobs
            foreach (var job in jobs)
            {
                var lineItem = InvoiceLineItemModel.FromJob(job);
                invoice.LineItems.Add(lineItem);
            }
            
            // Calculate totals and VAT rate
            invoice.Subtotal = invoice.LineItems.Sum(li => li.LineTotal);
            invoice.CalculateVatRate();
            
            return await CreateInvoiceAsync(invoice);
        }

        // Status updates
        public async Task<bool> MarkInvoiceAsSentAsync(int invoiceId)
        {
            await Task.Delay(100);
            
            var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice != null)
            {
                invoice.Status = InvoiceStatus.Sent;
                invoice.UpdatedDate = DateTime.Now;
                return true;
            }
            
            return false;
        }

        public async Task<bool> MarkInvoiceAsPaidAsync(int invoiceId, DateTime paidDate, string? paymentMethod = null, string? paymentReference = null)
        {
            await Task.Delay(100);
            
            var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice != null)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = paidDate;
                invoice.PaymentMethod = paymentMethod;
                invoice.PaymentReference = paymentReference;
                invoice.UpdatedDate = DateTime.Now;
                return true;
            }
            
            return false;
        }

        public async Task<bool> MarkInvoiceAsOverdueAsync(int invoiceId)
        {
            await Task.Delay(50);
            
            var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice != null && invoice.DueDate < DateTime.Now && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Overdue;
                invoice.UpdatedDate = DateTime.Now;
                return true;
            }
            
            return false;
        }

        public async Task<bool> CancelInvoiceAsync(int invoiceId, string? reason = null)
        {
            await Task.Delay(100);
            
            var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice != null)
            {
                invoice.Status = InvoiceStatus.Cancelled;
                if (!string.IsNullOrEmpty(reason))
                {
                    invoice.Notes = string.IsNullOrEmpty(invoice.Notes) ? $"Cancelled: {reason}" : $"{invoice.Notes}\nCancelled: {reason}";
                }
                invoice.UpdatedDate = DateTime.Now;
                return true;
            }
            
            return false;
        }

        // Query methods
        public async Task<List<InvoiceModel>> GetInvoicesByCustomerIdAsync(int customerId)
        {
            await Task.Delay(150);
            return _invoices.Where(i => i.CustomerId == customerId).OrderByDescending(i => i.CreatedDate).ToList();
        }

        public async Task<List<InvoiceModel>> GetInvoicesByStatusAsync(InvoiceStatus status)
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.Status == status).OrderByDescending(i => i.CreatedDate).ToList();
        }

        public async Task<List<InvoiceModel>> GetDraftInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Draft);
        }

        public async Task<List<InvoiceModel>> GetSentInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Sent);
        }

        public async Task<List<InvoiceModel>> GetPaidInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Paid);
        }

        public async Task<List<InvoiceModel>> GetOverdueInvoicesAsync()
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.IsOverdue).OrderBy(i => i.DueDate).ToList();
        }

        public async Task<List<InvoiceModel>> GetCancelledInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Cancelled);
        }

        public async Task<List<InvoiceModel>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.CreatedDate.Date >= startDate.Date && i.CreatedDate.Date <= endDate.Date)
                           .OrderByDescending(i => i.CreatedDate).ToList();
        }

        public async Task<List<InvoiceModel>> SearchInvoicesAsync(string searchTerm)
        {
            await Task.Delay(150);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllInvoicesAsync();
            
            searchTerm = searchTerm.ToLower();
            
            var results = new List<InvoiceModel>();
            foreach (var invoice in _invoices)
            {
                invoice.Customer = await _customerService.GetCustomerByIdAsync(invoice.CustomerId);
                
                if (invoice.InvoiceNumber.ToLower().Contains(searchTerm) ||
                    invoice.Customer?.Name.ToLower().Contains(searchTerm) == true ||
                    invoice.Notes?.ToLower().Contains(searchTerm) == true)
                {
                    results.Add(invoice);
                }
            }
            
            return results.OrderByDescending(i => i.CreatedDate).ToList();
        }

        // Statistics and reporting
        public async Task<decimal> GetTotalRevenueAsync()
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total);
        }

        public async Task<decimal> GetTotalOutstandingAsync()
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                           .Sum(i => i.Total);
        }

        public async Task<decimal> GetTotalOverdueAsync()
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.IsOverdue).Sum(i => i.Total);
        }

        public async Task<int> GetInvoiceCountByStatusAsync(InvoiceStatus status)
        {
            await Task.Delay(50);
            return _invoices.Count(i => i.Status == status);
        }

        // Dashboard metrics
        public async Task<decimal> GetWeeklyRevenueAsync()
        {
            await Task.Delay(100);
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            return _invoices.Where(i => i.Status == InvoiceStatus.Paid && i.PaidDate >= oneWeekAgo)
                           .Sum(i => i.Total);
        }

        public async Task<decimal> GetMonthlyRevenueAsync()
        {
            await Task.Delay(100);
            var oneMonthAgo = DateTime.Now.AddDays(-30);
            return _invoices.Where(i => i.Status == InvoiceStatus.Paid && i.PaidDate >= oneMonthAgo)
                           .Sum(i => i.Total);
        }

        public async Task<int> GetPendingInvoicesCountAsync()
        {
            await Task.Delay(50);
            return _invoices.Count(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue);
        }

        public async Task<decimal> GetPendingInvoicesAmountAsync()
        {
            await Task.Delay(50);
            return _invoices.Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                           .Sum(i => i.Total);
        }

        // Invoice numbering
        public async Task<string> GenerateNextInvoiceNumberAsync()
        {
            await Task.Delay(50);
            var year = DateTime.Now.Year;
            var maxNumber = _invoices
                .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}"))
                .Select(i => {
                    var parts = i.InvoiceNumber.Split('-');
                    return parts.Length == 3 && int.TryParse(parts[2], out var num) ? num : 0;
                })
                .DefaultIfEmpty(0)
                .Max();
            
            return $"INV-{year}-{(maxNumber + 1):D4}";
        }

        public async Task<bool> IsInvoiceNumberUniqueAsync(string invoiceNumber)
        {
            await Task.Delay(50);
            return !_invoices.Any(i => i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
        }

        // Job integration methods
        public async Task<List<JobModel>> GetUninvoicedCompletedJobsByCustomerAsync(int customerId)
        {
            await Task.Delay(100);
            var allJobs = await _jobService.GetJobsByCustomerIdAsync(customerId);
            return allJobs.Where(j => j.CanBeInvoiced).ToList();
        }

        public async Task<bool> CanJobsBeInvoicedAsync(List<int> jobIds)
        {
            await Task.Delay(100);
            foreach (var jobId in jobIds)
            {
                var job = await _jobService.GetJobByIdAsync(jobId);
                if (job == null || !job.CanBeInvoiced)
                    return false;
            }
            return true;
        }

        // Additional helper methods
        public async Task<bool> InvoiceExistsAsync(int id)
        {
            await Task.Delay(50);
            return _invoices.Any(i => i.Id == id);
        }

        public async Task<List<InvoiceModel>> GetInvoicesGroupedByCustomerAsync()
        {
            return await GetAllInvoicesAsync();
        }

        public async Task<List<InvoiceModel>> GetInvoicesDueByDateAsync(DateTime dueDate)
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.DueDate.Date == dueDate.Date && i.Status != InvoiceStatus.Paid)
                           .OrderBy(i => i.DueDate).ToList();
        }

        public async Task<List<InvoiceModel>> GetInvoicesCreatedThisMonthAsync()
        {
            await Task.Delay(100);
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _invoices.Where(i => i.CreatedDate >= startOfMonth)
                           .OrderByDescending(i => i.CreatedDate).ToList();
        }

        public async Task<List<InvoiceModel>> GetInvoicesCreatedThisWeekAsync()
        {
            await Task.Delay(100);
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            return _invoices.Where(i => i.CreatedDate >= oneWeekAgo)
                           .OrderByDescending(i => i.CreatedDate).ToList();
        }

        public async Task<decimal> GetTotalRevenueByCustomerAsync(int customerId)
        {
            await Task.Delay(100);
            return _invoices.Where(i => i.CustomerId == customerId && i.Status == InvoiceStatus.Paid)
                           .Sum(i => i.Total);
        }

        public async Task<List<InvoiceModel>> GetInvoicesWithFiltersAsync(int? customerId = null, InvoiceStatus? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            await Task.Delay(150);
            
            var query = _invoices.AsQueryable();
            
            if (customerId.HasValue)
                query = query.Where(i => i.CustomerId == customerId.Value);
            
            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);
            
            if (startDate.HasValue)
                query = query.Where(i => i.CreatedDate.Date >= startDate.Value.Date);
            
            if (endDate.HasValue)
                query = query.Where(i => i.CreatedDate.Date <= endDate.Value.Date);
            
            return query.OrderByDescending(i => i.CreatedDate).ToList();
        }

        public async Task<List<JobModel>> GetUninvoicedCompletedJobsByAddressAsync(int customerId, string address)
        {
            await Task.Delay(100);
            var jobs = await GetUninvoicedCompletedJobsByCustomerAsync(customerId);
            return jobs.Where(j => j.Address.Equals(address, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Line item management
        public async Task<InvoiceLineItemModel> AddLineItemAsync(int invoiceId, InvoiceLineItemModel lineItem)
        {
            await Task.Delay(100);
            
            lineItem.Id = _nextLineItemId++;
            lineItem.InvoiceId = invoiceId;
            _lineItems.Add(lineItem);
            
            // Update invoice totals
            var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice != null)
            {
                invoice.LineItems = _lineItems.Where(li => li.InvoiceId == invoiceId).ToList();
                invoice.Subtotal = invoice.LineItems.Sum(li => li.LineTotal);
                invoice.CalculateVatRate();
                invoice.UpdatedDate = DateTime.Now;
            }
            
            return lineItem;
        }

        public async Task<bool> RemoveLineItemAsync(int invoiceId, int lineItemId)
        {
            await Task.Delay(100);
            
            var lineItem = _lineItems.FirstOrDefault(li => li.Id == lineItemId && li.InvoiceId == invoiceId);
            if (lineItem != null)
            {
                _lineItems.Remove(lineItem);
                
                // Update invoice totals
                var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
                if (invoice != null)
                {
                    invoice.LineItems = _lineItems.Where(li => li.InvoiceId == invoiceId).ToList();
                    invoice.Subtotal = invoice.LineItems.Sum(li => li.LineTotal);
                    invoice.CalculateVatRate();
                    invoice.UpdatedDate = DateTime.Now;
                }
                
                return true;
            }
            
            return false;
        }

        public async Task<InvoiceModel> UpdateLineItemsAsync(int invoiceId, List<InvoiceLineItemModel> lineItems)
        {
            await Task.Delay(200);
            
            // Remove existing line items
            _lineItems.RemoveAll(li => li.InvoiceId == invoiceId);
            
            // Add new line items
            foreach (var lineItem in lineItems)
            {
                lineItem.Id = _nextLineItemId++;
                lineItem.InvoiceId = invoiceId;
                _lineItems.Add(lineItem);
            }
            
            // Update invoice
            var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice != null)
            {
                invoice.LineItems = lineItems;
                invoice.Subtotal = lineItems.Sum(li => li.LineTotal);
                invoice.CalculateVatRate();
                invoice.UpdatedDate = DateTime.Now;
                return invoice;
            }
            
            throw new ArgumentException("Invoice not found");
        }

        // Helper method to reset data (for testing)
        public static void ResetData()
        {
            _invoices.Clear();
            _lineItems.Clear();
            _nextId = 1;
            _nextLineItemId = 1;
        }
    }
}