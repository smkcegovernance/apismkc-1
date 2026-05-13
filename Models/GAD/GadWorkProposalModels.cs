using System;
using System.Collections.Generic;

namespace SmkcApi.Models.GAD
{
    // ── Dropdown DTOs ────────────────────────────────────────────────────────────

    public class GadSubheadDto
    {
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public string AcSubheadNameLL { get; set; }
        public string AcSubheadNameLLUnicode { get; set; }
    }

    public class GadBudgetInfoDto
    {
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal EffectiveBudget { get; set; }    // After cap applied
        public decimal ActualExpenditure { get; set; }  // Committed from GAD tables
        public decimal RemainingBudget { get; set; }    // EffectiveBudget - ActualExpenditure
        public decimal? CapPercentage { get; set; }     // null if no cap
        public decimal? CapAmount { get; set; }         // null if no cap
    }

    // ── Work Proposal Request DTOs ────────────────────────────────────────────────

    /// <summary>
    /// Shared fields for both Quotation and Tender work proposal requests.
    /// Mirrors all fields in the legacy WorkProposalEntry.aspx form.
    /// </summary>
    public abstract class WorkProposalRequestBase
    {
        // ── Core ─────────────────────────────────────────────────────────────────
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string NastiType { get; set; }
        public string NastiNo { get; set; }

        // ── Work description ─────────────────────────────────────────────────────
        public string WorkName { get; set; }
        public string WorkPlace { get; set; }
        /// <summary>o = आहे, n = नाही</summary>
        public string MapAttached { get; set; }
        /// <summary>Comma-separated ward numbers, e.g. "1,3,15"</summary>
        public string WardNos { get; set; }
        public string WorkNeed { get; set; }
        public string WorkDoneBefore { get; set; }
        /// <summary>कामगिरीसाठी अपेक्षित खर्च (txtExpectedCost)</summary>
        public decimal WorkAmount { get; set; }

        // ── Technical sanction ───────────────────────────────────────────────────
        /// <summary>y/n</summary>
        public string TechApproval { get; set; }
        public string TechSanctionNo { get; set; }
        public string TechSanctionDate { get; set; }
        /// <summary>DSR rates confirmed — y/n</summary>
        public string DsrRates { get; set; }

        // ── Land / place ─────────────────────────────────────────────────────────
        /// <summary>o/n</summary>
        public string PlaceOwnership { get; set; }
        public string NocDocAttached { get; set; }
        public string NocCertificate { get; set; }
        public string AnyDispute { get; set; }
        public string CourtCase { get; set; }
        public string CaseDetails { get; set; }

        // ── Construction / town planning ─────────────────────────────────────────
        public string TownPlanCheck { get; set; }
        public string TownPlanApproval { get; set; }
        public string ExpendValid { get; set; }
        public string StockListAttached { get; set; }
        public string PhotoAttached { get; set; }

        // ── Account head & budget ────────────────────────────────────────────────
        public string AcSubhead { get; set; }
        /// <summary>प्रस्तावित कामाचा खर्च</summary>
        public decimal ProposalCost { get; set; }

        // ── Compliance ───────────────────────────────────────────────────────────
        public string AcHeadValid { get; set; }
        public string OtherDept { get; set; }
        /// <summary>y/n</summary>
        public string WorkSplit { get; set; }
        public string MaintenancePeriod { get; set; }
        public string PrevMaintenance { get; set; }
        public string CompetentOfficer { get; set; }

        // ── Meta ─────────────────────────────────────────────────────────────────
        public string Remarks { get; set; }
        public string EnteredBy { get; set; }

        /// <summary>
        /// Optional: pre-generated sequence number (fetched via next-sequence endpoint).
        /// When > 0 the save will use this as ORDER_NO instead of calling NEXTVAL again.
        /// </summary>
        public long PreGeneratedOrderNo { get; set; }
    }

    /// <summary>
    /// Request body for saving a quotation (under ₹10 lakh) work proposal.
    /// Maps to GADQUOTATIONORDERSDET.
    /// </summary>
    public class Under10LProposalRequest : WorkProposalRequestBase
    {
        /// <summary>
        /// INWARD_TYPE value stored in GADQUOTATIONORDERSDET.
        /// Use "quotation" for standard darpatra proposals, "other" for other proposals.
        /// Defaults to "quotation" when null/empty.
        /// </summary>
        public string InwardType { get; set; }
    }

