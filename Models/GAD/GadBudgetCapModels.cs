using System;

namespace SmkcApi.Models.GAD
{
    public class BudgetCapDto
    {
        public long CapId { get; set; }
        public int UlbCode { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public string FinYear { get; set; }
        public decimal? CapPercentage { get; set; }
        public decimal? CapAmount { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal EffectiveBudget { get; set; }
        public string Remarks { get; set; }
        public string EntBy { get; set; }
        public DateTime? EntDt { get; set; }
        public string LupBy { get; set; }
        public DateTime? LupDate { get; set; }
    }

    public class BudgetCapSaveRequest
    {
        public int UlbCode { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public decimal? CapPercentage { get; set; }
        public decimal? CapAmount { get; set; }
        public string Remarks { get; set; }
        public string ActionBy { get; set; }   // user login ID from session
    }

    public class BudgetCapHistoryDto
    {
        public long HistId { get; set; }
        public long CapId { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public decimal? OldCapPct { get; set; }
        public decimal? OldCapAmt { get; set; }
        public decimal? NewCapPct { get; set; }
        public decimal? NewCapAmt { get; set; }
        public string OldRemarks { get; set; }
        public string NewRemarks { get; set; }
        public string Action { get; set; }
        public string ActionBy { get; set; }
        public DateTime ActionDt { get; set; }
    }
}
