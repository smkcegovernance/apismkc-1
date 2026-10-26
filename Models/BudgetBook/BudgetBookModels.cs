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
        public decimal EffectiveBudget { get; set; }   // For compatibility; budget-book remaining uses TotalBudget basis
        public decimal? CapPercentage { get; set; }
        public decimal? CapAmount { get; set; }
        public decimal CommittedAmount { get; set; }
        public decimal RemainingBudget { get; set; }   // TotalBudget - CommittedAmount
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

    public class FinalBudgetRevisionRequest
    {
        public long BookEntryNo { get; set; }
        public decimal RevisedFinalAmount { get; set; }
        public string RevisionRemark { get; set; }
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
        public decimal? PreviousFinalAmount { get; set; }
        public decimal? RevisedFinalAmount { get; set; }
        public string RevisionRemark { get; set; }
        public DateTime? RevisionDate { get; set; }
    }

    public class BudgetBookSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long BookEntryNo { get; set; }
        public long FinalBookEntryNo { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal PreviousRemainingBudget { get; set; }
        public decimal PreviousFinalAmount { get; set; }
        public decimal RevisedFinalAmount { get; set; }
    }

    public class BudgetBookListResult
    {
        public bool Success { get; set; }
        public List<BudgetBookEntryDto> Entries { get; set; }
        public int TotalCount { get; set; }
    }

    public class BudgetLiabilityReportRowDto
    {
        public string FinYear { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public decimal TotalBudgetAmount { get; set; }
        public decimal TotalExpenditureAmount { get; set; }
        public decimal BudgetBalanceAmount { get; set; }
    }

    public class BudgetLiabilityReportResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<BudgetLiabilityReportRowDto> Rows { get; set; }
        public decimal GrandTotalBudget { get; set; }
        public decimal GrandTotalExpenditure { get; set; }
        public decimal GrandTotalBalance { get; set; }
    }

    public class FundWiseBudgetLiabilityReportFundDto
    {
        public string FundCode { get; set; }
        public string FundName { get; set; }
        public decimal TotalBudgetAmount { get; set; }
        public decimal TotalExpenditureAmount { get; set; }
        public decimal BudgetBalanceAmount { get; set; }
        public List<BudgetLiabilityReportRowDto> Details { get; set; }
    }

    public class FundWiseBudgetLiabilityReportResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<FundWiseBudgetLiabilityReportFundDto> Funds { get; set; }
        public decimal GrandTotalBudget { get; set; }
        public decimal GrandTotalExpenditure { get; set; }
        public decimal GrandTotalBalance { get; set; }
        public int UnmappedBudgetHeadCount { get; set; }
    }

    public class BillPaymentReportRowDto
    {
        public string FinYear { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public decimal TotalBudgetAmount { get; set; }
        public decimal TotalExpenditureAmount { get; set; }
        public decimal BudgetBalanceAmount { get; set; }
    }

    public class BillPaymentReportResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<BillPaymentReportRowDto> Rows { get; set; }
        public decimal GrandTotalBudget { get; set; }
        public decimal GrandTotalExpenditure { get; set; }
        public decimal GrandTotalBalance { get; set; }
    }

    public class WardWiseExpenditureReportRowDto
    {
        public string FinYear { get; set; }
        public string DeptName { get; set; }
        public long NastiNo { get; set; }
        public long BookEntryNo { get; set; }
        public long FinalBookEntryNo { get; set; }
        public string WorkName { get; set; }
        public string WardNo { get; set; }
        public decimal ProposedCost { get; set; }
        public string BudgetHeadCode { get; set; }
        public string BudgetHeadName { get; set; }
        public string FundName { get; set; }
    }

    public class WardWiseExpenditureReportResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FinYear { get; set; }
        public string WardNo { get; set; }
        public string WardScope { get; set; }
        public string DeptNameFilter { get; set; }
        public List<WardWiseExpenditureReportRowDto> Rows { get; set; }
        public decimal TotalProposedCost { get; set; }
    }
}