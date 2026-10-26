using System.Collections.Generic;
using SmkcApi.Models.BMT;

namespace SmkcApi.Repositories.BMT
{
    public interface IBillMappingTrackingRepository
    {
        IEnumerable<BmtDepartmentDto> GetDepartments(int ulbCode);
        IEnumerable<BmtBudgetHeadDto> GetBudgetHeads(int ulbCode, string finYear);
        IEnumerable<LegacyBillDto> GetEligibleLegacyBills(int ulbCode, string finYear, string acSubhead);
        BmtNastiValidationDto ValidateNastiForMapping(int ulbCode, long nastiNo, string finYear);
        BmtOperationResult MapLegacyBillToNasti(int ulbCode, MapLegacyBillRequest request);
        BmtOperationResult MarkLegacyBillAsDirect(int ulbCode, MarkDirectBillRequest request);
        IEnumerable<BmtMappingRowDto> ListMappings(int ulbCode, string finYear, string acSubhead, string mapType);
        BmtOperationResult CreateDirectBillEntry(int ulbCode, DirectBillEntryRequest request);
        IEnumerable<DirectBillEntryDto> ListDirectBillEntries(int ulbCode, string finYear, string acSubhead);
    }
}
