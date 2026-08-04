using System.Collections.Generic;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public interface IGadWorkOrderRepository
    {
        WorkOrderNastiValidationResult ValidateNastiForWorkOrder(int ulbCode, int nastiNo, string finYear);
        IEnumerable<SanctionOfficialDto> GetSanctionAuthorities(int ulbCode);
        IEnumerable<SanctionOfficialDto> GetSigningOfficers(int ulbCode, int deptCode);
        WorkOrderNextNumberDto GetNextWorkOrderNumber(int ulbCode, int deptCode, string finYear);
        WorkOrderSaveResult GenerateWorkOrder(int ulbCode, GenerateWorkOrderRequest req);
        WorkOrderDetailDto GetWorkOrderDetail(int ulbCode, long workOrderNo, int deptCode, string finYear);
        IEnumerable<WorkOrderListItemDto> GetWorkOrderList(int ulbCode, string finYear, string userId);
    }
}
