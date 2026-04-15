using System;
using System.Collections.Generic;
using SmkcApi.Models.DepositManager;

namespace SmkcApi.Models
{
    public class WcwcApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RegistrationNumber { get; set; }
        public object Data { get; set; }
        public string ErrorCode { get; set; }
        public DateTime Timestamp { get; set; }

        public WcwcApiResponse()
        {
            Timestamp = DateTime.UtcNow;
        }

        public static WcwcApiResponse CreateSuccess(object data, string message, string registrationNumber)
        {
            return new WcwcApiResponse
            {
                Success = true,
                Message = message,
                Data = data,
                RegistrationNumber = registrationNumber
            };
        }

        public static WcwcApiResponse CreateError(string message, string errorCode)
        {
            return new WcwcApiResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    public class WcwcOperationResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string ErrorCode { get; set; }
    }

    public class WcwcRegistrationUpsertRequest
    {
        public string RegistrationIdentifier { get; set; }
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
        public List<string> DisabilitySelections { get; set; }
        public List<string> AssistiveDeviceSelections { get; set; }
        public List<WcwcUploadedFile> Files { get; set; }

        public WcwcRegistrationUpsertRequest()
        {
            DisabilitySelections = new List<string>();
            AssistiveDeviceSelections = new List<string>();
            Files = new List<WcwcUploadedFile>();
        }
    }

    public class WcwcUploadedFile
    {
        public string FieldName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Bytes { get; set; }
        public long FileSize { get; set; }
    }

    public class WcwcRegistrationSaveData
    {
        public int RegistrationId { get; set; }
        public string RegistrationNumber { get; set; }
    }

    public class WcwcOperatorLoginRequest
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
    }

    public class WcwcStatusUpdateRequest
    {
        public string Status { get; set; }
        public int? OperatorUserId { get; set; }
        public string Remarks { get; set; }
    }

    public class WcwcDocumentUploadRequest
    {
        public string RegistrationNumber { get; set; }
        public string DocumentCode { get; set; }
        public string FileName { get; set; }
        public string FileData { get; set; }
        public long? FileSize { get; set; }
        public string ContentType { get; set; }
        public int? UploadedByOperatorId { get; set; }

        public WcwcOperationResult<object> Validate()
        {
            if (string.IsNullOrWhiteSpace(RegistrationNumber))
            {
                return new WcwcOperationResult<object> { Success = false, Message = "registrationNumber is required", ErrorCode = "REGISTRATION_REQUIRED" };
            }

            if (string.IsNullOrWhiteSpace(DocumentCode))
            {
                return new WcwcOperationResult<object> { Success = false, Message = "documentCode is required", ErrorCode = "DOCUMENT_CODE_REQUIRED" };
            }

            if (string.IsNullOrWhiteSpace(FileName) || string.IsNullOrWhiteSpace(FileData))
            {
                return new WcwcOperationResult<object> { Success = false, Message = "fileName and fileData are required", ErrorCode = "FILE_REQUIRED" };
            }

            try
            {
                var base64Data = FileData;
                if (base64Data.Contains(","))
                {
                    var commaIndex = base64Data.IndexOf(',');
                    if (base64Data.Substring(0, commaIndex).Contains("base64"))
                    {
                        base64Data = base64Data.Substring(commaIndex + 1);
                    }
                }

                var bytes = Convert.FromBase64String(base64Data);
                if (bytes.Length == 0)
                {
                    return new WcwcOperationResult<object> { Success = false, Message = "fileData is empty", ErrorCode = "EMPTY_FILE" };
                }
            }
            catch (FormatException)
            {
                return new WcwcOperationResult<object> { Success = false, Message = "fileData must be valid base64", ErrorCode = "INVALID_BASE64" };
            }

            return new WcwcOperationResult<object> { Success = true, Message = "Validation passed" };
        }
    }

    public class WcwcDocumentStorageRequest
    {
        public string RegistrationNumber { get; set; }
        public string DocumentCode { get; set; }
        public ConsentDocumentDto Document { get; set; }
    }
}