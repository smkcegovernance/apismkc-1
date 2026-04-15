using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        WcwcApiResponse Search(string searchText, string status, string applicationMode, int? operatorUserId);
        WcwcApiResponse Cancel(string identifier, WcwcStatusUpdateRequest request);
        WcwcApiResponse OperatorLogin(WcwcOperatorLoginRequest request);
        WcwcApiResponse UploadDocument(WcwcDocumentUploadRequest request);
        WcwcApiResponse DownloadDocument(string registrationNumber, string documentCode, string fileName);
    }

    public class WcwcDisabilityService : IWcwcDisabilityService
    {
        private readonly IWcwcDisabilityRepository _repository;
        private readonly IWcwcDocumentStorageService _documentStorageService;

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
            { "aadhaarDoc", "AADHAAR_DOC" },
            { "bankDoc", "BANK_DOC" },
            { "photoDoc", "PHOTO_DOC" },
            { "applicantSignature", "APPLICANT_SIGNATURE" },
            { "surveyorSignature", "SURVEYOR_SIGNATURE" }
        };

        public WcwcDisabilityService(IWcwcDisabilityRepository repository, IWcwcDocumentStorageService documentStorageService)
        {
            _repository = repository;
            _documentStorageService = documentStorageService;
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

        public WcwcApiResponse Search(string searchText, string status, string applicationMode, int? operatorUserId)
        {
            var result = _repository.SearchRegistrations(searchText, status, applicationMode, operatorUserId);
            if (!result.Success)
            {
                return WcwcApiResponse.CreateError(result.Message, result.ErrorCode);
            }

            return WcwcApiResponse.CreateSuccess(result.Data, result.Message, null);
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
                var documentCode = FieldToDocumentCode[file.FieldName];
                var base64Data = Convert.ToBase64String(file.Bytes);
                var storedFileName = _documentStorageService.UploadDocument(
                    saveResult.Data.RegistrationNumber,
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

        private static bool ShouldPersistDocument(WcwcUploadedFile file)
        {
            return file != null && file.Bytes != null && file.Bytes.Length > 0 && FieldToDocumentCode.ContainsKey(file.FieldName);
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