    /// <summary>
    /// Request body for saving a tender (over ₹10 lakh) work proposal.
    /// Maps to GADTENDERORDERSDET.
    /// </summary>
    public class Over10LProposalRequest : WorkProposalRequestBase { }

    // ── Response DTOs ─────────────────────────────────────────────────────────────

    public class WorkProposalSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long OrderNo { get; set; }
    }

    public class WorkProposalDto
    {
        public long OrderNo { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string NastiNo { get; set; }
        public string NastiType { get; set; }
        public string WorkName { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public string WardNos { get; set; }
        public decimal WorkAmount { get; set; }
        public decimal ProposalCost { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EntryDate { get; set; }
        public string ProposalType { get; set; }
    }

    /// <summary>Full detail DTO used for print report.</summary>
    public class WorkProposalDetailDto
    {
        public long OrderNo { get; set; }
        public string ProposalType { get; set; }   // 'Q' = quotation, 'T' = tender
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string NastiType { get; set; }
        public string NastiNo { get; set; }

        // Work description
        public string WorkName { get; set; }
        public string WorkPlace { get; set; }
        public string MapAttached { get; set; }
        public string WardNos { get; set; }
        public string WorkNeed { get; set; }
        public string WorkDoneBefore { get; set; }
        public decimal WorkAmount { get; set; }

        // Technical sanction
        public string TechApproval { get; set; }
        public string TechSanctionNo { get; set; }
        public string TechSanctionDate { get; set; }
        public string DsrRates { get; set; }

        // Land / place
        public string PlaceOwnership { get; set; }
        public string NocDocAttached { get; set; }
        public string NocCertificate { get; set; }
        public string AnyDispute { get; set; }
        public string CourtCase { get; set; }
        public string CaseDetails { get; set; }

        // Construction / approvals
        public string TownPlanCheck { get; set; }
        public string TownPlanApproval { get; set; }
        public string ExpendValid { get; set; }
        public string StockListAttached { get; set; }
        public string PhotoAttached { get; set; }

        // Account head & budget
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public decimal ProposalCost { get; set; }
        public decimal BudgetAmount { get; set; }

        // Compliance
        public string AcHeadValid { get; set; }
        public string OtherDept { get; set; }
        public string WorkSplit { get; set; }
        public string MaintenancePeriod { get; set; }
        public string PrevMaintenance { get; set; }
        public string CompetentOfficer { get; set; }

        // Meta
        public string Remarks { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EntryDate { get; set; }
    }

    /// <summary>Summary DTO for list view.</summary>
    public class WorkProposalListDto
    {
        public long OrderNo { get; set; }
        public string ProposalType { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string NastiNo { get; set; }
        public string WorkName { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public decimal ProposalCost { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EntryDate { get; set; }
        public string WardNos { get; set; }
    }

    /// <summary>DTO returned when loading a proposal for audit/account remark entry.</summary>
    public class ProposalRemarkDto
    {
        public long OrderNo { get; set; }
        public string ProposalType { get; set; }   // 'Q' or 'T'
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string NastiNo { get; set; }
        public string WorkName { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal ProposalCost { get; set; }
        public string ExistingRemark { get; set; }
    }

    /// <summary>Result DTO returned when searching a proposal by Nasti No.</summary>
    public class NastiNoSearchResult
    {
        public long NastiNo { get; set; }
        /// <summary>'Q' = quotation, 'T' = tender, 'other' = other proposal</summary>
        public string ProposalType { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string WorkName { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public decimal ProposalCost { get; set; }
    }

    /// <summary>Request body for saving an audit or account remark on a work proposal.</summary>
    public class SaveRemarkRequest
    {
        public long OrderNo { get; set; }
        /// <summary>'Q' for quotation, 'T' for tender.</summary>
        public string ProposalType { get; set; }
        /// <summary>'audit' or 'account'.</summary>
        public string RemarkType { get; set; }
        public string AcSubhead { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal ProposalCost { get; set; }
        public string Remark { get; set; }
        public string UserId { get; set; }
        public int DeptCode { get; set; }
    }
}
