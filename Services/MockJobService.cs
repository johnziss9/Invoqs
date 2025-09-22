using Invoqs.Models;

namespace Invoqs.Services
{
    public class MockJobService : IJobService
    {
        private static List<JobModel> _jobs = new();
        private static int _nextId = 23; // Start from 23 to avoid conflicts with existing IDs

        static MockJobService()
        {
            LoadSampleData();
        }

        private static void LoadSampleData()
        {
            _jobs = new List<JobModel>
    {
        // ABC Construction Ltd (Customer ID: 1) - 3 Active, 12 Completed
        new JobModel
        {
            Id = 1,
            CustomerId = 1,
            Title = "8-Yard Skip Rental",
            Description = "Large skip rental for office building demolition debris",
            Type = JobType.SkipRental,
            Status = JobStatus.Active,
            Address = "123 Business Park, London, SW1A 1AA",
            StartDate = DateTime.Now.AddDays(-5),
            Price = 225.00m,
            CreatedDate = DateTime.Now.AddDays(-10),
            Notes = "8-yard skip, access via rear entrance only"
        },
        new JobModel
        {
            Id = 2,
            CustomerId = 1,
            Title = "12-Yard Skip Rental",
            Description = "Second large skip for additional demolition waste",
            Type = JobType.SkipRental,
            Status = JobStatus.Active,
            Address = "123 Business Park, London, SW1A 1AA", // Same address as Job 1
            StartDate = DateTime.Now.AddDays(-3),
            Price = 280.00m,
            CreatedDate = DateTime.Now.AddDays(-8),
            Notes = "12-yard skip, same location as first skip"
        },
        new JobModel
        {
            Id = 3,
            CustomerId = 1,
            Title = "6-Yard Skip Rental - Warehouse",
            Description = "Skip rental for warehouse clearance project",
            Type = JobType.SkipRental,
            Status = JobStatus.Active,
            Address = "45 Industrial Estate, Birmingham, B12 0XY",
            StartDate = DateTime.Now.AddDays(-2),
            Price = 180.00m,
            CreatedDate = DateTime.Now.AddDays(-7),
            Notes = "Weekly collection required"
        },
        new JobModel
        {
            Id = 4,
            CustomerId = 1,
            Title = "Sharp Sand Delivery - 10 Tons",
            Description = "Sand delivery for foundation preparation",
            Type = JobType.SandDelivery,
            Status = JobStatus.New,
            Address = "78 Construction Lane, Manchester, M1 5DR",
            StartDate = DateTime.Now.AddDays(3),
            Price = 180.00m,
            CreatedDate = DateTime.Now.AddDays(-3),
            Notes = "Sharp sand, 10 tons"
        },
        new JobModel
        {
            Id = 5,
            CustomerId = 1,
            Title = "Building Sand Delivery - 5 Tons",
            Description = "Additional sand delivery for same project",
            Type = JobType.SandDelivery,
            Status = JobStatus.New,
            Address = "78 Construction Lane, Manchester, M1 5DR", // Same address as Job 4
            StartDate = DateTime.Now.AddDays(5),
            Price = 100.00m,
            CreatedDate = DateTime.Now.AddDays(-2),
            Notes = "Building sand, 5 tons, same site"
        },
        new JobModel
        {
            Id = 6,
            CustomerId = 1,
            Title = "Shopping Centre Renovation",
            Description = "Skip rental for shopping centre renovation waste",
            Type = JobType.SkipRental,
            Status = JobStatus.Completed,
            Address = "99 High Street, Leeds, LS1 4DY",
            StartDate = DateTime.Now.AddDays(-30),
            EndDate = DateTime.Now.AddDays(-15),
            Price = 380.00m,
            CreatedDate = DateTime.Now.AddDays(-35),
            UpdatedDate = DateTime.Now.AddDays(-15),
            Notes = "Successfully completed, excellent service"
        },

        // Smith & Sons Builders (Customer ID: 2) - 1 Active, 8 Completed
        new JobModel
        {
            Id = 7,
            CustomerId = 2,
            Title = "House Extension Project",
            Description = "Skip rental for house extension construction waste",
            Type = JobType.SkipRental,
            Status = JobStatus.Active,
            Address = "12 Maple Avenue, Bristol, BS1 6QA",
            StartDate = DateTime.Now.AddDays(-8),
            Price = 220.00m,
            CreatedDate = DateTime.Now.AddDays(-12),
            Notes = "Residential area, quiet delivery required before 9am"
        },
        new JobModel
        {
            Id = 8,
            CustomerId = 2,
            Title = "Garden Landscaping - Sand Supply",
            Description = "Sand delivery for garden landscaping project",
            Type = JobType.SandDelivery,
            Status = JobStatus.Completed,
            Address = "34 Oak Road, Bristol, BS2 8LM",
            StartDate = DateTime.Now.AddDays(-25),
            EndDate = DateTime.Now.AddDays(-20),
            Price = 180.00m,
            CreatedDate = DateTime.Now.AddDays(-30),
            UpdatedDate = DateTime.Now.AddDays(-20),
            Notes = "Delivered on time, customer satisfied"
        },

        // Green Landscapes (Customer ID: 3) - 0 Active, 5 Completed
        new JobModel
        {
            Id = 9,
            CustomerId = 3,
            Title = "Park Renovation Project",
            Description = "Skip rental for park renovation debris removal",
            Type = JobType.SkipRental,
            Status = JobStatus.Completed,
            Address = "Green Park, 56 Park Lane, Liverpool, L1 8JQ",
            StartDate = DateTime.Now.AddDays(-60),
            EndDate = DateTime.Now.AddDays(-45),
            Price = 340.00m,
            CreatedDate = DateTime.Now.AddDays(-65),
            UpdatedDate = DateTime.Now.AddDays(-45),
            Notes = "Large landscaping project completed successfully"
        },
        new JobModel
        {
            Id = 10,
            CustomerId = 3,
            Title = "Golf Course Maintenance",
            Description = "Sand delivery for golf course bunker maintenance",
            Type = JobType.SandDelivery,
            Status = JobStatus.Completed,
            Address = "Riverside Golf Club, Golf Course Drive, Liverpool, L18 3JT",
            StartDate = DateTime.Now.AddDays(-40),
            EndDate = DateTime.Now.AddDays(-38),
            Price = 420.00m,
            CreatedDate = DateTime.Now.AddDays(-45),
            UpdatedDate = DateTime.Now.AddDays(-38),
            Notes = "Specialized bunker sand delivered"
        },

        // Urban Development Corp (Customer ID: 4) - 5 Active, 18 Completed
        new JobModel
        {
            Id = 11,
            CustomerId = 4,
            Title = "High-Rise Construction Phase 2",
            Description = "Multiple large skip rental for high-rise construction",
            Type = JobType.SkipRental,
            Status = JobStatus.Active,
            Address = "Tower Development Site, 100 Skyline Avenue, London, E14 5AB",
            StartDate = DateTime.Now.AddDays(-15),
            Price = 650.00m,
            CreatedDate = DateTime.Now.AddDays(-20),
            Notes = "Ongoing project, weekly collection schedule"
        },
        new JobModel
        {
            Id = 12,
            CustomerId = 4,
            Title = "Commercial Complex - Foundation Work",
            Description = "Sand delivery for commercial complex foundation",
            Type = JobType.SandDelivery,
            Status = JobStatus.Active,
            Address = "Commerce Plaza, 200 Business Street, London, EC2A 3HP",
            StartDate = DateTime.Now.AddDays(-10),
            Price = 850.00m,
            CreatedDate = DateTime.Now.AddDays(-15),
            Notes = "Large commercial project, multiple deliveries required"
        },
        new JobModel
        {
            Id = 13,
            CustomerId = 4,
            Title = "Coastal Defense Project",
            Description = "Fork lift service for coastal defense installation",
            Type = JobType.ForkLiftService,
            Status = JobStatus.Active,
            Address = "Cliff Edge, Coastal Road, Dover, CT16 1QQ",
            StartDate = DateTime.Now.AddDays(-7),
            Price = 1200.00m,
            CreatedDate = DateTime.Now.AddDays(-12),
            Notes = "Specialized coastal cliff stabilization work"
        },
        new JobModel
        {
            Id = 14,
            CustomerId = 4,
            Title = "Shopping Mall Extension - Waste Management",
            Description = "Comprehensive waste management for mall extension",
            Type = JobType.SkipRental,
            Status = JobStatus.New,
            Address = "Westfield Shopping Centre, Mall Drive, London, W12 7GF",
            StartDate = DateTime.Now.AddDays(5),
            Price = 750.00m,
            CreatedDate = DateTime.Now.AddDays(-2),
            Notes = "Large scale project starting next week"
        },
        new JobModel
        {
            Id = 15,
            CustomerId = 4,
            Title = "Infrastructure Project - Cliff Reinforcement",
            Description = "Fork lift service for infrastructure project",
            Type = JobType.ForkLiftService,
            Status = JobStatus.New,
            Address = "Highway A1, Cliff Section, Newcastle, NE1 4ST",
            StartDate = DateTime.Now.AddDays(10),
            Price = 980.00m,
            CreatedDate = DateTime.Now.AddDays(-1),
            Notes = "Highway cliff stabilization project"
        },

        // Residential Renovations (Customer ID: 5) - 2 Active, 7 Completed
        new JobModel
        {
            Id = 16,
            CustomerId = 5,
            Title = "Victorian House Restoration",
            Description = "Skip rental for Victorian house restoration project",
            Type = JobType.SkipRental,
            Status = JobStatus.Active,
            Address = "45 Victoria Terrace, Bath, BA1 5LZ",
            StartDate = DateTime.Now.AddDays(-6),
            Price = 290.00m,
            CreatedDate = DateTime.Now.AddDays(-10),
            Notes = "Heritage property, careful handling required"
        },
        new JobModel
        {
            Id = 17,
            CustomerId = 5,
            Title = "Kitchen Extension - Sand Supply",
            Description = "Sand delivery for kitchen extension foundation",
            Type = JobType.SandDelivery,
            Status = JobStatus.Active,
            Address = "78 Garden Close, Bath, BA2 4JH",
            StartDate = DateTime.Now.AddDays(-3),
            Price = 160.00m,
            CreatedDate = DateTime.Now.AddDays(-7),
            Notes = "Building sand for residential extension"
        },
        new JobModel
        {
            Id = 18,
            CustomerId = 5,
            Title = "Bathroom Renovation",
            Description = "Skip rental for bathroom renovation waste",
            Type = JobType.SkipRental,
            Status = JobStatus.Completed,
            Address = "23 Elm Street, Bath, BA1 2QS",
            StartDate = DateTime.Now.AddDays(-35),
            EndDate = DateTime.Now.AddDays(-30),
            Price = 180.00m,
            CreatedDate = DateTime.Now.AddDays(-40),
            UpdatedDate = DateTime.Now.AddDays(-30),
            Notes = "Small residential job completed efficiently"
        },

        // Coastal Builders (Customer ID: 6) - 1 Active, 4 Completed
        new JobModel
        {
            Id = 19,
            CustomerId = 6,
            Title = "Beachfront Property Development",
            Description = "Comprehensive services for beachfront development",
            Type = JobType.ForkLiftService,
            Status = JobStatus.Active,
            Address = "Seafront Development, Ocean View, Brighton, BN1 2FU",
            StartDate = DateTime.Now.AddDays(-4),
            Price = 1450.00m,
            CreatedDate = DateTime.Now.AddDays(-8),
            Notes = "Complex coastal development project"
        },
        new JobModel
        {
            Id = 20,
            CustomerId = 6,
            Title = "Coastal House - Foundation Sand",
            Description = "Sand delivery for coastal house foundation",
            Type = JobType.SandDelivery,
            Status = JobStatus.Completed,
            Address = "12 Cliff Top Road, Brighton, BN2 5EG",
            StartDate = DateTime.Now.AddDays(-20),
            EndDate = DateTime.Now.AddDays(-18),
            Price = 240.00m,
            CreatedDate = DateTime.Now.AddDays(-25),
            UpdatedDate = DateTime.Now.AddDays(-18),
            Notes = "Marine-grade sand delivered for coastal construction"
        },

        // Additional jobs for variety
        new JobModel
        {
            Id = 21,
            CustomerId = 2,
            Title = "Garden Wall Construction",
            Description = "Skip rental for garden wall construction debris",
            Type = JobType.SkipRental,
            Status = JobStatus.Cancelled,
            Address = "67 Rose Lane, Bristol, BS3 4HG",
            StartDate = DateTime.Now.AddDays(-14),
            Price = 150.00m,
            CreatedDate = DateTime.Now.AddDays(-18),
            UpdatedDate = DateTime.Now.AddDays(-12),
            Notes = "Cancelled due to planning permission issues"
        },
        new JobModel
        {
            Id = 22,
            CustomerId = 3,
            Title = "Cemetery Maintenance",
            Description = "Skip rental for cemetery maintenance work",
            Type = JobType.SkipRental,
            Status = JobStatus.Cancelled,
            Address = "St. Mary's Cemetery, Church Lane, Liverpool, L25 6DA",
            StartDate = DateTime.Now.AddDays(-50),
            Price = 200.00m,
            CreatedDate = DateTime.Now.AddDays(-55),
            UpdatedDate = DateTime.Now.AddDays(-48),
            Notes = "Project cancelled due to weather conditions"
        }
    };
        }

