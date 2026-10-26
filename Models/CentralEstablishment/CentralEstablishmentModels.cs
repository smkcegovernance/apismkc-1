using System;
using System.Collections.Generic;

namespace SmkcApi.Models.CentralEstablishment
{
    public class EmsDashboardSummary
    {
        public int TotalEmployees { get; set; }
        public int PermanentEmployees { get; set; }
        public int ContractualEmployees { get; set; }
        public int SubstituteEmployees { get; set; }
        public int OtherEmployees { get; set; }
        public int ActiveDepartments { get; set; }
        public int OperationalUnits { get; set; }
        public int OfficialDesignations { get; set; }
        public string SourceSystem { get; set; } = "EMPPMS2160926";
        public string LastUpdated { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public class EmsDepartmentStrengthRow
    {
        public string Department { get; set; }
        public int Sanctioned { get; set; }
        public int Filled { get; set; }
        public int Working { get; set; }
        public int Permanent { get; set; }
        public int Contractual { get; set; }
        public int Substitute { get; set; }
        public int Other { get; set; }
        public int Vacant => Math.Max(Sanctioned - Filled, 0);
    }

    public class EmsOfficerHierarchyRow
    {
        public int UnitNo { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public string Designation { get; set; }
        public string Status { get; set; }
        public string Officer { get; set; }
        public string Contact { get; set; }
    }

    public class EmsEmployeeDirectoryRow
    {
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string EmploymentType { get; set; }
        public string ParentDepartment { get; set; }
        public string CurrentDepartment { get; set; }
        public string Designation { get; set; }
        public string AdditionalCharge { get; set; }
        public string Status { get; set; }
    }

    public class EmsAssignmentHistoryItem
    {
        public string ActionType { get; set; }
        public string ReferenceNumber { get; set; }
        public string OldDepartment { get; set; }
        public string NewDepartment { get; set; }
        public string OldDesignation { get; set; }
        public string NewDesignation { get; set; }
        public string EffectiveDate { get; set; }
        public string Remarks { get; set; }
        public string ApprovedBy { get; set; }
    }

    public class EmsEmployeeDetail
    {
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string EmploymentType { get; set; }
        public string MainDepartment { get; set; }
        public string CurrentDepartment { get; set; }
        public string MainDesignation { get; set; }
        public string CurrentDesignation { get; set; }
        public string AdditionalCharge { get; set; }
        public string MobileNumber { get; set; }
        public string Status { get; set; }
        public string EmployeeCode { get; set; }
        public List<EmsAssignmentHistoryItem> History { get; set; } = new List<EmsAssignmentHistoryItem>();
        public List<string> ActionTypes { get; set; } = new List<string>();
    }

    public class EmsEmployeeActionRequest
    {
        public string ActionType { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string FromDepartment { get; set; }
        public string ToDepartment { get; set; }
        public string FromDesignation { get; set; }
        public string ToDesignation { get; set; }
        public string TransferType { get; set; }
        public string EffectiveDate { get; set; }
        public string RelievingDate { get; set; }
        public string JoiningDate { get; set; }
        public string OrderDate { get; set; }
        public string OrderNo { get; set; }
        public string Remarks { get; set; }
        public string UpdatedBy { get; set; }
        public string InchargeStartDate { get; set; }
        public string InchargeEndDate { get; set; }
    }

    public class EmsEmployeeActionResult
    {
        public bool Success { get; set; }
        public string ActionType { get; set; }
        public string EmployeeId { get; set; }
        public string ReferenceNumber { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ProcessedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public class EmsPostingTransactionRow
    {
        public string TxId { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string ActionType { get; set; }
        public string FromDepartment { get; set; }
        public string ToDepartment { get; set; }
        public string EffectiveDate { get; set; }
        public string OrderNo { get; set; }
        public string Status { get; set; }
        public string ReferenceNo { get; set; }
    }

    public class EmsOrderRegisterRow
    {
        public string OrderNo { get; set; }
        public string OrderDate { get; set; }
        public string Subject { get; set; }
        public string IssuingAuthority { get; set; }
        public int LinkedActions { get; set; }
        public string Source { get; set; } = "Official";
    }
}
