using SmkcApi.Models;
using SmkcApi.Models.WomenChildWelfare;

namespace SmkcApi.Repositories.WomenChildWelfare
{
    public interface IWomenChildWelfareRepository
    {
        WcwcRegistrationSubmitResponse SaveRegistration(WcwcRegistrationUpsertRequest request);
        void ReplaceDisabilities(int registrationId, string disabilityIdsCsv);
        void ReplaceAssistiveDevices(int registrationId, string assistiveDeviceIdsCsv);
        void UpsertDocument(int registrationId, string documentCode, string fileName, string mimeType, long fileSize, int? uploadedByOperatorId);
        WcwcRegistrationDetailsResponse GetRegistration(string registrationIdOrNumber);
        PagedResult<WcwcRegistrationListItem> GetRegistrations(int page, int pageSize, string searchText, string searchField, string status, string applicationMode, int? operatorUserId);
        WcwcOperatorLoginResponse LoginOperator(string userName, string passwordHash);
        void UpdateStatus(string registrationIdOrNumber, string newStatus, int? changedByOperatorId, string remarks);
        WcwcDocumentRecord GetDocument(string registrationIdOrNumber, string documentCode, string fileName);
    }
}