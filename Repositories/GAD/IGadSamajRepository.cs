using System.Collections.Generic;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public interface IGadSamajRepository
    {
        /// <summary>
        /// Looks up an order by nasti number and returns details with account remark status flags.
        /// Returns null if no order with that nasti number exists.
        /// </summary>
        NastiOrderDto GetOrderByNastiNo(int ulbCode, int nastiNo);

        /// <summary>
        /// Returns all authority/sanction officials for the given ULB.
        /// Used to populate the drpAuthOfficials dropdown.
        /// </summary>
        IEnumerable<SanctionOfficialDto> GetSanctionAuthorities(int ulbCode);

        /// <summary>
        /// Returns signing officers filtered by department code.
        /// Used to populate the drpSigningOfficials dropdown.
        /// </summary>
        IEnumerable<SanctionOfficialDto> GetSigningOfficers(int ulbCode, int deptCode);

        /// <summary>
        /// Returns the next sequential Samaj number for the given dept + finYear.
        /// Logic: MAX(WORKORDERNO)+1 in GADWORKORDERSDET where inward_type='samaj'.
        /// </summary>
        SamajNextNumberDto GetNextSamajNumber(int ulbCode, int deptCode, string finYear);

        /// <summary>
        /// Inserts the Samaj record into GADWORKORDERSDET and gadcontractors.
        /// Returns the assigned work order number and display number.
        /// </summary>
        SamajSaveResult GenerateSamaj(int ulbCode, GenerateSamajRequest request);

        /// <summary>
        /// Fetches a fully-populated Samaj detail record for printing.
        /// </summary>
        SamajDetailDto GetSamajDetail(int ulbCode, long workOrderNo, int deptCode, string finYear);

        /// <summary>
        /// Returns a list of all samaj records for a given ULB and financial year,
        /// ordered by date descending. When userId is supplied (and is not an admin),
        /// only records for the user's department are returned.
        /// </summary>
        IEnumerable<SamajListItemDto> GetSamajList(int ulbCode, string finYear, string userId = null);
    }
}
