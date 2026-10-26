using System;
using System.Collections.Generic;

namespace SmkcApi.Models.BMT
{
    public class BmtBudgetHeadDto
    {
        public string AcSubhead { get; set; }
        public string AcSubheadName { get; set; }
    }

    public class BmtDepartmentDto
    {
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
    }

    public class LegacyBillDto
    {
        public long RegBillNo { get; set; }
        public string DeptBillNo { get; set; }
        public string AccountBillNo { get; set; }
        public string PartyName { get; set; }
        public decimal BillAmount { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public bool IsMapped { get; set; }
        public string MapType { get; set; }
    }

    public class BmtNastiValidationDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long NastiNo { get; set; }
        public long BookEntryNo { get; set; }
        public long FinalBookEntryNo { get; set; }
        public string WorkName { get; set; }
        public int DeptCode { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public decimal FinalCommitmentAmount { get; set; }
        public decimal AlreadyMappedAmount { get; set; }
        public decimal AvailableAmount { get; set; }
    }

    public class MapLegacyBillRequest
    {
        public long RegBillNo { get; set; }
        public string DeptBillNo { get; set; }
        public string AccountBillNo { get; set; }
        public string PartyName { get; set; }
        public decimal BillAmount { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public long NastiNo { get; set; }
        public string MappedBy { get; set; }
    }

    public class MarkDirectBillRequest
    {
        public long RegBillNo { get; set; }
        public string DeptBillNo { get; set; }
        public string AccountBillNo { get; set; }
        public string PartyName { get; set; }
        public decimal BillAmount { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public string Reason { get; set; }
        public string MappedBy { get; set; }
    }

    public class BmtOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class BmtMappingRowDto
    {
        public long MappingId { get; set; }
        public string MapType { get; set; }
        public long RegBillNo { get; set; }
        public string DeptBillNo { get; set; }
        public string AccountBillNo { get; set; }
        public string PartyName { get; set; }
        public decimal MappedAmount { get; set; }
        public string AcSubhead { get; set; }
        public string FinYear { get; set; }
        public long? NastiNo { get; set; }
        public long? FinalBookEntryNo { get; set; }
        public decimal? FinalCommitmentAmount { get; set; }
        public string MapReason { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
    }

    public class DirectBillEntryRequest
    {
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string AcSubhead { get; set; }
        public string BillType { get; set; }
        public string AccountBillNo { get; set; }
        public DateTime BillDate { get; set; }
        public string PartyName { get; set; }
        public string BillDescription { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string Remarks { get; set; }
        public string EnteredBy { get; set; }
    }

    public class DirectBillEntryDto
    {
        public long DirectBillId { get; set; }
        public string FinYear { get; set; }
        public int DeptCode { get; set; }
        public string DeptName { get; set; }
        public string AcSubhead { get; set; }
        public string BillType { get; set; }
        public string AccountBillNo { get; set; }
        public DateTime BillDate { get; set; }
        public string PartyName { get; set; }
        public string BillDescription { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string Remarks { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
    }

    public class BmtListResult<T>
    {
        public List<T> Rows { get; set; }
        public int TotalCount { get; set; }
    }
}
