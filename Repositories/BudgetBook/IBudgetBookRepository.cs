using System;
using System.Collections.Generic;
using SmkcApi.Models.BudgetBook;

namespace SmkcApi.Repositories.BudgetBook
{
    public interface IBudgetBookRepository
    {
        IEnumerable<DepartmentDto> GetDepartments(int ulbCode);
        IEnumerable<SubheadDto> GetSubheads(int ulbCode, string finYear);
        BudgetRemainingDto GetRemainingBudget(int ulbCode, string acSubhead, string finYear, long? excludeNastiNo = null);
        BudgetBookSaveResult SavePrimaryEntry(int ulbCode, PrimaryBudgetEntryRequest request);
        BudgetBookEntryDto GetPrimaryEntry(int ulbCode, long bookEntryNo);
        BudgetBookSaveResult SaveFinalEntry(int ulbCode, FinalBudgetEntryRequest request);
        BudgetBookSaveResult ReviseFinalEntry(int ulbCode, FinalBudgetRevisionRequest request);
        IEnumerable<BudgetBookEntryDto> ListEntries(int ulbCode, string finYear, int? deptCode, int pageNo, int pageSize, out int totalCount, string search = null, string status = null);
        BudgetLiabilityReportResult GetLiabilityReport(int ulbCode, DateTime fromDate, DateTime toDate);
        FundWiseBudgetLiabilityReportResult GetFundWiseLiabilityReport(int ulbCode, DateTime fromDate, DateTime toDate);
        BillPaymentReportResult GetBillPaymentReport(int ulbCode, DateTime fromDate, DateTime toDate);
        WardWiseExpenditureReportResult GetWardWiseExpenditureReport(
            int ulbCode,
            DateTime fromDate,
            DateTime toDate,
            string finYear = null,
            string wardNo = null,
            string wardScope = null,
            string deptName = null);
    }
}
