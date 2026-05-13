using System.Collections.Generic;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public interface IGadBudgetCapRepository
    {
        /// <summary>All distinct budget codes (E-*) that have allocations for a fin year.</summary>
        IEnumerable<GadSubheadDto> GetBudgetCodes(string finYear);

        /// <summary>List all caps for a given financial year and ULB.</summary>
        IEnumerable<BudgetCapDto> ListCaps(int ulbCode, string finYear);

        /// <summary>Get cap for one specific subhead/year. Returns null if not set.</summary>
        BudgetCapDto GetCap(int ulbCode, string acSubhead, string finYear);

        /// <summary>Insert or update a budget cap. Records history automatically.</summary>
        void UpsertCap(BudgetCapSaveRequest req);

        /// <summary>Soft-delete a cap entry. Records history.</summary>
        void DeleteCap(int ulbCode, string acSubhead, string finYear, string actionBy);

        /// <summary>Return full history for a subhead/year.</summary>
        IEnumerable<BudgetCapHistoryDto> GetHistory(string acSubhead, string finYear);
    }
}