        public async Task<List<JobModel>> GetAllJobsAsync()
        {
            await Task.Delay(300);
            return _jobs.ToList();
        }

        public async Task<List<JobModel>> GetJobsByCustomerIdAsync(int customerId)
        {
            await Task.Delay(200);
            return _jobs.Where(j => j.CustomerId == customerId).ToList();
        }

        public async Task<JobModel?> GetJobByIdAsync(int jobId)
        {
            await Task.Delay(150);
            return _jobs.FirstOrDefault(j => j.Id == jobId);
        }

        public async Task<JobModel> CreateJobAsync(JobModel job)
        {
            await Task.Delay(500);

            job.Id = _nextId++;
            job.CreatedDate = DateTime.Now;
            job.UpdatedDate = null;

            _jobs.Add(job);
            return job;
        }

        public async Task<bool> UpdateJobAsync(JobModel job)
        {
            await Task.Delay(400);

            var existingJob = _jobs.FirstOrDefault(j => j.Id == job.Id);
            if (existingJob != null)
            {
                existingJob.Title = job.Title;
                existingJob.Description = job.Description;
                existingJob.Type = job.Type;
                existingJob.Status = job.Status;
                existingJob.Address = job.Address;
                existingJob.StartDate = job.StartDate;
                existingJob.EndDate = job.EndDate;
                existingJob.Price = job.Price;
                existingJob.Notes = job.Notes;

                // Preserve invoice properties - only update if explicitly provided
                if (job.IsInvoiced != existingJob.IsInvoiced)
                    existingJob.IsInvoiced = job.IsInvoiced;
                if (job.InvoiceId != existingJob.InvoiceId)
                    existingJob.InvoiceId = job.InvoiceId;
                if (job.InvoicedDate != existingJob.InvoicedDate)
                    existingJob.InvoicedDate = job.InvoicedDate;

                existingJob.UpdatedDate = DateTime.Now;

                return true;
            }

            return false;
        }

