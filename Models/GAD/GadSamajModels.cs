using System;
using System.Collections.Generic;

namespace SmkcApi.Models.GAD
{
    // ── Dropdown DTOs ─────────────────────────────────────────────────────────────

    public class SanctionOfficialDto
    {
        public int SanctionId { get; set; }
        public string SanctionNameLL { get; set; }
    }

    // ── Nasti Order (search result by nasti number) ────────────────────────────

    public class NastiOrderDto
    {
        public int NastiNo { get; set; }
        public int OrderId { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string ProposalName { get; set; }
        public decimal ProposalAmount { get; set; }
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        /// <summary>True when primary (provisional) budget book entry exists.</summary>
        public bool PrimaryBookEntryDone { get; set; }
        /// <summary>True when final budget book entry exists.</summary>
        public bool FinalBookEntryDone { get; set; }
        public long PrimaryBookEntryNo { get; set; }
        public long FinalBookEntryNo { get; set; }
    }

    // ── Samaj Generate Request ────────────────────────────────────────────────────

    public class GenerateSamajRequest
    {
        // ── Core refs ───────────────────────────────────────────────────────────
        /// <summary>Nasti number from GADORDERSDET.NASTINO (stored in FINALORDERNO for audit).</summary>
        public string NastiNo { get; set; }
        public int DeptCode { get; set; }
        public string FinYear { get; set; }

        // ── Work details (copied from GADORDERSDET) ──────────────────────────────
        public string ProposalName { get; set; }
        public decimal ProposalAmount { get; set; }

        // ── Vendor ───────────────────────────────────────────────────────────────
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }

        // ── Work period ──────────────────────────────────────────────────────────
        public string WorkPeriod { get; set; }
        /// <summary>दिवस or महिने</summary>
        public string PeriodType { get; set; }

        // ── Authority ────────────────────────────────────────────────────────────
        /// <summary>SANCTIONID from sanction officials table.</summary>
        public int AuthorityOfficials { get; set; }
        /// <summary>true when SANCTIONID is 9 or 10 (manual resolution-based approval)</summary>
        public bool IsManualOrder { get; set; }
        public string ManualFinalOrderNo { get; set; }
        public string SanctionDate { get; set; }
        /// <summary>Uploaded resolution file name</summary>
        public string SanctionFile { get; set; }

        // ── Signing officer ──────────────────────────────────────────────────────
        public int SigningAuth { get; set; }

        // ── Supervisor ───────────────────────────────────────────────────────────
        public string SupervisorName { get; set; }

        // ── Contractor details ───────────────────────────────────────────────────
        public decimal StampAmt { get; set; }
        public string ContractorClass { get; set; }
        /// <summary>y or n</summary>
        public string EmdFlag { get; set; }
        /// <summary>y or n</summary>
        public string DepositFlag { get; set; }
        public List<string> Contractors { get; set; }

        // ── Session / meta ───────────────────────────────────────────────────────
        public string EnteredBy { get; set; }
    }

    // ── Samaj Save Result ─────────────────────────────────────────────────────────

    public class SamajSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        /// <summary>The sequential work order number assigned (dept-wise).</summary>
        public long WorkOrderNo { get; set; }
        /// <summary>Display format: deptCode/workOrderNo/finYear</summary>
        public string SamajDisplayNo { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
    }

    // ── Next Samaj Number ─────────────────────────────────────────────────────────

    public class SamajNextNumberDto
    {
        public long NextNo { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DisplayNo { get; set; }
    }

    // ── Samaj List Item ───────────────────────────────────────────────────────────

    public class SamajListItemDto
    {
        public long WorkOrderNo { get; set; }
        public string SamajDisplayNo { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string FinYear { get; set; }
        public string WorkOrderDate { get; set; }
        public string ProposalName { get; set; }
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }
    }

    // ── Samaj Detail (for print) ──────────────────────────────────────────────────

    public class SamajDetailDto
    {
        public long WorkOrderNo { get; set; }
        public string SamajDisplayNo { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string FinYear { get; set; }
        public string FinalOrderNo { get; set; }
        public string ProposalName { get; set; }
        public decimal ProposalAmount { get; set; }
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }
        public string WorkPeriod { get; set; }
        public string PeriodType { get; set; }
        public string AuthorityName { get; set; }
        public bool IsManualOrder { get; set; }
        public string ManualFinalOrderNo { get; set; }
        public string SanctionDate { get; set; }
        public string SigningOfficerName { get; set; }
        public string SigningOfficerDesignation { get; set; }
        public string SupervisorName { get; set; }
        public decimal StampAmt { get; set; }
        public string ContractorClass { get; set; }
        public string EmdFlag { get; set; }
        public string DepositFlag { get; set; }
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
        public DateTime WorkOrderDate { get; set; }
        public List<string> Contractors { get; set; }
    }
}
