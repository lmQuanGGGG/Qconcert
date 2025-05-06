namespace Qconcert.ViewModels
{
    public class RevenueStatisticsViewModel
    {
        public decimal VipRevenue { get; set; }
        public decimal NormalRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Period { get; set; } // Ngày, tuần, tháng, quý, năm
    }
}