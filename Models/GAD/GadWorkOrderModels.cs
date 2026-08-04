using System;
using System.Collections.Generic;

namespace SmkcApi.Models.GAD
{
    // ── Nasti Validation Result (V1-V4 checks) ─────────────────────────────────
    public class WorkOrderNastiValidationResult
    {
        /// <summary>V1: Nasti exists in quotation/tender table.</summary>
        public bool NastiFound { get; set; }
        /// <summary>V2: Primary budget book entry is done (BUDGETENTBY IS NOT NULL).</summary>
        public bool PrimaryBookEntryDone { get; set; }
        /// <summary>V3: Final budget book entry is done (FINALBOOKENTRYNO > 0).</summary>
        public bool FinalBookEntryDone { get; set; }
        /// <summary>V4: Samaj has been generated for this nasti.</summary>
        public bool SamajGenerated { get; set; }
        /// <summary>V5: Work order has NOT yet been generated (true = safe to proceed).</summary>
        public bool WorkOrderNotExists { get; set; }

        // Budget book entry numbers (for display)
        public long PrimaryBookEntryNo { get; set; }
        public long FinalBookEntryNo { get; set; }

        // From proposal table
        public int NastiNo { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string ProposalName { get; set; }
        public decimal ProposalAmount { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }

        // From samaj record in GADWORKORDERSDET
        public long SamajNo { get; set; }
        public string SamajDisplayNo { get; set; }
        public string SamajDate { get; set; }
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }

        // Existing work order info (populated if V4 fails)
        public long ExistingWorkOrderNo { get; set; }
        public string ExistingWorkOrderDisplayNo { get; set; }

        public string ValidationMessage { get; set; }
    }

    // ── Generate Work Order Request ─────────────────────────────────────────────
    public class GenerateWorkOrderRequest
    {
        /// <summary>Nasti number stored in FINALORDERNO column.</summary>
        public string NastiNo { get; set; }
        public int DeptCode { get; set; }
        public string FinYear { get; set; }

        // Pre-filled from samaj validation result
        public string ProposalName { get; set; }
        public decimal ProposalAmount { get; set; }
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }

        // Work period
        public string WorkPeriod { get; set; }
        /// <summary>दिवस or महिने</summary>
        public string PeriodType { get; set; }

        // Authority (always manual in the new system)
        public int AuthorityOfficials { get; set; }
        public string ManualFinalOrderNo { get; set; }
        public string SanctionDate { get; set; }
        public string SanctionFile { get; set; }

        // Signing officer
        public int SigningAuth { get; set; }

        // Supervisor
        public string SupervisorName { get; set; }

        // Stamp amount
        public decimal StampAmt { get; set; }

        // Contractors list
        public List<string> Contractors { get; set; }

        // Session
        public string EnteredBy { get; set; }
    }

    // ── Work Order Save Result ──────────────────────────────────────────────────
    public class WorkOrderSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long WorkOrderNo { get; set; }
        public string WorkOrderDisplayNo { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
    }

    // ── Next Work Order Number ──────────────────────────────────────────────────
    public class WorkOrderNextNumberDto
    {
        public long NextNo { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DisplayNo { get; set; }
    }

    // ── Work Order List Item ────────────────────────────────────────────────────
    public class WorkOrderListItemDto
    {
        public long WorkOrderNo { get; set; }
        public string WorkOrderDisplayNo { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string FinYear { get; set; }
        public string WorkOrderDate { get; set; }
        public string ProposalName { get; set; }
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }
        public string NastiNo { get; set; }
    }

    // ── Work Order Detail DTO (for print) ───────────────────────────────────────
    public class WorkOrderDetailDto
    {
        public long WorkOrderNo { get; set; }
        public string WorkOrderDisplayNo { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string FinYear { get; set; }
        public string NastiNo { get; set; }
        public string ProposalName { get; set; }
        public decimal ProposalAmount { get; set; }
        public string EligibleVendorName { get; set; }
        public decimal VendorProposedAmount { get; set; }
        public string WorkPeriod { get; set; }
        public string PeriodType { get; set; }
        public string AuthorityName { get; set; }
        public string ManualFinalOrderNo { get; set; }
        public string SanctionDate { get; set; }
        public string SigningOfficerName { get; set; }
        public string SigningOfficerDesignation { get; set; }
        public string SupervisorName { get; set; }
        public decimal StampAmt { get; set; }
        public DateTime WorkOrderDate { get; set; }
        public List<string> Contractors { get; set; }
    }
}