        public async Task<bool> DeleteJobAsync(int jobId)
        {
            await Task.Delay(400);

            var job = _jobs.FirstOrDefault(j => j.Id == jobId);
            if (job != null)
            {
                _jobs.Remove(job);
                return true;
            }

            return false;
        }

        public async Task<List<JobModel>> GetJobsByStatusAsync(JobStatus status)
        {
            await Task.Delay(200);
            return _jobs.Where(j => j.Status == status).ToList();
        }

        public async Task<List<JobModel>> GetJobsByTypeAsync(JobType type)
        {
            await Task.Delay(200);
            return _jobs.Where(j => j.Type == type).ToList();
        }

        public async Task<List<JobModel>> GetJobsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await Task.Delay(250);
            return _jobs.Where(j => j.StartDate >= startDate && j.StartDate <= endDate).ToList();
        }

        public async Task<decimal> GetTotalRevenueByCustomerAsync(int customerId)
        {
            await Task.Delay(150);
            return _jobs.Where(j => j.CustomerId == customerId && j.Status == JobStatus.Completed)
                       .Sum(j => j.Price);
        }

        public async Task<int> GetActiveJobCountByCustomerAsync(int customerId)
        {
            await Task.Delay(100);
            return _jobs.Count(j => j.CustomerId == customerId && j.Status == JobStatus.Active);
        }

