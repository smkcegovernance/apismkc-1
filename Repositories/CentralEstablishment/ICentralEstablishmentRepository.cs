using System.Collections.Generic;
using SmkcApi.Models.CentralEstablishment;

namespace SmkcApi.Repositories.CentralEstablishment
{
    public interface ICentralEstablishmentRepository
    {
        EmsDashboardSummary GetDashboardSummary();
        IEnumerable<EmsDepartmentStrengthRow> GetDepartmentStrength();
        IEnumerable<EmsOfficerHierarchyRow> GetOfficerHierarchy();
        IEnumerable<EmsEmployeeDirectoryRow> GetEmployeeDirectory(string searchTerm = null, int pageSize = 50);
        IEnumerable<EmsPostingTransactionRow> GetPostingTransactions(string searchTerm = null, int pageSize = 50);
        IEnumerable<EmsOrderRegisterRow> GetOrderRegister(string searchTerm = null, int pageSize = 50);
        EmsEmployeeDetail GetEmployeeDetail(string employeeId);
        EmsEmployeeActionResult ProcessDepartmentTransfer(EmsEmployeeActionRequest request);
        EmsEmployeeActionResult ProcessDesignationChange(EmsEmployeeActionRequest request);
        EmsEmployeeActionResult ProcessInChargeAssignment(EmsEmployeeActionRequest request);
    }
}
