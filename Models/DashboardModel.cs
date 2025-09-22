namespace Invoqs.Models
{
    public class DashboardDataModel
    {
        // Revenue metrics
        public decimal WeekRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }

        // Job metrics
        public int ActiveJobs { get; set; }
        public int JobsScheduledToday { get; set; }
        public int SkipRentals { get; set; }
        public int SandDeliveries { get; set; }
        public int ForkLiftServices { get; set; }

        // Customer metrics
        public int TotalCustomers { get; set; }
        public int NewCustomersThisWeek { get; set; }

        // Invoice metrics
        public int PendingInvoices { get; set; }
        public decimal PendingAmount { get; set; }
    }
}