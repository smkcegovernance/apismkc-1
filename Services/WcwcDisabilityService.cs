using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Infrastructure;
using SmkcApi.Models;
using SmkcApi.Models.DepositManager;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface IWcwcDisabilityService
    {
        WcwcApiResponse Register(WcwcRegistrationUpsertRequest request);
        WcwcApiResponse Update(string identifier, WcwcRegistrationUpsertRequest request);
        WcwcApiResponse GetRegistration(string identifier);
        WcwcApiResponse GetApplicationStatus(string registrationNumber);
        WcwcApiResponse Search(string searchText, string status, string applicationMode, int? operatorUserId);
        WcwcApiResponse UpdateStatus(string identifier, WcwcStatusUpdateRequest request);
        Task<WcwcApiResponse> SendStatusSmsAsync(string identifier, WcwcStatusSmsRequest request);
        WcwcApiResponse Cancel(string identifier, WcwcStatusUpdateRequest request);
        WcwcApiResponse DeleteRegistration(string identifier, WcwcStatusUpdateRequest request);
        WcwcApiResponse OperatorLogin(WcwcOperatorLoginRequest request);
        WcwcApiResponse UploadDocument(WcwcDocumentUploadRequest request);
        WcwcApiResponse DownloadDocument(string registrationNumber, string documentCode, string fileName);
        WcwcApiResponse GetReport(string reportType, DateTime? fromDate, DateTime? toDate, string applicationMode);
    }

    public class WcwcDisabilityService : IWcwcDisabilityService
    {
        private readonly IWcwcDisabilityRepository _repository;
        private readonly IWcwcDocumentStorageService _documentStorageService;
        private readonly ISmsSender _smsSender;

        private static readonly IDictionary<string, int> DisabilityTypeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "blindness", 3 },
            { "low vision", 2 },
            { "hearing impairment", 4 },
            { "speech and language disability", 5 },
            { "locomotor disability", 1 },
            { "mental illness", 7 },
            { "specific learning disabilities", 13 },
            { "specific learning disability", 13 },
            { "cerebral palsy", 9 },
            { "autism spectrum disorder", 8 },
            { "multiple disabilities", 20 },
            { "leprosy cured persons", 21 },
            { "dwarfism", 10 },
            { "intellectual disability", 6 },
            { "muscular dystrophy", 11 },
            { "chronic neurological conditions", 12 },
            { "multiple sclerosis", 14 },
            { "thalassemia", 15 },
            { "hemophilia", 16 },
            { "sickle cell disease", 17 },
            { "acid attack victim", 18 },
            { "parkinson's disease", 19 },
            { "parkinsons disease", 19 }
        };

        private static readonly IDictionary<string, int> AssistiveDeviceMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "तीनचाकी सायकल", 1 },
            { "व्हील चेअर", 2 },
            { "कुबड्या", 5 },
            { "श्रवणयंत्र", 4 },
            { "वॉकर", 6 },
            { "जयपूरफुट", 8 },
            { "शस्त्रक्रिया", 8 },
            { "उत्पादक वस्तू", 8 },
            { "इतर", 8 }
        };

        private static readonly IDictionary<string, string> FieldToDocumentCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "udidDoc", "UDID_DOC" },
            { "udiddocument", "UDID_DOC" },
            { "aadhaarDoc", "AADHAAR_DOC" },
            { "aadhaardocument", "AADHAAR_DOC" },
            { "rationCardDoc", "RATION_CARD_DOC" },
            { "rationcarddocument", "RATION_CARD_DOC" },
            { "rationcardfile", "RATION_CARD_DOC" },
            { "bankDoc", "BANK_DOC" },
            { "bankdocument", "BANK_DOC" },
            { "incomeCertificateDoc", "INCOME_CERTIFICATE_DOC" },
            { "incomecertificatedocument", "INCOME_CERTIFICATE_DOC" },
            { "photoDoc", "PHOTO_DOC" },
            { "photodocument", "PHOTO_DOC" },
            { "applicantSignature", "APPLICANT_SIGNATURE" },
            { "applicantsign", "APPLICANT_SIGNATURE" },
            { "surveyorSignature", "SURVEYOR_SIGNATURE" }
        };

        private static readonly string[] RequiredDocumentCodes = new[]
        {
            "UDID_DOC",
            "AADHAAR_DOC",
            "RATION_CARD_DOC",
            "BANK_DOC",
            "PHOTO_DOC",
            "APPLICANT_SIGNATURE"
        };

        public WcwcDisabilityService(IWcwcDisabilityRepository repository, IWcwcDocumentStorageService documentStorageService, ISmsSender smsSender)
        {
            _repository = repository;
            _documentStorageService = documentStorageService;
            _smsSender = smsSender;
        }

        public WcwcApiResponse Register(WcwcRegistrationUpsertRequest request)
        {
            return SaveInternal(null, request);
        }

        public WcwcApiResponse Update(string identifier, WcwcRegistrationUpsertRequest request)
        {
            var resolved = ResolveIdentifier(identifier);
            if (!resolved.Success)
            {
                return WcwcApiResponse.CreateError(resolved.Message, resolved.ErrorCode);
            }

            var row = GetFirstRow(resolved.Data);
            if (row == null)
            {
                return WcwcApiResponse.CreateError("Registration not found", "REGISTRATION_NOT_FOUND");
            }

            return SaveInternal(Convert.ToInt32(row["REGISTRATION_ID"]), request);
        }

        public WcwcApiResponse GetRegistration(string identifier)
        {
            var resolved = ResolveIdentifier(identifier);
            if (!resolved.Success)
            {
                return WcwcApiResponse.CreateError(resolved.Message, resolved.ErrorCode);
            }

            return WcwcApiResponse.CreateSuccess(resolved.Data, "Registration retrieved successfully", ExtractRegistrationNumber(resolved.Data));
        }

        public WcwcApiResponse GetApplicationStatus(string registrationNumber)
        {
            if (string.IsNullOrWhiteSpace(registrationNumber))
                return WcwcApiResponse.CreateError("Registration number is required", "IDENTIFIER_REQUIRED");

            var result = _repository.GetApplicationStatus(registrationNumber.Trim());
            if (!result.Success)
                return WcwcApiResponse.CreateError(result.Message, result.ErrorCode ?? "STATUS_LOOKUP_FAILED");

            return WcwcApiResponse.CreateSuccess(result.Data, result.Message, registrationNumber);
        }

        public WcwcApiResponse GetReport(string reportType, DateTime? fromDate, DateTime? toDate, string applicationMode)
        {
            var result = _repository.GetReport(reportType ?? "ALL", fromDate, toDate, applicationMode);
            if (!result.Success)
                return WcwcApiResponse.CreateError(result.Message, result.ErrorCode ?? "REPORT_FAILED");

            return WcwcApiResponse.CreateSuccess(result.Data, result.Message, null);
        }

        public WcwcApiResponse Search(string searchText, string status, string applicationMode, int? operatorUserId)
        {
            var result = _repository.SearchRegistrations(searchText, status, applicationMode, operatorUserId);
            if (!result.Success)
            {
                return WcwcApiResponse.CreateError(result.Message, result.ErrorCode);
            }

            return WcwcApiResponse.CreateSuccess(result.Data, result.Message, null);
        }

        public WcwcApiResponse UpdateStatus(string identifier, WcwcStatusUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return WcwcApiResponse.CreateError("नवीन स्थिती आवश्यक आहे", "MISSING_STATUS");

            var newStatus = request.Status.Trim().ToUpperInvariant();
            var allowedStatuses = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal)
                { "UNDER_REVIEW", "APPROVED", "REJECTED", "DUPLICATE" };
            if (!allowedStatuses.Contains(newStatus))
                return WcwcApiResponse.CreateError("स्थिती UNDER_REVIEW, APPROVED, REJECTED किंवा DUPLICATE असणे आवश्यक आहे", "INVALID_STATUS");

            if ((newStatus == "REJECTED" || newStatus == "DUPLICATE") && string.IsNullOrWhiteSpace(request.Remarks))
                return WcwcApiResponse.CreateError("नाकारताना / डुप्लिकेट करताना कारण/शेरा आवश्यक आहे", "REMARKS_REQUIRED");

            var resolved = ResolveIdentifier(identifier);
            if (!resolved.Success) return WcwcApiResponse.CreateError(resolved.Message, resolved.ErrorCode);
            var row = GetFirstRow(resolved.Data);
            if (row == null) return WcwcApiResponse.CreateError("नोंदणी आढळली नाही", "REGISTRATION_NOT_FOUND");

            var result = _repository.UpdateStatus(Convert.ToInt32(row["REGISTRATION_ID"]), newStatus, request.OperatorUserId, request.Remarks);
            if (!result.Success) return WcwcApiResponse.CreateError(result.Message, result.ErrorCode);
            return WcwcApiResponse.CreateSuccess(null, result.Message, Convert.ToString(row["REGISTRATION_NO"]));
        }

        public async Task<WcwcApiResponse> SendStatusSmsAsync(string identifier, WcwcStatusSmsRequest request)
        {
            var resolved = ResolveIdentifier(identifier);
            if (!resolved.Success)
            {
                return WcwcApiResponse.CreateError(resolved.Message, resolved.ErrorCode);
            }

            var row = GetFirstRow(resolved.Data);
            if (row == null)
            {
                return WcwcApiResponse.CreateError("नोंदणी आढळली नाही", "REGISTRATION_NOT_FOUND");
            }

            var mobile = GetValue(row, "MOBILE_NUMBER");
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return WcwcApiResponse.CreateError("मोबाइल क्रमांक उपलब्ध नाही", "MOBILE_NOT_FOUND");
            }

            var registrationNumber = GetValue(row, "REGISTRATION_NO");
            if (string.IsNullOrWhiteSpace(registrationNumber))
            {
                return WcwcApiResponse.CreateError("नोंदणी क्रमांक उपलब्ध नाही", "REGISTRATION_NOT_FOUND");
            }

            var currentStatus = !string.IsNullOrWhiteSpace(request != null ? request.Status : null)
                ? request.Status.Trim().ToUpperInvariant()
                : GetValue(row, "STATUS");

            if (string.IsNullOrWhiteSpace(currentStatus))
            {
                return WcwcApiResponse.CreateError("स्थिती उपलब्ध नाही", "STATUS_NOT_FOUND");
            }

            var statusLabel = ToMarathiStatusLabel(currentStatus);
            var baseUrl = ConfigurationManager.AppSettings["Wcwc_PublicStatusPageBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:3000/public/disability-registration/status";
            }

            var statusUrl = baseUrl.TrimEnd('/') + "?regNo=" + Uri.EscapeDataString(registrationNumber);
            var message = BuildStatusSmsMessage(registrationNumber, currentStatus, statusLabel, statusUrl);

            var providerResponse = await _smsSender.SendSmsRawAsync(
                new[] { mobile },
                message,
                unicode: true,
                dlttemplateid: ConfigurationManager.AppSettings["Sms_DefaultDltTemplateId"])
                .ConfigureAwait(false);

            return WcwcApiResponse.CreateSuccess(new
            {
                registrationNumber,
                mobileNumber = MaskMobile(mobile),
                status = currentStatus,
                message,
                providerResponse
            }, "स्थिती SMS पाठवला गेला", registrationNumber);
        }

        public WcwcApiResponse Cancel(string identifier, WcwcStatusUpdateRequest request)
        {
            var resolved = ResolveIdentifier(identifier);
            if (!resolved.Success)
            {
                return WcwcApiResponse.CreateError(resolved.Message, resolved.ErrorCode);
            }

            var row = GetFirstRow(resolved.Data);
            if (row == null)
            {
                return WcwcApiResponse.CreateError("Registration not found", "REGISTRATION_NOT_FOUND");
            }

            var result = _repository.UpdateStatus(Convert.ToInt32(row["REGISTRATION_ID"]), "CANCELLED", request != null ? request.OperatorUserId : null, request != null ? request.Remarks : null);
            if (!result.Success)
            {
                return WcwcApiResponse.CreateError(result.Message, result.ErrorCode);
            }

            return WcwcApiResponse.CreateSuccess(null, result.Message, Convert.ToString(row["REGISTRATION_NO"]));
        }

        public WcwcApiResponse DeleteRegistration(string identifier, WcwcStatusUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return WcwcApiResponse.CreateError("Registration identifier is required", "IDENTIFIER_REQUIRED");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Remarks))
            {
                return WcwcApiResponse.CreateError("Department delete reason is required", "DELETE_REASON_REQUIRED");
            }

            var resolved = ResolveIdentifier(identifier);
            if (!resolved.Success)
            {
                return WcwcApiResponse.CreateError(resolved.Message, resolved.ErrorCode);
            }

            var row = GetFirstRow(resolved.Data);
            if (row == null)
            {
                return WcwcApiResponse.CreateError("Registration not found", "REGISTRATION_NOT_FOUND");
            }

            var registrationId = Convert.ToInt32(row["REGISTRATION_ID"]);
            var registrationNo = Convert.ToString(row["REGISTRATION_NO"]);
            var result = _repository.DeleteRegistration(registrationId, request.OperatorUserId, request.Remarks);
            if (!result.Success)
            {
                return WcwcApiResponse.CreateError(result.Message, result.ErrorCode);
            }

            return WcwcApiResponse.CreateSuccess(null, "Registration deleted successfully. Applicant may resubmit the form.", registrationNo);
        }

        public WcwcApiResponse OperatorLogin(WcwcOperatorLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.PasswordHash))
            {
                return WcwcApiResponse.CreateError("userName and passwordHash are required", "INVALID_LOGIN_REQUEST");
            }

            var result = _repository.OperatorLogin(request.UserName, request.PasswordHash);
            if (!result.Success)
            {
                return WcwcApiResponse.CreateError(result.Message, result.ErrorCode);
            }

            return WcwcApiResponse.CreateSuccess(result.Data, result.Message, null);
        }

        public WcwcApiResponse UploadDocument(WcwcDocumentUploadRequest request)
        {
            if (request == null)
            {
                return WcwcApiResponse.CreateError("Request body is required", "MISSING_REQUEST_BODY");
            }

            var validation = request.Validate();
            if (!validation.Success)
            {
                return WcwcApiResponse.CreateError(validation.Message, validation.ErrorCode);
            }

            var registrationResult = _repository.GetRegistration(null, request.RegistrationNumber);
            if (!registrationResult.Success)
            {
                return WcwcApiResponse.CreateError(registrationResult.Message, registrationResult.ErrorCode);
            }

            var row = GetFirstRow(registrationResult.Data);
            if (row == null)
            {
                return WcwcApiResponse.CreateError("Registration not found", "REGISTRATION_NOT_FOUND");
            }

            var fileBytes = DecodeBase64(request.FileData);
            var storedFileName = _documentStorageService.UploadDocument(
                request.RegistrationNumber,
                request.DocumentCode,
                new ConsentDocumentDto
                {
                    FileName = request.FileName,
                    FileData = request.FileData,
                    FileSize = fileBytes.LongLength,
                    ContentType = request.ContentType,
                    UploadedAt = DateTime.UtcNow
                });

            var upsertResult = _repository.UpsertDocument(Convert.ToInt32(row["REGISTRATION_ID"]), request.DocumentCode, storedFileName, request.ContentType, fileBytes.LongLength, request.UploadedByOperatorId);
            if (!upsertResult.Success)
            {
                return WcwcApiResponse.CreateError(upsertResult.Message, upsertResult.ErrorCode);
            }

            return WcwcApiResponse.CreateSuccess(new { fileName = storedFileName, documentCode = request.DocumentCode }, "Document uploaded successfully", request.RegistrationNumber);
        }

        public WcwcApiResponse DownloadDocument(string registrationNumber, string documentCode, string fileName)
        {
            var data = _documentStorageService.DownloadDocument(fileName, registrationNumber, documentCode);
            if (data == null)
            {
                return WcwcApiResponse.CreateError("Document not found", "DOCUMENT_NOT_FOUND");
            }

            return WcwcApiResponse.CreateSuccess(new { fileName = fileName, fileData = data }, "Document downloaded successfully", registrationNumber);
        }

        private WcwcApiResponse SaveInternal(int? registrationId, WcwcRegistrationUpsertRequest request)
        {
            var validationError = ValidateRequest(request);
            if (validationError != null)
            {
                return validationError;
            }

            NormalizeRequest(request);

            var duplicateCheck = _repository.FindDuplicateByContactNumbers(request.AadhaarNumber, request.MobileNumber, registrationId);
            if (!duplicateCheck.Success)
            {
                return WcwcApiResponse.CreateError(duplicateCheck.Message, duplicateCheck.ErrorCode ?? "DUPLICATE_CHECK_FAILED");
            }

            if (duplicateCheck.Data != null)
            {
                var duplicateField = (duplicateCheck.Data.DuplicateField ?? string.Empty).ToUpperInvariant();
                var duplicateRegistrationNo = string.IsNullOrWhiteSpace(duplicateCheck.Data.RegistrationNumber)
                    ? "(unknown)"
                    : duplicateCheck.Data.RegistrationNumber;

                if (duplicateField == "AADHAAR_AND_MOBILE")
                {
                    return WcwcApiResponse.CreateError(
                        "हा आधार क्रमांक आणि मोबाईल क्रमांक आधीच नोंदणीकृत आहे (अर्ज क्रमांक: " + duplicateRegistrationNo + ")",
                        "DUPLICATE_REGISTRATION_NUMBER");
                }

                if (duplicateField == "AADHAAR")
                {
                    return WcwcApiResponse.CreateError(
                        "हा आधार क्रमांक आधीच नोंदणीकृत आहे (अर्ज क्रमांक: " + duplicateRegistrationNo + ")",
                        "DUPLICATE_REGISTRATION_NUMBER");
                }

                return WcwcApiResponse.CreateError(
                    "हा मोबाईल क्रमांक आधीच नोंदणीकृत आहे (अर्ज क्रमांक: " + duplicateRegistrationNo + ")",
                    "DUPLICATE_REGISTRATION_NUMBER");
            }

            var existingDocumentCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string existingRegistrationNumber = null;
            if (registrationId.HasValue)
            {
                var currentRegistration = _repository.GetRegistration(registrationId, null);
                if (!currentRegistration.Success)
                {
                    return WcwcApiResponse.CreateError(currentRegistration.Message, currentRegistration.ErrorCode ?? "REGISTRATION_LOOKUP_FAILED");
                }

                existingDocumentCodes = GetExistingDocumentCodes(currentRegistration.Data);
                existingRegistrationNumber = ExtractRegistrationNumber(currentRegistration.Data);
            }

            var missingDocuments = GetMissingRequiredDocuments(request.Files, existingDocumentCodes);
            if (missingDocuments.Count > 0)
            {
                return WcwcApiResponse.CreateError(
                    "काही आवश्यक कागदपत्रे अपलोड केलेली नाहीत: " + string.Join(", ", missingDocuments),
                    "MISSING_REQUIRED_DOCUMENTS");
            }

            var saveResult = _repository.SaveRegistration(request, registrationId);
            if (!saveResult.Success)
            {
                return WcwcApiResponse.CreateError(saveResult.Message, saveResult.ErrorCode);
            }

            var disabilityIds = MapDisabilityIds(request.DisabilitySelections);
            var deviceIds = MapAssistiveDeviceIds(request.AssistiveDeviceSelections);

            var disabilitiesResult = _repository.ReplaceDisabilities(saveResult.Data.RegistrationId, disabilityIds);
            if (!disabilitiesResult.Success)
            {
                return WcwcApiResponse.CreateError(disabilitiesResult.Message, disabilitiesResult.ErrorCode);
            }

            var devicesResult = _repository.ReplaceDevices(saveResult.Data.RegistrationId, deviceIds);
            if (!devicesResult.Success)
            {
                return WcwcApiResponse.CreateError(devicesResult.Message, devicesResult.ErrorCode);
            }

            foreach (var file in request.Files.Where(ShouldPersistDocument))
            {
                var documentCode = ResolveDocumentCode(file.FieldName);
                if (string.IsNullOrWhiteSpace(documentCode))
                {
                    continue;
                }

                var storageRegistrationNumber = !string.IsNullOrWhiteSpace(saveResult.Data.RegistrationNumber)
                    ? saveResult.Data.RegistrationNumber
                    : existingRegistrationNumber;

                if (string.IsNullOrWhiteSpace(storageRegistrationNumber))
                {
                    return WcwcApiResponse.CreateError("Registration number not found for document upload", "REGISTRATION_NO_MISSING");
                }

                var base64Data = Convert.ToBase64String(file.Bytes);
                var storedFileName = _documentStorageService.UploadDocument(
                    storageRegistrationNumber,
                    documentCode,
                    new ConsentDocumentDto
                    {
                        FileName = file.FileName,
                        FileData = base64Data,
                        FileSize = file.FileSize,
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    });

                var documentResult = _repository.UpsertDocument(
                    saveResult.Data.RegistrationId,
                    documentCode,
                    storedFileName,
                    string.IsNullOrWhiteSpace(file.ContentType) ? GetMimeType(file.FileName) : file.ContentType,
                    file.FileSize,
                    request.OperatorUserId);

                if (!documentResult.Success)
                {
                    return WcwcApiResponse.CreateError(documentResult.Message, documentResult.ErrorCode);
                }
            }

            var registrationResult = _repository.GetRegistration(saveResult.Data.RegistrationId, null);
            return WcwcApiResponse.CreateSuccess(registrationResult.Data, saveResult.Message, saveResult.Data.RegistrationNumber);
        }

        private WcwcApiResponse ValidateRequest(WcwcRegistrationUpsertRequest request)
        {
            if (request == null)
            {
                return WcwcApiResponse.CreateError("Request body is required", "MISSING_REQUEST_BODY");
            }

            if (string.IsNullOrWhiteSpace(request.Surname) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.FatherName))
            {
                return WcwcApiResponse.CreateError("Surname, first name, and father name are required", "MISSING_NAME_FIELDS");
            }

            if (string.IsNullOrWhiteSpace(request.AadhaarNumber) || request.AadhaarNumber.Trim().Length != 12)
            {
                return WcwcApiResponse.CreateError("Valid 12 digit Aadhaar number is required", "INVALID_AADHAAR");
            }

            if (string.IsNullOrWhiteSpace(request.RationCardNumber))
            {
                return WcwcApiResponse.CreateError("Ration card number is required", "INVALID_RATION_CARD_NUMBER");
            }

            var rationCardColor = (request.RationCardColor ?? string.Empty).Trim().ToUpperInvariant();
            if (rationCardColor != "WHITE" && rationCardColor != "YELLOW" && rationCardColor != "ORANGE")
            {
                return WcwcApiResponse.CreateError("Ration card color must be WHITE, YELLOW, or ORANGE", "INVALID_RATION_CARD_COLOR");
            }

            if (string.IsNullOrWhiteSpace(request.MobileNumber) || request.MobileNumber.Trim().Length != 10)
            {
                return WcwcApiResponse.CreateError("Valid 10 digit mobile number is required", "INVALID_MOBILE");
            }

            if (request.DisabilitySelections == null || request.DisabilitySelections.Count == 0)
            {
                return WcwcApiResponse.CreateError("At least one disability type is required", "DISABILITY_REQUIRED");
            }

            return null;
        }

        private static void NormalizeRequest(WcwcRegistrationUpsertRequest request)
        {
            request.ApplicationMode = string.IsNullOrWhiteSpace(request.ApplicationMode) ? "PUBLIC" : request.ApplicationMode.Trim().ToUpperInvariant();
            request.SubmissionChannel = string.IsNullOrWhiteSpace(request.SubmissionChannel)
                ? (request.ApplicationMode == "DEPARTMENT" ? "DEPARTMENT_OPERATOR" : "SELF")
                : request.SubmissionChannel.Trim().ToUpperInvariant();
            request.AadhaarNumber = string.IsNullOrWhiteSpace(request.AadhaarNumber) ? request.AadhaarNumber : request.AadhaarNumber.Trim();
            request.MobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber) ? request.MobileNumber : request.MobileNumber.Trim();
            request.RationCardColor = string.IsNullOrWhiteSpace(request.RationCardColor)
                ? request.RationCardColor
                : request.RationCardColor.Trim().ToUpperInvariant();

            request.HasCertificate = NormalizeFlag(request.HasCertificate);
            request.HasUdid = NormalizeFlag(request.HasUdid);
            request.HasStPass = NormalizeFlag(request.HasStPass);
            request.HasRailwayPass = NormalizeFlag(request.HasRailwayPass);
            request.HasMsrtcPass = NormalizeFlag(request.HasMsrtcPass);
            request.IsEmployed = NormalizeFlag(request.IsEmployed);
            request.HasGovtBenefit = NormalizeFlag(request.HasGovtBenefit);
            request.HasMcBenefit = NormalizeFlag(request.HasMcBenefit);
            request.HasSgnPension = NormalizeFlag(request.HasSgnPension);
            request.HasOwnHouse = NormalizeFlag(request.HasOwnHouse);
            request.WantsHousingBenefit = NormalizeFlag(request.WantsHousingBenefit);
            request.HasOwnLand = NormalizeFlag(request.HasOwnLand);
            request.HasGuardianship = NormalizeFlag(request.HasGuardianship);
            request.NeedsAssistiveDevice = NormalizeFlag(request.NeedsAssistiveDevice);
            request.DocumentsSubmitted = NormalizeFlag(request.DocumentsSubmitted);
        }

        private WcwcOperationResult<object> ResolveIdentifier(string identifier)
        {
            return _repository.ResolveRegistration(identifier);
        }

        private static Dictionary<string, object> GetFirstRow(object data)
        {
            var bundle = data as Dictionary<string, object>;
            if (bundle == null || !bundle.ContainsKey("registration"))
            {
                return null;
            }

            var rows = bundle["registration"] as List<Dictionary<string, object>>;
            return rows != null && rows.Count > 0 ? rows[0] : null;
        }

        private static string ExtractRegistrationNumber(object data)
        {
            var row = GetFirstRow(data);
            return row != null && row.ContainsKey("REGISTRATION_NO") ? Convert.ToString(row["REGISTRATION_NO"]) : null;
        }

        private static IEnumerable<int> MapDisabilityIds(IEnumerable<string> selections)
        {
            return (selections ?? Enumerable.Empty<string>())
                .Select(GetNormalizedSelectionKey)
                .Where(DisabilityTypeMap.ContainsKey)
                .Select(functionKey => DisabilityTypeMap[functionKey])
                .Distinct()
                .ToList();
        }

        private static IEnumerable<int> MapAssistiveDeviceIds(IEnumerable<string> selections)
        {
            return (selections ?? Enumerable.Empty<string>())
                .Select(selection => selection == null ? string.Empty : selection.Trim())
                .Where(AssistiveDeviceMap.ContainsKey)
                .Select(functionKey => AssistiveDeviceMap[functionKey])
                .Distinct()
                .ToList();
        }

        private static string GetNormalizedSelectionKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var value = raw.Trim();
            var dotIndex = value.IndexOf('.');
            if (dotIndex >= 0 && dotIndex < value.Length - 1)
            {
                value = value.Substring(dotIndex + 1).Trim();
            }

            return value.ToLowerInvariant();
        }

        private static string NormalizeFlag(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "N";
            }

            var normalized = value.Trim();
            return normalized == "Y" || normalized == "YES" || normalized == "TRUE" || normalized == "होय" || normalized == "आहे"
                ? "Y"
                : "N";
        }

        private static string GetValue(Dictionary<string, object> row, string key)
        {
            if (row == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            object value;
            return row.TryGetValue(key.ToUpperInvariant(), out value) && value != null ? Convert.ToString(value) : null;
        }

        private static string ToMarathiStatusLabel(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return string.Empty;
            }

            switch (status.Trim().ToUpperInvariant())
            {
                case "SUBMITTED": return "नवीन";
                case "UNDER_REVIEW": return "आढाव्याधीन";
                case "APPROVED": return "मंजूर";
                case "REJECTED": return "नाकारले";
                case "DUPLICATE": return "डुप्लिकेट";
                case "CANCELLED": return "रद्द";
                case "DRAFT": return "ड्राफ्ट";
                default: return status;
            }
        }

        private static string BuildStatusSmsMessage(string registrationNumber, string status, string statusLabel, string statusUrl)
        {
            var normalizedStatus = string.IsNullOrWhiteSpace(status)
                ? string.Empty
                : status.Trim().ToUpperInvariant();

            switch (normalizedStatus)
            {
                case "UNDER_REVIEW":
                    return string.Format("SMKC सूचना: अर्ज क्र. {0} सध्या तपासणी प्रक्रियेत आहे ({1}). स्थिती पाहा: {2}", registrationNumber, statusLabel, statusUrl);
                case "APPROVED":
                    return string.Format("SMKC अभिनंदन: अर्ज क्र. {0} मंजूर झाला आहे. पुढील माहिती व स्थिती येथे पहा: {1}", registrationNumber, statusUrl);
                case "REJECTED":
                    return string.Format("SMKC सूचना: अर्ज क्र. {0} नाकारला गेला आहे ({1}). कारण/तपशील पाहण्यासाठी लिंक उघडा: {2}", registrationNumber, statusLabel, statusUrl);
                case "DUPLICATE":
                    return string.Format("SMKC सूचना: अर्ज क्र. {0} डुप्लिकेट म्हणून नोंदला गेला आहे. तपशील पाहा: {1}", registrationNumber, statusUrl);
                case "CANCELLED":
                    return string.Format("SMKC सूचना: अर्ज क्र. {0} रद्द करण्यात आला आहे. तपशील पाहा: {1}", registrationNumber, statusUrl);
                default:
                    return string.Format("SMKC सूचना: अर्ज क्र. {0} ची सद्यस्थिती {1} आहे. अधिक माहितीसाठी येथे पहा: {2}", registrationNumber, statusLabel, statusUrl);
            }
        }

        private static string MaskMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 4)
            {
                return mobile;
            }

            return new string('X', Math.Max(0, mobile.Length - 4)) + mobile.Substring(mobile.Length - 4);
        }

        private static bool ShouldPersistDocument(WcwcUploadedFile file)
        {
            return file != null
                && file.Bytes != null
                && file.Bytes.Length > 0
                && !string.IsNullOrWhiteSpace(ResolveDocumentCode(file.FieldName));
        }

        private static string ResolveDocumentCode(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return null;
            }

            var raw = fieldName.Trim();
            string code;
            if (FieldToDocumentCode.TryGetValue(raw, out code))
            {
                return code;
            }

            var compact = raw.Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);

            if (FieldToDocumentCode.TryGetValue(compact, out code))
            {
                return code;
            }

            switch (compact.ToLowerInvariant())
            {
                case "udiddoc":
                    return "UDID_DOC";
                case "aadhaardoc":
                    return "AADHAAR_DOC";
                case "rationcarddoc":
                case "rationdoc":
                    return "RATION_CARD_DOC";
                case "bankdoc":
                    return "BANK_DOC";
                case "photodoc":
                    return "PHOTO_DOC";
                case "applicantsignature":
                    return "APPLICANT_SIGNATURE";
                case "surveyorsignature":
                    return "SURVEYOR_SIGNATURE";
                default:
                    return null;
            }
        }

        private static HashSet<string> GetExistingDocumentCodes(object data)
        {
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var bundle = data as Dictionary<string, object>;
            if (bundle == null || !bundle.ContainsKey("documents"))
            {
                return codes;
            }

            var rows = bundle["documents"] as List<Dictionary<string, object>>;
            if (rows == null)
            {
                return codes;
            }

            foreach (var row in rows)
            {
                if (row == null || !row.ContainsKey("DOCUMENT_CODE"))
                {
                    continue;
                }

                var code = Convert.ToString(row["DOCUMENT_CODE"]);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    codes.Add(code.Trim());
                }
            }

            return codes;
        }

        private static List<string> GetMissingRequiredDocuments(IEnumerable<WcwcUploadedFile> files, ISet<string> existingDocumentCodes)
        {
            var uploadedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files ?? Enumerable.Empty<WcwcUploadedFile>())
            {
                var code = file == null ? null : ResolveDocumentCode(file.FieldName);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    uploadedCodes.Add(code);
                }
            }

            var missing = new List<string>();
            foreach (var documentCode in RequiredDocumentCodes)
            {
                if (!uploadedCodes.Contains(documentCode) && (existingDocumentCodes == null || !existingDocumentCodes.Contains(documentCode)))
                {
                    missing.Add(GetRequiredDocumentLabel(documentCode));
                }
            }

            return missing;
        }

        private static string GetRequiredDocumentLabel(string documentCode)
        {
            switch (documentCode)
            {
                case "UDID_DOC": return "UDID / दिव्यांग प्रमाणपत्र";
                case "AADHAAR_DOC": return "आधारकार्ड";
                case "RATION_CARD_DOC": return "रेशनकार्ड";
                case "BANK_DOC": return "बँक खाते झेरॉक्स";
                case "PHOTO_DOC": return "फोटो";
                case "APPLICANT_SIGNATURE": return "अर्जदाराची सही";
                default: return documentCode;
            }
        }

        private static byte[] DecodeBase64(string fileData)
        {
            var raw = fileData;
            if (raw.Contains(","))
            {
                var commaIndex = raw.IndexOf(',');
                if (raw.Substring(0, commaIndex).Contains("base64"))
                {
                    raw = raw.Substring(commaIndex + 1);
                }
            }

            return Convert.FromBase64String(raw);
        }

        private static string GetMimeType(string fileName)
        {
            return System.Web.MimeMapping.GetMimeMapping(fileName ?? string.Empty);
        }
    }
}