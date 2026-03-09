namespace Invoqs.Models
{
    public class DashboardDataModel
    {
        // Revenue metrics
        public decimal MonthRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }

        // Job metrics
        public int TotalJobs { get; set; }
        public int UninvoicedJobs { get; set; }
        public int JobsScheduledToday { get; set; }
        public int SkipRentals { get; set; }
        public int SandDeliveries { get; set; }
        public int ForkLiftServices { get; set; }
        public int Transfers { get; set; }
        public int SellForklifts { get; set; }

        // Customer metrics
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }

        // Invoice metrics
        public int PendingInvoices { get; set; }
        public decimal PendingAmount { get; set; }
    }
}