        public async Task<int> GetCompletedJobCountByCustomerAsync(int customerId)
        {
            await Task.Delay(100);
            return _jobs.Count(j => j.CustomerId == customerId && j.Status == JobStatus.Completed);
        }

        // New methods for address grouping
        public async Task<List<AddressJobGroup>> GetJobsGroupedByAddressAsync(int customerId)
        {
            await Task.Delay(200);

            var customerJobs = _jobs.Where(j => j.CustomerId == customerId).ToList();
            var groupedJobs = customerJobs
                .GroupBy(j => j.Address)
                .Select(g => new AddressJobGroup
                {
                    Address = g.Key,
                    Jobs = g.OrderBy(j => j.StartDate).ToList()
                })
                .OrderBy(g => g.Address)
                .ToList();

            return groupedJobs;
        }

        public async Task<List<JobModel>> GetJobsByAddressAsync(int customerId, string address)
        {
            await Task.Delay(150);
            return _jobs.Where(j => j.CustomerId == customerId && j.Address.Equals(address, StringComparison.OrdinalIgnoreCase))
                       .OrderBy(j => j.StartDate)
                       .ToList();
        }

        public async Task<AddressJobGroup?> GetAddressJobGroupAsync(int customerId, string address)
        {
            await Task.Delay(150);

            var addressJobs = _jobs.Where(j => j.CustomerId == customerId &&
                                              j.Address.Equals(address, StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(j => j.StartDate)
                                   .ToList();

            if (!addressJobs.Any())
                return null;

            return new AddressJobGroup
            {
                Address = address,
                Jobs = addressJobs
            };
        }

        // Additional helper methods for mock data
        public void ResetData()
        {
            LoadSampleData();
            _nextId = 21;
        }

        public void AddRandomJob(int customerId)
        {
            var random = new Random();
            var jobTitles = new[]
            {
                "Construction Site Clearance", "Renovation Project", "Garden Landscaping",
                "Building Extension", "Demolition Work", "Site Preparation",
                "Waste Management", "Foundation Work", "Road Construction"
            };

            var addresses = new[]
            {
                "123 High Street, London, SW1A 1AA",
                "45 Oak Avenue, Manchester, M1 5DR",
                "78 Mill Lane, Birmingham, B12 0XY",
                "12 Park Road, Leeds, LS1 4DY",
                "34 Church Street, Bristol, BS1 6QA"
            };

            var jobTypes = Enum.GetValues<JobType>();
            var jobStatuses = Enum.GetValues<JobStatus>();

            var job = new JobModel
            {
                Id = _nextId++,
                CustomerId = customerId,
                Title = jobTitles[random.Next(jobTitles.Length)],
                Description = $"Auto-generated job for testing purposes",
                Type = jobTypes[random.Next(jobTypes.Length)],
                Status = jobStatuses[random.Next(jobStatuses.Length)],
                Address = addresses[random.Next(addresses.Length)],
                StartDate = DateTime.Now.AddDays(-random.Next(0, 60)),
                Price = random.Next(100, 1000),
                CreatedDate = DateTime.Now.AddDays(-random.Next(1, 70)),
                Notes = "Auto-generated for testing"
            };

            if (job.Status == JobStatus.Completed)
            {
                job.EndDate = job.StartDate.AddDays(random.Next(1, 30));
                job.UpdatedDate = job.EndDate;
            }

            _jobs.Add(job);
        }
        
        public async Task<List<JobModel>> GetCompletedUninvoicedJobsAsync()
        {
            await Task.Delay(150);
            return _jobs.Where(j => j.CanBeInvoiced).OrderBy(j => j.StartDate).ToList();
        }

        public async Task<List<JobModel>> GetCompletedUninvoicedJobsByCustomerAsync(int customerId)
        {
            await Task.Delay(150);
            return _jobs.Where(j => j.CustomerId == customerId && j.CanBeInvoiced)
                    .OrderBy(j => j.StartDate).ToList();
        }

        public async Task<List<JobModel>> GetCompletedUninvoicedJobsByAddressAsync(int customerId, string address)
        {
            await Task.Delay(150);
            return _jobs.Where(j => j.CustomerId == customerId && 
                                j.Address.Equals(address, StringComparison.OrdinalIgnoreCase) && 
                                j.CanBeInvoiced)
                    .OrderBy(j => j.StartDate).ToList();
        }

        public async Task<List<JobModel>> GetJobsByInvoiceIdAsync(int invoiceId)
        {
            await Task.Delay(150);
            return _jobs.Where(j => j.InvoiceId == invoiceId).OrderBy(j => j.StartDate).ToList();
        }

        public async Task<bool> MarkJobsAsInvoicedAsync(List<int> jobIds, int invoiceId)
        {
            await Task.Delay(200);
            
            bool anyUpdated = false;
            foreach (var jobId in jobIds)
            {
                var job = _jobs.FirstOrDefault(j => j.Id == jobId);
                if (job != null && job.CanBeInvoiced)
                {
                    job.MarkAsInvoiced(invoiceId);
                    anyUpdated = true;
                }
            }
            
            return anyUpdated;
        }

        public async Task<bool> RemoveJobsFromInvoiceAsync(List<int> jobIds)
        {
            await Task.Delay(200);
            
            bool anyUpdated = false;
            foreach (var jobId in jobIds)
            {
                var job = _jobs.FirstOrDefault(j => j.Id == jobId);
                if (job != null && job.IsInvoiced)
                {
                    job.RemoveFromInvoice();
                    anyUpdated = true;
                }
            }
            
            return anyUpdated;
        }

        public async Task<bool> CanJobBeInvoicedAsync(int jobId)
        {
            await Task.Delay(50);
            var job = _jobs.FirstOrDefault(j => j.Id == jobId);
            return job?.CanBeInvoiced ?? false;
        }

        public async Task<bool> CanJobsBeInvoicedAsync(List<int> jobIds)
        {
            await Task.Delay(100);
            
            foreach (var jobId in jobIds)
            {
                var job = _jobs.FirstOrDefault(j => j.Id == jobId);
                if (job == null || !job.CanBeInvoiced)
                    return false;
            }
            
            return jobIds.Any(); // Return true only if there are jobs and all can be invoiced
        }

        public async Task<List<AddressJobGroup>> GetUninvoicedJobsGroupedByAddressAsync(int customerId)
        {
            await Task.Delay(200);
            
            var customerJobs = _jobs.Where(j => j.CustomerId == customerId && j.CanBeInvoiced).ToList();
            var groupedJobs = customerJobs
                .GroupBy(j => j.Address)
                .Select(g => new AddressJobGroup
                {
                    Address = g.Key,
                    Jobs = g.OrderBy(j => j.StartDate).ToList()
                })
                .Where(g => g.HasJobsToInvoice) // Only groups with invoiceable jobs
                .OrderBy(g => g.Address)
                .ToList();
            
            return groupedJobs;
        }

        public async Task<AddressJobGroup?> GetUninvoicedAddressJobGroupAsync(int customerId, string address)
        {
            await Task.Delay(150);
            
            var addressJobs = _jobs.Where(j => j.CustomerId == customerId &&
                                            j.Address.Equals(address, StringComparison.OrdinalIgnoreCase) &&
                                            j.CanBeInvoiced)
                                .OrderBy(j => j.StartDate)
                                .ToList();
            
            if (!addressJobs.Any())
                return null;
            
            return new AddressJobGroup
            {
                Address = address,
                Jobs = addressJobs
            };
        }
    }
}