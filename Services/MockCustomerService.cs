using Invoqs.Models;

namespace Invoqs.Services
{
    public class MockCustomerService : ICustomerService
    {
        private static List<CustomerModel> _customers = new();
        private static int _nextId = 7; // Start from 7 since we have 6 initial customers

        static MockCustomerService()
        {
            LoadSampleData();
        }

        private static void LoadSampleData()
        {
            _customers = new List<CustomerModel>
            {
                new CustomerModel
                {
                    Id = 1,
                    Name = "ABC Construction Ltd",
                    Email = "contact@abcconstruction.co.uk",
                    Phone = "01234 567890",
                    ActiveJobs = 3,
                    CompletedJobs = 12,
                    TotalRevenue = 15500m,
                    CreatedDate = DateTime.Now.AddMonths(-6),
                    UpdatedDate = DateTime.Now.AddDays(-15),
                    CompanyRegistrationNumber = "12345678",
                    VatNumber = "GB123456789",
                    Notes = "Reliable customer with regular bookings. Prefers skip deliveries on weekdays."
                },
                new CustomerModel
                {
                    Id = 2,
                    Name = "Smith & Sons Builders",
                    Email = "info@smithbuilders.com",
                    Phone = "01234 567891",
                    ActiveJobs = 1,
                    CompletedJobs = 8,
                    TotalRevenue = 9200m,
                    CreatedDate = DateTime.Now.AddMonths(-3),
                    UpdatedDate = DateTime.Now.AddDays(-5),
                    Notes = "Family-run business. Often requires additional services."
                },
                new CustomerModel
                {
                    Id = 3,
                    Name = "Green Landscapes",
                    Email = "hello@greenlandscapes.co.uk",
                    Phone = "01234 567892",
                    ActiveJobs = 0,
                    CompletedJobs = 5,
                    TotalRevenue = 3400m,
                    CreatedDate = DateTime.Now.AddMonths(-2),
                    VatNumber = "GB987654321",
                    Notes = "Seasonal business - mainly active in spring and summer months."
                },
                new CustomerModel
                {
                    Id = 4,
                    Name = "Urban Development Corp",
                    Email = "projects@urbandevelopment.com",
                    Phone = "01234 567893",
                    ActiveJobs = 5,
                    CompletedJobs = 18,
                    TotalRevenue = 28900m,
                    CreatedDate = DateTime.Now.AddMonths(-8),
                    UpdatedDate = DateTime.Now.AddDays(-2),
                    CompanyRegistrationNumber = "87654321",
                    VatNumber = "GB111222333",
                    Notes = "Large commercial projects. Requires multiple skip sizes and frequent collections."
                },
                new CustomerModel
                {
                    Id = 5,
                    Name = "Residential Renovations",
                    Email = "bookings@resrenovations.co.uk",
                    Phone = "01234 567894",
                    ActiveJobs = 2,
                    CompletedJobs = 7,
                    TotalRevenue = 7800m,
                    CreatedDate = DateTime.Now.AddMonths(-4),
                    UpdatedDate = DateTime.Now.AddDays(-10),
                    CompanyRegistrationNumber = "11223344",
                    Notes = "Specializes in home renovations. Usually books multiple skips per project."
                },
                new CustomerModel
                {
                    Id = 6,
                    Name = "Coastal Builders",
                    Email = "team@coastalbuilders.com",
                    Phone = "01234 567895",
                    ActiveJobs = 1,
                    CompletedJobs = 4,
                    TotalRevenue = 5200m,
                    CreatedDate = DateTime.Now.AddMonths(-1),
                    VatNumber = "GB555666777",
                    Notes = "New customer. Focused on coastal property developments."
                }
            };
        }

        public async Task<List<CustomerModel>> GetAllCustomersAsync()
        {
            // Simulate API delay
            await Task.Delay(300);
            return _customers.ToList();
        }

        public async Task<CustomerModel?> GetCustomerByIdAsync(int id)
        {
            // Simulate API delay
            await Task.Delay(200);
            return _customers.FirstOrDefault(c => c.Id == id);
        }

        public async Task<CustomerModel> CreateCustomerAsync(CustomerModel customer)
        {
            // Simulate API delay
            await Task.Delay(500);

            customer.Id = _nextId++;
            customer.CreatedDate = DateTime.Now;
            customer.UpdatedDate = null;
            customer.ActiveJobs = 0;
            customer.CompletedJobs = 0;
            customer.TotalRevenue = 0;

            _customers.Add(customer);
            return customer;
        }

        public async Task<CustomerModel> UpdateCustomerAsync(CustomerModel customer)
        {
            // Simulate API delay
            await Task.Delay(500);

            var existingCustomer = _customers.FirstOrDefault(c => c.Id == customer.Id);
            if (existingCustomer != null)
            {
                // Update properties but preserve job stats and dates
                existingCustomer.Name = customer.Name;
                existingCustomer.Email = customer.Email;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.CompanyRegistrationNumber = customer.CompanyRegistrationNumber;
                existingCustomer.VatNumber = customer.VatNumber;
                existingCustomer.Notes = customer.Notes;
                existingCustomer.UpdatedDate = DateTime.Now;

                return existingCustomer;
            }

            throw new Exception("Customer not found");
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            // Simulate API delay
            await Task.Delay(500);

            var customer = _customers.FirstOrDefault(c => c.Id == id);
            if (customer != null)
            {
                _customers.Remove(customer);
                return true;
            }

            return false;
        }

        public async Task<bool> CustomerExistsAsync(int id)
        {
            // Simulate API delay
            await Task.Delay(100);
            return _customers.Any(c => c.Id == id);
        }

        public async Task<List<CustomerModel>> SearchCustomersAsync(string searchTerm)
        {
            // Simulate API delay
            await Task.Delay(200);

            if (string.IsNullOrWhiteSpace(searchTerm))
                return _customers.ToList();

            var lowerSearchTerm = searchTerm.ToLower();
            return _customers.Where(c =>
                c.Name.ToLower().Contains(lowerSearchTerm) ||
                c.Email.ToLower().Contains(lowerSearchTerm) ||
                c.Phone.Contains(searchTerm)
            ).ToList();
        }

        // Additional helper methods for mock data
        public void ResetData()
        {
            LoadSampleData();
            _nextId = 7;
        }

        public void AddRandomCustomer()
        {
            var random = new Random();
            var companies = new[]
            {
                "BuildTech Solutions", "Modern Constructions", "Elite Builders",
                "Premier Developments", "Quality Contractors", "Innovative Structures"
            };
            var domains = new[] { "co.uk", "com", "org.uk" };

            var companyName = companies[random.Next(companies.Length)];
            var domain = domains[random.Next(domains.Length)];

            var customer = new CustomerModel
            {
                Id = _nextId++,
                Name = companyName,
                Email = $"info@{companyName.ToLower().Replace(" ", "")}.{domain}",
                Phone = $"0{random.Next(1000, 9999)} {random.Next(100000, 999999)}",
                ActiveJobs = random.Next(0, 6),
                CompletedJobs = random.Next(0, 20),
                TotalRevenue = random.Next(1000, 50000),
                CreatedDate = DateTime.Now.AddDays(-random.Next(1, 365)),
                UpdatedDate = random.Next(0, 2) == 0 ? DateTime.Now.AddDays(-random.Next(1, 30)) : null,
                Notes = "Auto-generated customer for testing purposes."
            };

            _customers.Add(customer);
        }
    }
}