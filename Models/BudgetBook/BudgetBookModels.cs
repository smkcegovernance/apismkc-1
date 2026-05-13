using System;
using System.Collections.Generic;

namespace SmkcApi.Models.BudgetBook
{
    // ── Dropdown DTOs ────────────────────────────────────────────────────────────

    public class DepartmentDto
    {
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string DeptNameLL { get; set; }
        public string DeptNameLLUnicode { get; set; }
    }

    public class SubheadDto
    {
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public string AcSubheadNameLL { get; set; }
        public string AcSubheadNameLLUnicode { get; set; }
        public decimal TotalBudget { get; set; }
    }

    // ── Budget Remaining ─────────────────────────────────────────────────────────

    public class BudgetRemainingDto
    {
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal EffectiveBudget { get; set; }   // After cap applied
        public decimal? CapPercentage { get; set; }
        public decimal? CapAmount { get; set; }
        public decimal CommittedAmount { get; set; }
        public decimal RemainingBudget { get; set; }   // EffectiveBudget - CommittedAmount
    }

    // ── Request DTOs ─────────────────────────────────────────────────────────────

    public class PrimaryBudgetEntryRequest
    {
        public int DeptCode { get; set; }
        public string WorkName { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public decimal ProposedAmount { get; set; }
        public long NastiNo { get; set; }
        public string FileType { get; set; }
        public string EnteredBy { get; set; }
    }

    public class FinalBudgetEntryRequest
    {
        public long BookEntryNo { get; set; }
        public decimal FinalProposedAmount { get; set; }
        public string EnteredBy { get; set; }
    }

    // ── Response DTOs ────────────────────────────────────────────────────────────

    public class BudgetBookEntryDto
    {
        public long BookEntryNo { get; set; }
        public long FinalBookEntryNo { get; set; }
        public string FinYear { get; set; }
        public string AcHead { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public decimal ProposedWorkAmount { get; set; }
        public decimal FinalProposedWorkAmount { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal RemainingBudgetAmount { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EntryDate { get; set; }
        public string FinalEnteredBy { get; set; }
        public DateTime? FinalEntryDate { get; set; }
        public string Status { get; set; }
        public string Cancelled { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string WorkName { get; set; }
        public long NastiNo { get; set; }
        public string FileType { get; set; }
    }

    public class BudgetBookSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long BookEntryNo { get; set; }
        public long FinalBookEntryNo { get; set; }
        public decimal RemainingBudget { get; set; }
    }

    public class BudgetBookListResult
    {
        public bool Success { get; set; }
        public List<BudgetBookEntryDto> Entries { get; set; }
        public int TotalCount { get; set; }
    }
}
