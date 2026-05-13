using System.Collections.Generic;
using SmkcApi.Models.BudgetBook;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public interface IGadWorkProposalRepository
    {
        /// <summary>Returns E-% account heads available for the given dept and fin year.</summary>
        IEnumerable<GadSubheadDto> GetAccountHeads(int ulbCode, int deptCode, string finYear);

        /// <summary>Returns budget total and actual expenditure for an account subhead.</summary>
        GadBudgetInfoDto GetBudgetInfo(int ulbCode, int deptCode, string acSubhead, string finYear, string proposalType = null);

        /// <summary>Saves a quotation (under ₹10 lakh) work proposal.</summary>
        WorkProposalSaveResult SaveUnder10LProposal(int ulbCode, Under10LProposalRequest request);

        /// <summary>Saves a tender (over ₹10 lakh) work proposal.</summary>
        WorkProposalSaveResult SaveOver10LProposal(int ulbCode, Over10LProposalRequest request);

        /// <summary>Returns departments for GAD module (reuses shared ULBERP.DEPARTMENTDET).</summary>
        IEnumerable<DepartmentDto> GetDepartments(int ulbCode, string userId);

        /// <summary>Returns work proposals list filtered by userId's department (PTTEST01/ADMIN001 see all).
        /// If requireAccountRemark=true, only returns proposals where accounts have added AUDIT_REMARK.</summary>
        IEnumerable<WorkProposalListDto> GetProposals(int ulbCode, string userId, string finYear, bool requireAccountRemark, string search, int pageSize, int pageNo, out int totalCount);

        /// <summary>Returns full detail of a single proposal for the print report.</summary>
        WorkProposalDetailDto GetProposalDetail(int ulbCode, string proposalType, long orderNo);

        /// <summary>Returns the next value from SEQGADQUOTATION (type='Q') or SEQGADTENDERS (type='T').</summary>
        long GetNextSequence(string proposalType);

        /// <summary>Returns proposal summary needed for audit/account remark entry form.</summary>
        ProposalRemarkDto GetProposalForRemark(int ulbCode, string proposalType, long orderNo, string remarkType);

        /// <summary>Saves an audit or account remark. RemarkType='audit' → AUDIT_REMARK; 'account' → ACCOUNT_REMARK.</summary>
        bool SaveProposalRemark(int ulbCode, SaveRemarkRequest request);

        /// <summary>Searches quotation/tender/other proposals by nasti number. Returns null if not found.</summary>
        NastiNoSearchResult GetProposalByNastiNo(int ulbCode, long nastiNo, string finYear = null);
    }
}
