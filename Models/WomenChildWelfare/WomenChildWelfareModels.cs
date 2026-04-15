using System;
using System.Collections.Generic;

namespace SmkcApi.Models.WomenChildWelfare
{
    public class WcwcOperatorLoginRequest
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
    }

    public class WcwcOperatorLoginResponse
    {
        public int OperatorUserId { get; set; }
        public string OperatorCode { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string MobileNo { get; set; }
        public string RoleCode { get; set; }
        public string DepartmentName { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class WcwcStatusUpdateRequest
    {
        public string Status { get; set; }
        public int? ChangedByOperatorId { get; set; }
        public string Remarks { get; set; }
    }

    public class WcwcStoredDocumentUploadRequest
    {
        public string RegistrationIdOrNumber { get; set; }
        public string DocumentCode { get; set; }
        public string FileName { get; set; }
        public string FileData { get; set; }
        public long? FileSize { get; set; }
        public string ContentType { get; set; }
        public int? UploadedByOperatorId { get; set; }
    }

    public class WcwcDocumentReference
    {
        public string DocumentCode { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class WcwcUploadedFile
    {
        public string FieldName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }

    public class WcwcRegistrationUpsertRequest
    {
        public int? RegistrationId { get; set; }
        public string RegistrationNo { get; set; }
        public string ApplicationMode { get; set; }
        public string SubmissionChannel { get; set; }
        public string Surname { get; set; }
        public string FirstName { get; set; }
        public string FatherName { get; set; }
        public string MotherName { get; set; }
        public string Education { get; set; }
        public string AadhaarNumber { get; set; }
        public string Dob { get; set; }
        public string MaritalStatus { get; set; }
        public string Religion { get; set; }
        public string Caste { get; set; }
        public string FamilyRelation { get; set; }
        public string FullAddress { get; set; }
        public string WardNumber { get; set; }
        public string PrabhagSamiti { get; set; }
        public string Uphc { get; set; }
        public string Pincode { get; set; }
        public string Constituency { get; set; }
        public string MobileNumber { get; set; }
        public string AlternatePhone { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string AccountNumber { get; set; }
        public string IfscCode { get; set; }
        public string BplNumber { get; set; }
        public string BplYear { get; set; }
        public string HasCertificate { get; set; }
        public string CertificateNumber { get; set; }
        public string CertificateDate { get; set; }
        public string CertificateType { get; set; }
        public string DisabilityPercentage { get; set; }
        public string HasUdid { get; set; }
        public string UdidNumber { get; set; }
        public string HasStPass { get; set; }
        public string StPassNumber { get; set; }
        public string HasRailwayPass { get; set; }
        public string RailwayPassNumber { get; set; }
        public string HasMsrtcPass { get; set; }
        public string MsrtcPassNumber { get; set; }
        public string IsEmployed { get; set; }
        public string EmploymentType { get; set; }
        public string Occupation { get; set; }
        public string HasGovtBenefit { get; set; }
        public string GovtBenefitScheme { get; set; }
        public string HasMcBenefit { get; set; }
        public string McBenefitDetails { get; set; }
        public string HasSgnPension { get; set; }
        public string HasOwnHouse { get; set; }
        public string WantsHousingBenefit { get; set; }
        public string HasOwnLand { get; set; }
        public string HasGuardianship { get; set; }
        public string GuardianName { get; set; }
        public string GuardianAddress { get; set; }
        public string GuardianPhone { get; set; }
        public string NeedsAssistiveDevice { get; set; }
        public string DocumentsSubmitted { get; set; }
        public bool TermsAccepted { get; set; }
        public string ApplicantSignDate { get; set; }
        public string SurveyorName { get; set; }
        public string SurveyorDesignation { get; set; }
        public string SurveyorMobile { get; set; }
        public string PublicSubmitterName { get; set; }
        public string PublicSubmitterMobile { get; set; }
        public string PublicCenterName { get; set; }
        public string ManualApplicationNo { get; set; }
        public int? OperatorUserId { get; set; }
        public string CreatedBy { get; set; }
        public List<string> DisabilityTypes { get; set; }
        public List<int> DisabilityTypeIds { get; set; }
        public List<string> AssistiveDevices { get; set; }
        public List<int> AssistiveDeviceIds { get; set; }
        public List<WcwcUploadedFile> Files { get; set; }

        public WcwcRegistrationUpsertRequest()
        {
            DisabilityTypes = new List<string>();
            DisabilityTypeIds = new List<int>();
            AssistiveDevices = new List<string>();
            AssistiveDeviceIds = new List<int>();
            Files = new List<WcwcUploadedFile>();
        }
    }

    public class WcwcRegistrationSubmitResponse
    {
        public int RegistrationId { get; set; }
        public string RegistrationNumber { get; set; }
        public string Status { get; set; }
        public string ApplicationMode { get; set; }
        public string SubmissionChannel { get; set; }
        public List<WcwcDocumentReference> Documents { get; set; }

        public WcwcRegistrationSubmitResponse()
        {
            Documents = new List<WcwcDocumentReference>();
        }
    }

    public class WcwcRegistrationListItem
    {
        public int RegistrationId { get; set; }
        public string RegistrationNo { get; set; }
        public string ApplicationMode { get; set; }
        public string SubmissionChannel { get; set; }
        public string Status { get; set; }
        public string Surname { get; set; }
        public string FirstName { get; set; }
        public string FatherName { get; set; }
        public string MobileNumber { get; set; }
        public string AadhaarNumber { get; set; }
        public DateTime? Dob { get; set; }
        public decimal? DisabilityPercentage { get; set; }
        public string HasCertificate { get; set; }
        public string HasUdid { get; set; }
        public string NeedsAssistiveDevice { get; set; }
        public int? OperatorUserId { get; set; }
        public string OperatorName { get; set; }
        public string DisabilityNames { get; set; }
        public string AssistiveDevices { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WcwcDocumentRecord
    {
        public int RegistrationId { get; set; }
        public string RegistrationNo { get; set; }
        public string DocumentCode { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public int? UploadedByOperatorId { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WcwcRegistrationDetailsResponse
    {
        public WcwcRegistrationListItem Registration { get; set; }
        public List<string> DisabilityTypes { get; set; }
        public List<int> DisabilityTypeIds { get; set; }
        public List<string> AssistiveDevices { get; set; }
        public List<int> AssistiveDeviceIds { get; set; }
        public List<WcwcDocumentRecord> Documents { get; set; }

        public WcwcRegistrationDetailsResponse()
        {
            DisabilityTypes = new List<string>();
            DisabilityTypeIds = new List<int>();
            AssistiveDevices = new List<string>();
            AssistiveDeviceIds = new List<int>();
            Documents = new List<WcwcDocumentRecord>();
        }
    }
}