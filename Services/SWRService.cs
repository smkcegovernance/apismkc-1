using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmkcApi.Models;
using SmkcApi.Models.DepositManager;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface ISWRService
    {
        SWRApiResponse Register(SWRRegistrationRequest request);
        SWRApiResponse Update(string identifier, SWRRegistrationRequest request);
        SWRApiResponse GetRegistration(string identifier);
        SWRApiResponse Search(string searchText, string status, string applicationMode);
        SWRApiResponse UpdateStatus(string identifier, SWRStatusUpdateRequest request);
        SWRApiResponse DeleteRegistration(string identifier, SWRStatusUpdateRequest request);
        SWRApiResponse DownloadDocument(string registrationNumber, string documentCode, string fileName);
    }

    public class SWRService : ISWRService
    {
        private readonly ISWRRepository _repository;
        private readonly IWcwcDocumentStorageService _documentStorageService;

        public SWRService(ISWRRepository repository, IWcwcDocumentStorageService documentStorageService)
        {
            _repository = repository;
            _documentStorageService = documentStorageService;
        }

        // ---------------------------------------------------------------
        //  Register
        // ---------------------------------------------------------------
        public SWRApiResponse Register(SWRRegistrationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return SWRApiResponse.CreateError("Full name is required", "VALIDATION_ERROR");
            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                return SWRApiResponse.CreateError("Mobile number is required", "VALIDATION_ERROR");

            if (!string.IsNullOrWhiteSpace(request.AadhaarNumber))
            {
                var duplicateAadhaarResult = _repository.AadhaarExists(request.AadhaarNumber, null);
                if (!duplicateAadhaarResult.Success)
                    return SWRApiResponse.CreateError(duplicateAadhaarResult.Message, duplicateAadhaarResult.ErrorCode ?? "DB_ERROR");
                if (duplicateAadhaarResult.Data)
                    return SWRApiResponse.CreateError("This Aadhaar number is already registered", "DUPLICATE_AADHAAR");
            }

            var saveResult = _repository.SaveRegistration(request, null);
            if (!saveResult.Success)
                return SWRApiResponse.CreateError(saveResult.Message, saveResult.ErrorCode ?? "SAVE_ERROR");

            var regId = saveResult.Data.RegId;

            var dedicatedSave = _repository.UpsertDedicatedFields(regId, BuildDedicatedFields(request));
            if (!dedicatedSave.Success)
                return SWRApiResponse.CreateError(dedicatedSave.Message, dedicatedSave.ErrorCode ?? "SAVE_ERROR");

            // Save schemes if provided
            if (request.Schemes != null && request.Schemes.Count > 0)
            {
                var schemesResult = _repository.ReplaceSchemes(regId, request.Schemes);
                if (!schemesResult.Success)
                    return SWRApiResponse.CreateError(schemesResult.Message, schemesResult.ErrorCode ?? "SCHEME_ERROR");
            }

            var documentsResult = PersistUploadedDocuments(saveResult.Data.RegistrationNumber, request, regId);
            if (!documentsResult.Success)
                return documentsResult;

            return SWRApiResponse.CreateSuccess(
                new
                {
                    regId,
                    registrationNumber = saveResult.Data.RegistrationNumber,
                    uploadedDocuments = documentsResult.Data
                },
                "Registration completed successfully",
                saveResult.Data.RegistrationNumber
            );
        }

        // ---------------------------------------------------------------
        //  Update
        // ---------------------------------------------------------------
        public SWRApiResponse Update(string identifier, SWRRegistrationRequest request)
        {
            var resolveResult = _repository.ResolveRegistration(identifier);
            if (!resolveResult.Success)
                return SWRApiResponse.CreateError(resolveResult.Message, resolveResult.ErrorCode ?? "NOT_FOUND");

            dynamic existing = resolveResult.Data;
            int regId = Convert.ToInt32(existing["reg_id"]);

            if (!string.IsNullOrWhiteSpace(request.AadhaarNumber))
            {
                var duplicateAadhaarResult = _repository.AadhaarExists(request.AadhaarNumber, regId);
                if (!duplicateAadhaarResult.Success)
                    return SWRApiResponse.CreateError(duplicateAadhaarResult.Message, duplicateAadhaarResult.ErrorCode ?? "DB_ERROR");
                if (duplicateAadhaarResult.Data)
                    return SWRApiResponse.CreateError("This Aadhaar number is already registered", "DUPLICATE_AADHAAR");
            }

            var saveResult = _repository.SaveRegistration(request, regId);
            if (!saveResult.Success)
                return SWRApiResponse.CreateError(saveResult.Message, saveResult.ErrorCode ?? "SAVE_ERROR");

            var dedicatedSave = _repository.UpsertDedicatedFields(regId, BuildDedicatedFields(request));
            if (!dedicatedSave.Success)
                return SWRApiResponse.CreateError(dedicatedSave.Message, dedicatedSave.ErrorCode ?? "SAVE_ERROR");

            if (request.Schemes != null)
            {
                var schemesResult = _repository.ReplaceSchemes(regId, request.Schemes);
                if (!schemesResult.Success)
                    return SWRApiResponse.CreateError(schemesResult.Message, schemesResult.ErrorCode ?? "SCHEME_ERROR");
            }

            var documentsResult = PersistUploadedDocuments(saveResult.Data.RegistrationNumber, request, regId);
            if (!documentsResult.Success)
                return documentsResult;

            return SWRApiResponse.CreateSuccess(
                new
                {
                    regId,
                    registrationNumber = saveResult.Data.RegistrationNumber,
                    uploadedDocuments = documentsResult.Data
                },
                "Registration updated successfully",
                saveResult.Data.RegistrationNumber
            );
        }

        // ---------------------------------------------------------------
        //  Get
        // ---------------------------------------------------------------
        public SWRApiResponse GetRegistration(string identifier)
        {
            var result = _repository.ResolveRegistration(identifier);
            if (!result.Success)
                return SWRApiResponse.CreateError(result.Message, result.ErrorCode ?? "NOT_FOUND");

            var row = result.Data as Dictionary<string, object>;
            if (row != null)
            {
                HydrateOtherInfoFromDedicatedColumns(row);
            }

            return SWRApiResponse.CreateSuccess(result.Data, "OK");
        }

        // ---------------------------------------------------------------
        //  Search
        // ---------------------------------------------------------------
        public SWRApiResponse Search(string searchText, string status, string applicationMode)
        {
            var result = _repository.Search(searchText, status, applicationMode);
            if (!result.Success)
                return SWRApiResponse.CreateError(result.Message, result.ErrorCode ?? "SEARCH_ERROR");

            HydrateSearchResponse(result.Data);

            return SWRApiResponse.CreateSuccess(result.Data, "OK");
        }

        // ---------------------------------------------------------------
        //  Update Status
        // ---------------------------------------------------------------
        public SWRApiResponse UpdateStatus(string identifier, SWRStatusUpdateRequest request)
        {
            var resolveResult = _repository.ResolveRegistration(identifier);
            if (!resolveResult.Success)
                return SWRApiResponse.CreateError(resolveResult.Message, resolveResult.ErrorCode ?? "NOT_FOUND");

            dynamic existing = resolveResult.Data;
            int regId = Convert.ToInt32(existing["reg_id"]);

            var result = _repository.UpdateStatus(regId, request.Status, request.Remarks, request.OperatorUserId);
            if (!result.Success)
                return SWRApiResponse.CreateError(result.Message, result.ErrorCode ?? "UPDATE_ERROR");

            return SWRApiResponse.CreateSuccess(null, "Status updated successfully");
        }

        public SWRApiResponse DeleteRegistration(string identifier, SWRStatusUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Remarks) || request.Remarks.Trim().Length < 10)
            {
                return SWRApiResponse.CreateError("Delete reason (minimum 10 characters) is required", "DELETE_REASON_REQUIRED");
            }

            var resolveResult = _repository.ResolveRegistration(identifier);
            if (!resolveResult.Success)
                return SWRApiResponse.CreateError(resolveResult.Message, resolveResult.ErrorCode ?? "NOT_FOUND");

            dynamic existing = resolveResult.Data;
            int regId = Convert.ToInt32(existing["reg_id"]);
            string regNo = Convert.ToString(existing["reg_number"]);

            var result = _repository.DeleteRegistration(regId);
            if (!result.Success)
                return SWRApiResponse.CreateError(result.Message, result.ErrorCode ?? "DELETE_ERROR");

            return SWRApiResponse.CreateSuccess(
                null,
                "Registration permanently deleted",
                regNo
            );
        }

        public SWRApiResponse DownloadDocument(string registrationNumber, string documentCode, string fileName)
        {
            if (string.IsNullOrWhiteSpace(registrationNumber) || string.IsNullOrWhiteSpace(documentCode))
            {
                return SWRApiResponse.CreateError("registrationNumber and documentCode are required", "INVALID_REQUEST");
            }

            var resolvedRegistrationNumber = ResolveRegistrationNumberForDownload(registrationNumber);
            if (string.IsNullOrWhiteSpace(resolvedRegistrationNumber))
            {
                return SWRApiResponse.CreateError("Registration not found", "REG_NOT_FOUND");
            }

            var resolvedFileName = CoerceString(fileName, 260);
            if (string.IsNullOrWhiteSpace(resolvedFileName))
            {
                resolvedFileName = ResolveDocumentFileNameFromSnapshot(registrationNumber, documentCode);
            }

            if (string.IsNullOrWhiteSpace(resolvedFileName))
            {
                resolvedFileName = _documentStorageService.ResolveDocumentFileName(resolvedRegistrationNumber, documentCode);
            }

            if (string.IsNullOrWhiteSpace(resolvedFileName))
            {
                return SWRApiResponse.CreateError("File name is required for this document", "FILE_NAME_REQUIRED");
            }

            var data = _documentStorageService.DownloadDocument(resolvedFileName, resolvedRegistrationNumber, documentCode);
            if (data == null)
            {
                return SWRApiResponse.CreateError("Document not found", "DOCUMENT_NOT_FOUND");
            }

            return SWRApiResponse.CreateSuccess(new { fileName = resolvedFileName, fileData = data }, "Document downloaded successfully", resolvedRegistrationNumber);
        }

        private string ResolveDocumentFileNameFromSnapshot(string registrationIdentifier, string documentCode)
        {
            if (string.IsNullOrWhiteSpace(registrationIdentifier) || string.IsNullOrWhiteSpace(documentCode))
            {
                return string.Empty;
            }

            var resolveResult = _repository.ResolveRegistration(registrationIdentifier.Trim());
            if (!resolveResult.Success || resolveResult.Data == null)
            {
                return string.Empty;
            }

            var row = resolveResult.Data as Dictionary<string, object>;
            if (row == null)
            {
                return string.Empty;
            }

            var otherInfoRaw = CoerceString(ReadValue(row, "other_info"), 2000000);
            if (string.IsNullOrWhiteSpace(otherInfoRaw))
            {
                return string.Empty;
            }

            try
            {
                var snapshot = JsonConvert.DeserializeObject<Dictionary<string, object>>(otherInfoRaw)
                               ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                var documents = ExtractObject(snapshot, "documents");
                var files = ExtractObject(documents, "files");

                var expandedKey = ResolveExpandedFileNameKey(documentCode);
                if (!string.IsNullOrWhiteSpace(expandedKey))
                {
                    var fromExpanded = CoerceString(ReadValue(files, expandedKey), 260);
                    if (!string.IsNullOrWhiteSpace(fromExpanded))
                    {
                        return fromExpanded;
                    }
                }

                var compactFiles = ExtractObject(documents, "f");
                var compactKey = ResolveCompactFileNameKey(documentCode);
                if (!string.IsNullOrWhiteSpace(compactKey))
                {
                    return CoerceString(ReadValue(compactFiles, compactKey), 260);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string ResolveExpandedFileNameKey(string documentCode)
        {
            switch ((documentCode ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "SWR_SINGLE_WOMAN_PROOF": return "singleWomanProofFileName";
                case "SWR_AADHAAR_COPY": return "aadhaarCopyFileName";
                case "SWR_RATION_COPY": return "rationCopyFileName";
                case "SWR_VOTER_COPY": return "voterCopyFileName";
                case "SWR_BANK_PASSBOOK": return "bankPassbookFileName";
                case "SWR_INCOME_CERTIFICATE": return "incomeCertificateFileName";
                case "SWR_DISABILITY_CERTIFICATE": return "disabilityCertificateFileName";
                case "SWR_OTHER_DOCUMENT": return "otherDocumentFileName";
                default: return string.Empty;
            }
        }

        private static string ResolveCompactFileNameKey(string documentCode)
        {
            switch ((documentCode ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "SWR_SINGLE_WOMAN_PROOF": return "swp";
                case "SWR_AADHAAR_COPY": return "aad";
                case "SWR_RATION_COPY": return "rat";
                case "SWR_VOTER_COPY": return "vot";
                case "SWR_BANK_PASSBOOK": return "bnk";
                case "SWR_INCOME_CERTIFICATE": return "inc";
                case "SWR_DISABILITY_CERTIFICATE": return "dis";
                case "SWR_OTHER_DOCUMENT": return "oth";
                default: return string.Empty;
            }
        }

        private string ResolveRegistrationNumberForDownload(string registrationNumberOrId)
        {
            if (string.IsNullOrWhiteSpace(registrationNumberOrId))
                return null;

            int regId;
            if (!int.TryParse(registrationNumberOrId.Trim(), out regId))
                return registrationNumberOrId;

            var resolveResult = _repository.ResolveRegistration(registrationNumberOrId.Trim());
            if (!resolveResult.Success || resolveResult.Data == null)
                return null;

            var row = resolveResult.Data as Dictionary<string, object>;
            if (row == null)
                return null;

            object resolved;
            if (!row.TryGetValue("reg_number", out resolved) || resolved == null)
                return null;

            var value = resolved.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private SWRApiResponse PersistUploadedDocuments(string registrationNumber, SWRRegistrationRequest request, int regId)
        {
            if (request == null || request.Files == null || request.Files.Count == 0)
                return SWRApiResponse.CreateSuccess(null, "No files to persist", registrationNumber);

            var uploaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var uploadedFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in request.Files)
            {
                var documentCode = ResolveDocumentCode(file.FieldName);
                if (string.IsNullOrWhiteSpace(documentCode))
                    continue;

                try
                {
                    var base64Data = Convert.ToBase64String(file.Bytes ?? new byte[0]);
                    var storedFileName = _documentStorageService.UploadDocument(
                        registrationNumber,
                        documentCode,
                        new ConsentDocumentDto
                        {
                            FileName = file.FileName,
                            FileData = base64Data,
                            FileSize = file.FileSize,
                            ContentType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        });

                    uploaded[GetDocumentSnapshotKey(file.FieldName)] = storedFileName;
                    uploadedFlags[GetDocumentFlagKey(file.FieldName)] = true;
                }
                catch (Exception ex)
                {
                    var errorCode = IsStorageUnavailable(ex)
                        ? "DOCUMENT_STORAGE_UNAVAILABLE"
                        : "DOCUMENT_UPLOAD_FAILED";

                    return SWRApiResponse.CreateError(
                        "Document upload failed for " + (file.FileName ?? file.FieldName) + ": " + ex.Message,
                        errorCode);
                }
            }

            if (uploaded.Count == 0)
                return SWRApiResponse.CreateSuccess(null, "No supported files uploaded", registrationNumber);

            var updatedOtherInfo = UpdateDocumentSnapshot(request.OtherInfo, uploaded, uploadedFlags);
            request.OtherInfo = updatedOtherInfo;

            try
            {
                var resave = _repository.SaveRegistration(request, regId);
                if (!resave.Success)
                    return SWRApiResponse.CreateError(resave.Message, resave.ErrorCode ?? "DOC_SNAPSHOT_SAVE_ERROR");
            }
            catch (Exception ex)
            {
                return SWRApiResponse.CreateError("Document snapshot save failed: " + ex.Message, "DOC_SNAPSHOT_SAVE_ERROR");
            }

            return SWRApiResponse.CreateSuccess(uploaded, "Documents uploaded successfully", registrationNumber);
        }

        private static bool IsStorageUnavailable(Exception ex)
        {
            if (ex is WebException)
                return true;

            if (ex is IOException)
                return true;

            var message = (ex.Message ?? string.Empty).ToLowerInvariant();
            return message.Contains("timeout")
                || message.Contains("timed out")
                || message.Contains("connection")
                || message.Contains("refused")
                || message.Contains("unreachable")
                || message.Contains("550")
                || message.Contains("quota");
        }

        private static string ResolveDocumentCode(string fieldName)
        {
            switch ((fieldName ?? string.Empty).Trim())
            {
                case "singleWomanProofFile": return "SWR_SINGLE_WOMAN_PROOF";
                case "aadhaarCopyFile": return "SWR_AADHAAR_COPY";
                case "rationCopyFile": return "SWR_RATION_COPY";
                case "voterCopyFile": return "SWR_VOTER_COPY";
                case "bankPassbookFile": return "SWR_BANK_PASSBOOK";
                case "incomeCertificateFile": return "SWR_INCOME_CERTIFICATE";
                case "disabilityCertificateFile": return "SWR_DISABILITY_CERTIFICATE";
                case "otherDocumentFile": return "SWR_OTHER_DOCUMENT";
                default: return null;
            }
        }

        private static string GetDocumentSnapshotKey(string fieldName)
        {
            switch ((fieldName ?? string.Empty).Trim())
            {
                case "singleWomanProofFile": return "singleWomanProofFileName";
                case "aadhaarCopyFile": return "aadhaarCopyFileName";
                case "rationCopyFile": return "rationCopyFileName";
                case "voterCopyFile": return "voterCopyFileName";
                case "bankPassbookFile": return "bankPassbookFileName";
                case "incomeCertificateFile": return "incomeCertificateFileName";
                case "disabilityCertificateFile": return "disabilityCertificateFileName";
                case "otherDocumentFile": return "otherDocumentFileName";
                default: return fieldName ?? string.Empty;
            }
        }

        private static string GetDocumentFlagKey(string fieldName)
        {
            switch ((fieldName ?? string.Empty).Trim())
            {
                case "singleWomanProofFile": return "singleWomanProof";
                case "aadhaarCopyFile": return "aadhaarCopy";
                case "rationCopyFile": return "rationCopy";
                case "voterCopyFile": return "voterCopy";
                case "bankPassbookFile": return "bankPassbookOrCheque";
                case "incomeCertificateFile": return "incomeCertificate";
                case "disabilityCertificateFile": return "disabilityCertificate";
                case "otherDocumentFile": return "other";
                default: return string.Empty;
            }
        }

        private const int OtherInfoMaxLength = 2000000;

        private static string UpdateDocumentSnapshot(string otherInfo, IDictionary<string, string> uploadedFiles, IDictionary<string, bool> uploadedFlags)
        {
            var snapshot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(otherInfo))
            {
                try
                {
                    snapshot = JsonConvert.DeserializeObject<Dictionary<string, object>>(otherInfo) ?? snapshot;
                }
                catch
                {
                    snapshot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                }
            }

            HydrateCompactSnapshot(snapshot);

            object documentsObj;
            var documents = snapshot.TryGetValue("documents", out documentsObj) && documentsObj is JObject
                ? ((JObject)documentsObj).ToObject<Dictionary<string, object>>()
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            object filesObj;
            var files = documents.TryGetValue("files", out filesObj) && filesObj is JObject
                ? ((JObject)filesObj).ToObject<Dictionary<string, object>>()
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (files.Count == 0)
            {
                object compactFilesObj;
                if (documents.TryGetValue("f", out compactFilesObj) && compactFilesObj is JObject)
                {
                    var compactFiles = ((JObject)compactFilesObj).ToObject<Dictionary<string, object>>()
                        ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    files["singleWomanProofFileName"] = CoerceString(ReadValue(compactFiles, "swp"), 120);
                    files["aadhaarCopyFileName"] = CoerceString(ReadValue(compactFiles, "aad"), 120);
                    files["rationCopyFileName"] = CoerceString(ReadValue(compactFiles, "rat"), 120);
                    files["voterCopyFileName"] = CoerceString(ReadValue(compactFiles, "vot"), 120);
                    files["bankPassbookFileName"] = CoerceString(ReadValue(compactFiles, "bnk"), 120);
                    files["incomeCertificateFileName"] = CoerceString(ReadValue(compactFiles, "inc"), 120);
                    files["disabilityCertificateFileName"] = CoerceString(ReadValue(compactFiles, "dis"), 120);
                    files["otherDocumentFileName"] = CoerceString(ReadValue(compactFiles, "oth"), 120);
                }
            }

            foreach (var entry in uploadedFiles)
            {
                files[entry.Key] = entry.Value;
            }

            foreach (var entry in uploadedFlags)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key))
                {
                    documents[entry.Key] = entry.Value;
                }
            }

            documents["files"] = files;
            snapshot["documents"] = documents;

            var serialized = JsonConvert.SerializeObject(snapshot);
            if (serialized.Length <= OtherInfoMaxLength)
            {
                return serialized;
            }

            // Overflow-safe fallback: keep compact keys first to preserve more user-entered fields.
            var compactSnapshot = BuildCompactOverflowSnapshot(snapshot, documents, files);
            serialized = JsonConvert.SerializeObject(compactSnapshot);
            if (serialized.Length <= OtherInfoMaxLength)
            {
                return serialized;
            }

            var compactDocs = ExtractObject(compactSnapshot, "documents");
            if (compactDocs.ContainsKey("f"))
            {
                compactDocs.Remove("f");
                compactSnapshot["documents"] = compactDocs;
                serialized = JsonConvert.SerializeObject(compactSnapshot);
                if (serialized.Length <= OtherInfoMaxLength)
                {
                    return serialized;
                }
            }

            var compactDetails = ExtractObject(compactSnapshot, "d");
            compactDetails.Remove("ch");
            compactDetails.Remove("sk");
            compactDetails["mns"] = CoerceString(ReadValue(compactDetails, "mns"), 60);
            compactDetails["eb"] = CoerceString(ReadValue(compactDetails, "eb"), 30);
            compactDetails["gsn"] = CoerceString(ReadValue(compactDetails, "gsn"), 30);
            compactDetails["asn"] = CoerceString(ReadValue(compactDetails, "asn"), 30);
            compactDetails["dp"] = CoerceString(ReadValue(compactDetails, "dp"), 30);
            compactSnapshot["d"] = compactDetails;
            serialized = JsonConvert.SerializeObject(compactSnapshot);
            if (serialized.Length <= OtherInfoMaxLength)
            {
                return serialized;
            }

            var dropOrder = new[]
            {
                "rto", "la", "bss", "cedu", "eq", "wmi", "mi", "shg", "ags", "wse",
                "dw", "rs", "lds", "ls", "rt", "tc", "bc", "gc", "cb18", "pn", "pd", "spn"
            };
            foreach (var key in dropOrder)
            {
                if (!compactDetails.ContainsKey(key))
                {
                    continue;
                }

                compactDetails.Remove(key);
                compactSnapshot["d"] = compactDetails;
                serialized = JsonConvert.SerializeObject(compactSnapshot);
                if (serialized.Length <= OtherInfoMaxLength)
                {
                    return serialized;
                }
            }

            compactSnapshot.Remove("p");
            serialized = JsonConvert.SerializeObject(compactSnapshot);
            if (serialized.Length <= OtherInfoMaxLength)
            {
                return serialized;
            }

            var critical = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "d", new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "e", CoerceString(ReadValue(compactDetails, "e"), 60) },
                        { "vn", CoerceString(ReadValue(compactDetails, "vn"), 24) },
                        { "fm", CoerceString(ReadValue(compactDetails, "fm"), 4) },
                        { "bn", CoerceString(ReadValue(compactDetails, "bn"), 32) },
                        { "brn", CoerceString(ReadValue(compactDetails, "brn"), 32) },
                        { "an", CoerceString(ReadValue(compactDetails, "an"), 24) },
                        { "if", CoerceString(ReadValue(compactDetails, "if"), 11) },
                        { "mns", CoerceString(ReadValue(compactDetails, "mns"), 40) },
                        { "eb", CoerceString(ReadValue(compactDetails, "eb"), 24) },
                        { "gsn", CoerceString(ReadValue(compactDetails, "gsn"), 24) },
                        { "rsup", ReadValue(compactDetails, "rsup") },
                        { "spn", CoerceString(ReadValue(compactDetails, "spn"), 24) },
                        { "pn", CoerceString(ReadValue(compactDetails, "pn"), 20) },
                        { "pd", CoerceString(ReadValue(compactDetails, "pd"), 12) },
                        { "dp", CoerceString(ReadValue(compactDetails, "dp"), 40) },
                        { "dd", CoerceString(ReadValue(compactDetails, "dd"), 10) },
                        { "asn", CoerceString(ReadValue(compactDetails, "asn"), 28) },
                    }
                },
                { "documents", compactDocs }
            };

            return JsonConvert.SerializeObject(critical);
        }

        private static Dictionary<string, object> BuildCompactOverflowSnapshot(IDictionary<string, object> snapshot, IDictionary<string, object> documents, IDictionary<string, object> files)
        {
            var compact = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var compactProfile = ToCaseInsensitiveDictionary(ExtractObject(snapshot, "p"));
            if (compactProfile.Count == 0)
            {
                var profile = ExtractObject(snapshot, "profile");
                compactProfile["a"] = CoerceString(ReadValue(profile, "fullAddress"), 140);
                compactProfile["p"] = CoerceString(ReadValue(profile, "pinCode"), 12);
                compactProfile["c"] = CoerceString(ReadValue(profile, "cityAreaPrabhag"), 40);
                compactProfile["o"] = CoerceString(ReadValue(profile, "officePrabhag"), 24);
            }

            var compactDetails = ToCaseInsensitiveDictionary(ExtractObject(snapshot, "d"));
            if (compactDetails.Count == 0)
            {
                var details = ExtractObject(snapshot, "details");
                compactDetails["e"] = CoerceString(ReadValue(details, "email"), 80);
                compactDetails["rn"] = CoerceString(ReadValue(details, "rationCardNumber"), 24);
                compactDetails["vn"] = CoerceString(ReadValue(details, "voterIdNumber"), 24);
                compactDetails["fm"] = CoerceString(ReadValue(details, "familyMemberCount"), 6);
                compactDetails["bn"] = CoerceString(ReadValue(details, "bankName"), 40);
                compactDetails["brn"] = CoerceString(ReadValue(details, "branchName"), 40);
                compactDetails["an"] = CoerceString(ReadValue(details, "accountNumber"), 28);
                compactDetails["if"] = CoerceString(ReadValue(details, "ifscCode"), 20);
                compactDetails["mns"] = CoerceString(ReadValue(details, "majorNeedSupport"), 100);
                compactDetails["dp"] = CoerceString(ReadValue(details, "declarationPlace"), 40);
                compactDetails["dd"] = CoerceString(ReadValue(details, "declarationDate"), 14);
                compactDetails["asn"] = CoerceString(ReadValue(details, "applicantSignatureName"), 60);
            }

            compact["p"] = compactProfile;
            compact["d"] = compactDetails;

            var compactDocuments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "sf", CoerceBool(ReadValue(documents, "singleWomanProof")) },
                { "af", CoerceBool(ReadValue(documents, "aadhaarCopy")) },
                { "rf", CoerceBool(ReadValue(documents, "rationCopy")) },
                { "vf", CoerceBool(ReadValue(documents, "voterCopy")) },
                { "bf", CoerceBool(ReadValue(documents, "bankPassbookOrCheque")) },
                { "inf", CoerceBool(ReadValue(documents, "incomeCertificate")) },
                { "dif", CoerceBool(ReadValue(documents, "disabilityCertificate")) },
                { "of", CoerceBool(ReadValue(documents, "other")) },
            };

            var compactFiles = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "swp", CoerceString(ReadValue(files, "singleWomanProofFileName"), 120) },
                { "aad", CoerceString(ReadValue(files, "aadhaarCopyFileName"), 120) },
                { "rat", CoerceString(ReadValue(files, "rationCopyFileName"), 120) },
                { "vot", CoerceString(ReadValue(files, "voterCopyFileName"), 120) },
                { "bnk", CoerceString(ReadValue(files, "bankPassbookFileName"), 120) },
                { "inc", CoerceString(ReadValue(files, "incomeCertificateFileName"), 120) },
                { "dis", CoerceString(ReadValue(files, "disabilityCertificateFileName"), 120) },
                { "oth", CoerceString(ReadValue(files, "otherDocumentFileName"), 120) },
            };

            compactDocuments["f"] = compactFiles;
            compact["documents"] = compactDocuments;
            return compact;
        }

        private static Dictionary<string, object> ToCaseInsensitiveDictionary(IDictionary<string, object> source)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (source == null)
            {
                return result;
            }

            foreach (var pair in source)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        private static bool CoerceBool(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            var text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            bool parsed;
            return bool.TryParse(text, out parsed) && parsed;
        }

        private static void HydrateCompactSnapshot(IDictionary<string, object> snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var compactProfile = ExtractObject(snapshot, "p");
            var compactDetails = ExtractObject(snapshot, "d");
            var profile = ExtractObject(snapshot, "profile");
            var details = ExtractObject(snapshot, "details");

            if (compactProfile.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(CoerceString(ReadValue(profile, "fullAddress"), 500)))
                    profile["fullAddress"] = CoerceString(ReadValue(compactProfile, "a"), 500);
                if (string.IsNullOrWhiteSpace(CoerceString(ReadValue(profile, "pinCode"), 50)))
                    profile["pinCode"] = CoerceString(ReadValue(compactProfile, "p"), 50);
                if (string.IsNullOrWhiteSpace(CoerceString(ReadValue(profile, "cityAreaPrabhag"), 200)))
                    profile["cityAreaPrabhag"] = CoerceString(ReadValue(compactProfile, "c"), 200);
                if (string.IsNullOrWhiteSpace(CoerceString(ReadValue(profile, "officePrabhag"), 200)))
                    profile["officePrabhag"] = CoerceString(ReadValue(compactProfile, "o"), 200);
            }

            if (compactDetails.Count > 0)
            {
                ApplyIfMissing(details, "email", compactDetails, "e");
                ApplyIfMissing(details, "rationCardType", compactDetails, "rt");
                ApplyIfMissing(details, "livelihoodSource", compactDetails, "ls");
                ApplyIfMissing(details, "isDivyangSingleWoman", compactDetails, "dw");
                ApplyIfMissing(details, "residenceType", compactDetails, "rs");
                ApplyIfMissing(details, "landStatus", compactDetails, "lds");
                ApplyIfMissing(details, "landAreaAcre", compactDetails, "la");
                ApplyIfMissing(details, "familyMemberCount", compactDetails, "fm");
                ApplyIfMissing(details, "isBpl", compactDetails, "bpl");
                ApplyIfMissing(details, "totalChildren", compactDetails, "tc");
                ApplyIfMissing(details, "boysCount", compactDetails, "bc");
                ApplyIfMissing(details, "girlsCount", compactDetails, "gc");
                ApplyIfMissing(details, "childrenBelow18Count", compactDetails, "cb18");
                ApplyIfMissing(details, "childrenInEducation", compactDetails, "cedu");
                ApplyIfMissing(details, "getsBalSangopanScholarship", compactDetails, "bss");
                ApplyIfMissing(details, "currentOccupation", compactDetails, "oc");
                ApplyIfMissing(details, "monthlyIncome", compactDetails, "mi");
                ApplyIfMissing(details, "womanMonthlyIncome", compactDetails, "wmi");
                ApplyIfMissing(details, "familyAnnualIncome", compactDetails, "fai");
                ApplyIfMissing(details, "isSelfHelpGroupMember", compactDetails, "shg");
                ApplyIfMissing(details, "accountHolderName", compactDetails, "ah");
                ApplyIfMissing(details, "majorNeedSupport", compactDetails, "mns");
                ApplyIfMissing(details, "declarationPlace", compactDetails, "dp");
                ApplyIfMissing(details, "declarationDate", compactDetails, "dd");
                ApplyIfMissing(details, "applicantSignatureName", compactDetails, "asn");
                ApplyIfMissing(details, "rationCardNumber", compactDetails, "rn");
                ApplyIfMissing(details, "voterIdNumber", compactDetails, "vn");
                ApplyIfMissing(details, "bankName", compactDetails, "bn");
                ApplyIfMissing(details, "branchName", compactDetails, "brn");
                ApplyIfMissing(details, "accountNumber", compactDetails, "an");
                ApplyIfMissing(details, "ifscCode", compactDetails, "if");

                object childObj;
                if (!details.ContainsKey("children") && compactDetails.TryGetValue("ch", out childObj) && childObj != null)
                {
                    details["children"] = childObj;
                }

                object skillsObj;
                if (!details.ContainsKey("skills") && compactDetails.TryGetValue("sk", out skillsObj) && skillsObj != null)
                {
                    details["skills"] = skillsObj;
                }

                object supportObj;
                if (!details.ContainsKey("requiredSupport") && compactDetails.TryGetValue("rsup", out supportObj) && supportObj != null)
                {
                    details["requiredSupport"] = supportObj;
                }
            }

            snapshot["profile"] = profile;
            snapshot["details"] = details;
        }

        private static void ApplyIfMissing(IDictionary<string, object> target, string targetKey, IDictionary<string, object> source, string sourceKey)
        {
            if (target == null || source == null)
            {
                return;
            }

            object current;
            if (target.TryGetValue(targetKey, out current) && !string.IsNullOrWhiteSpace(CoerceString(current, 500)))
            {
                return;
            }

            object next;
            if (source.TryGetValue(sourceKey, out next) && !string.IsNullOrWhiteSpace(CoerceString(next, 500)))
            {
                target[targetKey] = next;
            }
        }

        private static Dictionary<string, object> BuildCompactProfile(IDictionary<string, object> snapshot)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var source = ExtractObject(snapshot, "profile");

            result["fullAddress"] = CoerceString(ReadValue(source, "fullAddress"), 140);
            result["pinCode"] = CoerceString(ReadValue(source, "pinCode"), 10);
            result["cityAreaPrabhag"] = CoerceString(ReadValue(source, "cityAreaPrabhag"), 60);
            result["officePrabhag"] = CoerceString(ReadValue(source, "officePrabhag"), 40);
            return result;
        }

        private static Dictionary<string, object> BuildOverflowProfile(IDictionary<string, object> snapshot)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var source = ExtractObject(snapshot, "profile");

            result["fullAddress"] = CoerceString(ReadValue(source, "fullAddress"), 90);
            result["pinCode"] = CoerceString(ReadValue(source, "pinCode"), 10);
            return result;
        }

        private static Dictionary<string, object> BuildCompactDetails(IDictionary<string, object> snapshot)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var source = ExtractObject(snapshot, "details");

            result["rationCardNumber"] = CoerceString(ReadValue(source, "rationCardNumber"), 24);
            result["voterIdNumber"] = CoerceString(ReadValue(source, "voterIdNumber"), 24);
            result["bankName"] = CoerceString(ReadValue(source, "bankName"), 40);
            result["branchName"] = CoerceString(ReadValue(source, "branchName"), 40);
            result["accountNumber"] = CoerceString(ReadValue(source, "accountNumber"), 28);
            result["ifscCode"] = CoerceString(ReadValue(source, "ifscCode"), 20);
            result["familyAnnualIncome"] = CoerceString(ReadValue(source, "familyAnnualIncome"), 20);
            result["currentOccupation"] = CoerceString(ReadValue(source, "currentOccupation"), 40);
            return result;
        }

        private static Dictionary<string, object> BuildUltraCompactDetails(IDictionary<string, object> snapshot)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var source = ExtractObject(snapshot, "details");

            result["bankName"] = CoerceString(ReadValue(source, "bankName"), 24);
            result["branchName"] = CoerceString(ReadValue(source, "branchName"), 24);
            result["accountNumber"] = CoerceString(ReadValue(source, "accountNumber"), 20);
            result["ifscCode"] = CoerceString(ReadValue(source, "ifscCode"), 15);
            result["familyAnnualIncome"] = CoerceString(ReadValue(source, "familyAnnualIncome"), 14);
            result["currentOccupation"] = CoerceString(ReadValue(source, "currentOccupation"), 20);
            return result;
        }

        private static IDictionary<string, object> ExtractObject(IDictionary<string, object> source, string key)
        {
            if (source == null)
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            object raw;
            if (!source.TryGetValue(key, out raw) || raw == null)
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            if (raw is JObject)
            {
                return ((JObject)raw).ToObject<Dictionary<string, object>>()
                       ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            var dict = raw as IDictionary<string, object>;
            return dict ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        private static object ReadValue(IDictionary<string, object> dict, string key)
        {
            if (dict == null)
            {
                return null;
            }

            object value;
            return dict.TryGetValue(key, out value) ? value : null;
        }

        private static string CoerceString(object value, int maxLength)
        {
            var text = value == null ? string.Empty : Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim();
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }

        private static SWRDedicatedFields BuildDedicatedFields(SWRRegistrationRequest request)
        {
            var fields = new SWRDedicatedFields();
            if (request == null)
            {
                return fields;
            }

            fields.OfficePrabhag = CoerceString(request.OfficePrabhag, 80);
            fields.FullAddress = CoerceString(request.FullAddress, 250);
            fields.CityAreaPrabhag = CoerceString(request.CityAreaPrabhag, 160);
            fields.PinCode = CoerceString(request.PinCode, 10);
            fields.SingleWomanProofName = CoerceString(request.SingleWomanProofName, 160);
            fields.ProofDocumentNumber = CoerceString(request.ProofDocumentNumber, 60);
            DateTime reqDate;
            if (DateTime.TryParse(request.ProofDocumentDate, out reqDate))
            {
                fields.ProofDocumentDate = reqDate;
            }
            fields.MajorNeedSupport = CoerceString(request.MajorNeedSupport, 500);
            fields.DeclarationPlace = CoerceString(request.DeclarationPlace, 120);
            DateTime declarationDate;
            if (DateTime.TryParse(request.DeclarationDate, out declarationDate))
            {
                fields.DeclarationDate = declarationDate;
            }
            fields.ApplicantSignatureName = CoerceString(request.ApplicantSignatureName, 160);
            fields.RequiredSupportSummary = CoerceString(request.RequiredSupportSummary, 500);
            fields.BankName = CoerceString(request.BankName, 120);
            fields.BranchName = CoerceString(request.BranchName, 120);
            fields.AccountNumber = CoerceString(request.AccountNumber, 32);
            fields.IfscCode = CoerceString(request.IfscCode, 20);
            fields.AccountHolderName = CoerceString(request.AccountHolderName, 160);
            fields.FamilyMemberCount = CoerceString(request.FamilyMemberCount, 6);
            fields.RationCardNumber = CoerceString(request.RationCardNumber, 50);
            fields.VoterIdNumber = CoerceString(request.VoterIdNumber, 50);
            fields.RationCardType = CoerceString(request.RationCardType, 20);
            fields.RationCardTypeOther = CoerceString(request.RationCardTypeOther, 120);
            fields.Email = CoerceString(request.Email, 120);
            fields.LivelihoodSource = CoerceString(request.LivelihoodSource, 120);
            fields.IsDivyangSingleWoman = CoerceString(request.IsDivyangSingleWoman, 10);
            fields.ResidenceType = CoerceString(request.ResidenceType, 30);
            fields.LandStatus = CoerceString(request.LandStatus, 30);
            fields.LandAreaAcre = CoerceString(request.LandAreaAcre, 30);
            fields.IsBpl = CoerceString(request.IsBpl, 10);
            fields.TotalChildren = CoerceString(request.TotalChildren, 6);
            fields.BoysCount = CoerceString(request.BoysCount, 6);
            fields.GirlsCount = CoerceString(request.GirlsCount, 6);
            fields.ChildrenBelow18Count = CoerceString(request.ChildrenBelow18Count, 6);
            fields.ChildrenInEducation = CoerceString(request.ChildrenInEducation, 10);
            fields.GetsBalSangopanScholarship = CoerceString(request.GetsBalSangopanScholarship, 10);
            fields.Education = CoerceString(request.Education, 200);
            fields.EducationQualification = CoerceString(request.EducationQualification, 200);
            fields.CurrentOccupation = CoerceString(request.CurrentOccupation, 500);
            fields.MonthlyIncome = CoerceString(request.MonthlyIncome, 24);
            fields.WomanMonthlyIncome = CoerceString(request.WomanMonthlyIncome, 24);
            fields.FamilyAnnualIncome = CoerceString(request.FamilyAnnualIncome, 24);
            fields.IsSelfHelpGroupMember = CoerceString(request.IsSelfHelpGroupMember, 10);
            fields.RequiredSupportOther = CoerceString(request.RequiredSupportOther, 1000);
            fields.IsAvailingGovtScheme = CoerceString(request.IsAvailingGovtScheme, 10);
            fields.GovtSchemeName = CoerceString(request.GovtSchemeName, 500);
            fields.WantsSelfEmployment = CoerceString(request.WantsSelfEmployment, 10);
            fields.ExpectedBusinessType = CoerceString(request.ExpectedBusinessType, 500);
            fields.SkillsJson = CoerceString(request.SkillsJson, 2000000);
            fields.RequiredSupportJson = CoerceString(request.RequiredSupportJson, 2000000);

            if (string.IsNullOrWhiteSpace(request.OtherInfo))
            {
                return fields;
            }

            try
            {
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.OtherInfo)
                          ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                var profile = ExtractObject(obj, "profile");
                var details = ExtractObject(obj, "details");
                var compactProfile = ExtractObject(obj, "p");
                var compactDetails = ExtractObject(obj, "d");

                if (string.IsNullOrWhiteSpace(fields.OfficePrabhag)) fields.OfficePrabhag = CoerceString(ReadValue(profile, "officePrabhag"), 80);
                if (string.IsNullOrWhiteSpace(fields.OfficePrabhag)) fields.OfficePrabhag = CoerceString(ReadValue(compactProfile, "o"), 80);

                if (string.IsNullOrWhiteSpace(fields.FullAddress)) fields.FullAddress = CoerceString(ReadValue(profile, "fullAddress"), 250);
                if (string.IsNullOrWhiteSpace(fields.FullAddress)) fields.FullAddress = CoerceString(ReadValue(compactProfile, "a"), 250);

                if (string.IsNullOrWhiteSpace(fields.CityAreaPrabhag)) fields.CityAreaPrabhag = CoerceString(ReadValue(profile, "cityAreaPrabhag"), 160);
                if (string.IsNullOrWhiteSpace(fields.CityAreaPrabhag)) fields.CityAreaPrabhag = CoerceString(ReadValue(compactProfile, "c"), 160);

                if (string.IsNullOrWhiteSpace(fields.PinCode)) fields.PinCode = CoerceString(ReadValue(profile, "pinCode"), 10);
                if (string.IsNullOrWhiteSpace(fields.PinCode)) fields.PinCode = CoerceString(ReadValue(compactProfile, "p"), 10);

                if (string.IsNullOrWhiteSpace(fields.SingleWomanProofName)) fields.SingleWomanProofName = CoerceString(ReadValue(details, "singleWomanProofName"), 160);
                if (string.IsNullOrWhiteSpace(fields.SingleWomanProofName)) fields.SingleWomanProofName = CoerceString(ReadValue(compactDetails, "spn"), 160);

                if (string.IsNullOrWhiteSpace(fields.ProofDocumentNumber)) fields.ProofDocumentNumber = CoerceString(ReadValue(details, "proofDocumentNumber"), 60);
                if (string.IsNullOrWhiteSpace(fields.ProofDocumentNumber)) fields.ProofDocumentNumber = CoerceString(ReadValue(compactDetails, "pn"), 60);

                var proofDate = CoerceString(ReadValue(details, "proofDocumentDate"), 20);
                if (string.IsNullOrWhiteSpace(proofDate)) proofDate = CoerceString(ReadValue(compactDetails, "pd"), 20);
                DateTime parsedDate;
                if (!fields.ProofDocumentDate.HasValue)
                {
                    fields.ProofDocumentDate = DateTime.TryParse(proofDate, out parsedDate) ? parsedDate : (DateTime?)null;
                }

                if (string.IsNullOrWhiteSpace(fields.MajorNeedSupport)) fields.MajorNeedSupport = CoerceString(ReadValue(details, "majorNeedSupport"), 500);
                if (string.IsNullOrWhiteSpace(fields.MajorNeedSupport)) fields.MajorNeedSupport = CoerceString(ReadValue(compactDetails, "mns"), 500);

                if (string.IsNullOrWhiteSpace(fields.DeclarationPlace)) fields.DeclarationPlace = CoerceString(ReadValue(details, "declarationPlace"), 120);
                if (string.IsNullOrWhiteSpace(fields.DeclarationPlace)) fields.DeclarationPlace = CoerceString(ReadValue(compactDetails, "dp"), 120);

                if (!fields.DeclarationDate.HasValue)
                {
                    var declarationDateRaw = CoerceString(ReadValue(details, "declarationDate"), 20);
                    if (string.IsNullOrWhiteSpace(declarationDateRaw)) declarationDateRaw = CoerceString(ReadValue(compactDetails, "dd"), 20);
                    DateTime parsedDeclarationDate;
                    fields.DeclarationDate = DateTime.TryParse(declarationDateRaw, out parsedDeclarationDate) ? parsedDeclarationDate : (DateTime?)null;
                }

                if (string.IsNullOrWhiteSpace(fields.ApplicantSignatureName)) fields.ApplicantSignatureName = CoerceString(ReadValue(details, "applicantSignatureName"), 160);
                if (string.IsNullOrWhiteSpace(fields.ApplicantSignatureName)) fields.ApplicantSignatureName = CoerceString(ReadValue(compactDetails, "asn"), 160);

                if (string.IsNullOrWhiteSpace(fields.RequiredSupportSummary))
                {
                    object requiredSupportRaw;
                    if (compactDetails.TryGetValue("rsup", out requiredSupportRaw) && requiredSupportRaw != null)
                    {
                        var array = requiredSupportRaw as JArray;
                        if (array != null)
                        {
                            var parts = new List<string>();
                            foreach (var item in array)
                            {
                                var text = CoerceString(item, 40);
                                if (!string.IsNullOrWhiteSpace(text)) parts.Add(text);
                            }
                            fields.RequiredSupportSummary = CoerceString(string.Join("|", parts.ToArray()), 500);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(fields.BankName)) fields.BankName = CoerceString(ReadValue(details, "bankName"), 120);
                if (string.IsNullOrWhiteSpace(fields.BankName)) fields.BankName = CoerceString(ReadValue(compactDetails, "bn"), 120);

                if (string.IsNullOrWhiteSpace(fields.BranchName)) fields.BranchName = CoerceString(ReadValue(details, "branchName"), 120);
                if (string.IsNullOrWhiteSpace(fields.BranchName)) fields.BranchName = CoerceString(ReadValue(compactDetails, "brn"), 120);

                if (string.IsNullOrWhiteSpace(fields.FamilyMemberCount)) fields.FamilyMemberCount = CoerceString(ReadValue(details, "familyMemberCount"), 6);
                if (string.IsNullOrWhiteSpace(fields.FamilyMemberCount)) fields.FamilyMemberCount = CoerceString(ReadValue(compactDetails, "fm"), 6);

                if (string.IsNullOrWhiteSpace(fields.RationCardNumber)) fields.RationCardNumber = CoerceString(ReadValue(details, "rationCardNumber"), 50);
                if (string.IsNullOrWhiteSpace(fields.RationCardNumber)) fields.RationCardNumber = CoerceString(ReadValue(compactDetails, "rn"), 50);

                if (string.IsNullOrWhiteSpace(fields.VoterIdNumber)) fields.VoterIdNumber = CoerceString(ReadValue(details, "voterIdNumber"), 50);
                if (string.IsNullOrWhiteSpace(fields.VoterIdNumber)) fields.VoterIdNumber = CoerceString(ReadValue(compactDetails, "vn"), 50);

                if (string.IsNullOrWhiteSpace(fields.RationCardType)) fields.RationCardType = CoerceString(ReadValue(details, "rationCardType"), 20);
                if (string.IsNullOrWhiteSpace(fields.RationCardType)) fields.RationCardType = CoerceString(ReadValue(compactDetails, "rt"), 20);

                if (string.IsNullOrWhiteSpace(fields.RationCardTypeOther)) fields.RationCardTypeOther = CoerceString(ReadValue(details, "rationCardTypeOther"), 120);
                if (string.IsNullOrWhiteSpace(fields.RationCardTypeOther)) fields.RationCardTypeOther = CoerceString(ReadValue(compactDetails, "rto"), 120);

                if (string.IsNullOrWhiteSpace(fields.Email)) fields.Email = CoerceString(ReadValue(details, "email"), 120);
                if (string.IsNullOrWhiteSpace(fields.Email)) fields.Email = CoerceString(ReadValue(compactDetails, "e"), 120);

                if (string.IsNullOrWhiteSpace(fields.LivelihoodSource)) fields.LivelihoodSource = CoerceString(ReadValue(details, "livelihoodSource"), 120);
                if (string.IsNullOrWhiteSpace(fields.LivelihoodSource)) fields.LivelihoodSource = CoerceString(ReadValue(compactDetails, "ls"), 120);

                if (string.IsNullOrWhiteSpace(fields.IsDivyangSingleWoman)) fields.IsDivyangSingleWoman = CoerceString(ReadValue(details, "isDivyangSingleWoman"), 10);
                if (string.IsNullOrWhiteSpace(fields.IsDivyangSingleWoman)) fields.IsDivyangSingleWoman = CoerceString(ReadValue(compactDetails, "dw"), 10);

                if (string.IsNullOrWhiteSpace(fields.ResidenceType)) fields.ResidenceType = CoerceString(ReadValue(details, "residenceType"), 30);
                if (string.IsNullOrWhiteSpace(fields.ResidenceType)) fields.ResidenceType = CoerceString(ReadValue(compactDetails, "rs"), 30);

                if (string.IsNullOrWhiteSpace(fields.LandStatus)) fields.LandStatus = CoerceString(ReadValue(details, "landStatus"), 30);
                if (string.IsNullOrWhiteSpace(fields.LandStatus)) fields.LandStatus = CoerceString(ReadValue(compactDetails, "lds"), 30);

                if (string.IsNullOrWhiteSpace(fields.LandAreaAcre)) fields.LandAreaAcre = CoerceString(ReadValue(details, "landAreaAcre"), 30);
                if (string.IsNullOrWhiteSpace(fields.LandAreaAcre)) fields.LandAreaAcre = CoerceString(ReadValue(compactDetails, "la"), 30);

                if (string.IsNullOrWhiteSpace(fields.IsBpl)) fields.IsBpl = CoerceString(ReadValue(details, "isBpl"), 10);
                if (string.IsNullOrWhiteSpace(fields.IsBpl)) fields.IsBpl = CoerceString(ReadValue(compactDetails, "bpl"), 10);

                if (string.IsNullOrWhiteSpace(fields.TotalChildren)) fields.TotalChildren = CoerceString(ReadValue(details, "totalChildren"), 6);
                if (string.IsNullOrWhiteSpace(fields.TotalChildren)) fields.TotalChildren = CoerceString(ReadValue(compactDetails, "tc"), 6);

                if (string.IsNullOrWhiteSpace(fields.BoysCount)) fields.BoysCount = CoerceString(ReadValue(details, "boysCount"), 6);
                if (string.IsNullOrWhiteSpace(fields.BoysCount)) fields.BoysCount = CoerceString(ReadValue(compactDetails, "bc"), 6);

                if (string.IsNullOrWhiteSpace(fields.GirlsCount)) fields.GirlsCount = CoerceString(ReadValue(details, "girlsCount"), 6);
                if (string.IsNullOrWhiteSpace(fields.GirlsCount)) fields.GirlsCount = CoerceString(ReadValue(compactDetails, "gc"), 6);

                if (string.IsNullOrWhiteSpace(fields.ChildrenBelow18Count)) fields.ChildrenBelow18Count = CoerceString(ReadValue(details, "childrenBelow18Count"), 6);
                if (string.IsNullOrWhiteSpace(fields.ChildrenBelow18Count)) fields.ChildrenBelow18Count = CoerceString(ReadValue(compactDetails, "cb18"), 6);

                if (string.IsNullOrWhiteSpace(fields.ChildrenInEducation)) fields.ChildrenInEducation = CoerceString(ReadValue(details, "childrenInEducation"), 10);
                if (string.IsNullOrWhiteSpace(fields.ChildrenInEducation)) fields.ChildrenInEducation = CoerceString(ReadValue(compactDetails, "cedu"), 10);

                if (string.IsNullOrWhiteSpace(fields.GetsBalSangopanScholarship)) fields.GetsBalSangopanScholarship = CoerceString(ReadValue(details, "getsBalSangopanScholarship"), 10);
                if (string.IsNullOrWhiteSpace(fields.GetsBalSangopanScholarship)) fields.GetsBalSangopanScholarship = CoerceString(ReadValue(compactDetails, "bss"), 10);

                if (string.IsNullOrWhiteSpace(fields.Education)) fields.Education = CoerceString(ReadValue(details, "education"), 200);
                if (string.IsNullOrWhiteSpace(fields.Education)) fields.Education = CoerceString(ReadValue(compactDetails, "ed"), 200);

                if (string.IsNullOrWhiteSpace(fields.EducationQualification)) fields.EducationQualification = CoerceString(ReadValue(details, "educationQualification"), 200);
                if (string.IsNullOrWhiteSpace(fields.EducationQualification)) fields.EducationQualification = CoerceString(ReadValue(compactDetails, "eq"), 200);

                if (string.IsNullOrWhiteSpace(fields.CurrentOccupation)) fields.CurrentOccupation = CoerceString(ReadValue(details, "currentOccupation"), 500);
                if (string.IsNullOrWhiteSpace(fields.CurrentOccupation)) fields.CurrentOccupation = CoerceString(ReadValue(compactDetails, "oc"), 500);

                if (string.IsNullOrWhiteSpace(fields.MonthlyIncome)) fields.MonthlyIncome = CoerceString(ReadValue(details, "monthlyIncome"), 24);
                if (string.IsNullOrWhiteSpace(fields.MonthlyIncome)) fields.MonthlyIncome = CoerceString(ReadValue(compactDetails, "mi"), 24);

                if (string.IsNullOrWhiteSpace(fields.WomanMonthlyIncome)) fields.WomanMonthlyIncome = CoerceString(ReadValue(details, "womanMonthlyIncome"), 24);
                if (string.IsNullOrWhiteSpace(fields.WomanMonthlyIncome)) fields.WomanMonthlyIncome = CoerceString(ReadValue(compactDetails, "wmi"), 24);

                if (string.IsNullOrWhiteSpace(fields.FamilyAnnualIncome)) fields.FamilyAnnualIncome = CoerceString(ReadValue(details, "familyAnnualIncome"), 24);
                if (string.IsNullOrWhiteSpace(fields.FamilyAnnualIncome)) fields.FamilyAnnualIncome = CoerceString(ReadValue(compactDetails, "fai"), 24);

                if (string.IsNullOrWhiteSpace(fields.IsSelfHelpGroupMember)) fields.IsSelfHelpGroupMember = CoerceString(ReadValue(details, "isSelfHelpGroupMember"), 10);
                if (string.IsNullOrWhiteSpace(fields.IsSelfHelpGroupMember)) fields.IsSelfHelpGroupMember = CoerceString(ReadValue(compactDetails, "shg"), 10);

                if (string.IsNullOrWhiteSpace(fields.RequiredSupportOther)) fields.RequiredSupportOther = CoerceString(ReadValue(details, "requiredSupportOther"), 1000);
                if (string.IsNullOrWhiteSpace(fields.RequiredSupportOther)) fields.RequiredSupportOther = CoerceString(ReadValue(compactDetails, "rso"), 1000);

                if (string.IsNullOrWhiteSpace(fields.IsAvailingGovtScheme)) fields.IsAvailingGovtScheme = CoerceString(ReadValue(details, "isAvailingGovtScheme"), 10);
                if (string.IsNullOrWhiteSpace(fields.IsAvailingGovtScheme)) fields.IsAvailingGovtScheme = CoerceString(ReadValue(compactDetails, "ags"), 10);

                if (string.IsNullOrWhiteSpace(fields.GovtSchemeName)) fields.GovtSchemeName = CoerceString(ReadValue(details, "govtSchemeName"), 500);
                if (string.IsNullOrWhiteSpace(fields.GovtSchemeName)) fields.GovtSchemeName = CoerceString(ReadValue(compactDetails, "gsn"), 500);

                if (string.IsNullOrWhiteSpace(fields.WantsSelfEmployment)) fields.WantsSelfEmployment = CoerceString(ReadValue(details, "wantsSelfEmployment"), 10);
                if (string.IsNullOrWhiteSpace(fields.WantsSelfEmployment)) fields.WantsSelfEmployment = CoerceString(ReadValue(compactDetails, "wse"), 10);

                if (string.IsNullOrWhiteSpace(fields.ExpectedBusinessType)) fields.ExpectedBusinessType = CoerceString(ReadValue(details, "expectedBusinessType"), 500);
                if (string.IsNullOrWhiteSpace(fields.ExpectedBusinessType)) fields.ExpectedBusinessType = CoerceString(ReadValue(compactDetails, "eb"), 500);

                if (string.IsNullOrWhiteSpace(fields.AccountNumber)) fields.AccountNumber = CoerceString(ReadValue(details, "accountNumber"), 32);
                if (string.IsNullOrWhiteSpace(fields.AccountNumber)) fields.AccountNumber = CoerceString(ReadValue(compactDetails, "an"), 32);

                if (string.IsNullOrWhiteSpace(fields.IfscCode)) fields.IfscCode = CoerceString(ReadValue(details, "ifscCode"), 20);
                if (string.IsNullOrWhiteSpace(fields.IfscCode)) fields.IfscCode = CoerceString(ReadValue(compactDetails, "if"), 20);

                if (string.IsNullOrWhiteSpace(fields.AccountHolderName)) fields.AccountHolderName = CoerceString(ReadValue(details, "accountHolderName"), 160);
                if (string.IsNullOrWhiteSpace(fields.AccountHolderName)) fields.AccountHolderName = CoerceString(ReadValue(compactDetails, "ah"), 160);

                var skills = new List<string>();
                object skillsRaw;
                if (details.TryGetValue("skills", out skillsRaw) && skillsRaw is JArray)
                {
                    foreach (var item in (JArray)skillsRaw)
                    {
                        var text = CoerceString(item, 40);
                        if (!string.IsNullOrWhiteSpace(text)) skills.Add(text);
                    }
                }
                if (skills.Count == 0 && compactDetails.TryGetValue("sk", out skillsRaw) && skillsRaw is JArray)
                {
                    foreach (var item in (JArray)skillsRaw)
                    {
                        var text = CoerceString(item, 40);
                        if (!string.IsNullOrWhiteSpace(text)) skills.Add(text);
                    }
                }
                if (skills.Count > 0)
                {
                    fields.SkillsSummary = CoerceString(string.Join("|", skills.ToArray()), 500);
                }

                object childrenRaw;
                JArray children = null;
                if (details.TryGetValue("children", out childrenRaw) && childrenRaw is JArray)
                {
                    children = (JArray)childrenRaw;
                }
                else if (compactDetails.TryGetValue("ch", out childrenRaw) && childrenRaw is JArray)
                {
                    children = (JArray)childrenRaw;
                }

                if (children != null)
                {
                    var normalizedChildren = new JArray();
                    foreach (var token in children)
                    {
                        var childObj = token as JObject;
                        if (childObj == null) continue;

                        var childDict = childObj.ToObject<Dictionary<string, object>>()
                            ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                        var item = new JObject();
                        item["n"] = CoerceString(ReadValue(childDict, "n") ?? ReadValue(childDict, "childName"), 30);
                        item["a"] = CoerceString(ReadValue(childDict, "a") ?? ReadValue(childDict, "age"), 3);
                        item["s"] = CoerceString(ReadValue(childDict, "s") ?? ReadValue(childDict, "school"), 30);
                        item["d"] = CoerceString(ReadValue(childDict, "d") ?? ReadValue(childDict, "isDivyang"), 6);
                        normalizedChildren.Add(item);
                        if (normalizedChildren.Count >= 4) break;
                    }

                    if (normalizedChildren.Count > 0)
                    {
                        fields.ChildrenJson = CoerceString(normalizedChildren.ToString(Formatting.None), 2000000);
                    }
                }
            }
            catch
            {
            }

            return fields;
        }

        private static void HydrateOtherInfoFromDedicatedColumns(IDictionary<string, object> row)
        {
            if (row == null)
            {
                return;
            }

            var rawOtherInfo = CoerceString(ReadValue(row, "other_info"), 2000000);
            Dictionary<string, object> other;
            try
            {
                other = string.IsNullOrWhiteSpace(rawOtherInfo)
                    ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    : JsonConvert.DeserializeObject<Dictionary<string, object>>(rawOtherInfo)
                      ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                other = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            var profile = ToCaseInsensitiveDictionary(ExtractObject(other, "profile"));
            var compactProfile = ToCaseInsensitiveDictionary(ExtractObject(other, "p"));
            var details = ToCaseInsensitiveDictionary(ExtractObject(other, "details"));
            var compactDetails = ToCaseInsensitiveDictionary(ExtractObject(other, "d"));

            var officePrabhag = CoerceString(ReadValue(row, "office_prabhag"), 80);
            var fullAddress = CoerceString(ReadValue(row, "full_address"), 250);
            var cityAreaPrabhag = CoerceString(ReadValue(row, "city_area_prabhag"), 160);
            var pinCode = CoerceString(ReadValue(row, "pin_code"), 10);
            var proofName = CoerceString(ReadValue(row, "single_woman_proof_name"), 160);
            var proofNo = CoerceString(ReadValue(row, "proof_document_number"), 60);
            var proofDateRaw = ReadValue(row, "proof_document_date");
            var majorNeedSupport = CoerceString(ReadValue(row, "major_need_support"), 500);
            var declarationPlace = CoerceString(ReadValue(row, "declaration_place"), 120);
            var declarationDateRaw = ReadValue(row, "declaration_date");
            var applicantSignatureName = CoerceString(ReadValue(row, "applicant_signature_name"), 160);
            var requiredSupportSummary = CoerceString(ReadValue(row, "required_support_summary"), 500);
            var skillsSummary = CoerceString(ReadValue(row, "skills_summary"), 500);
            var childrenJson = CoerceString(ReadValue(row, "children_json"), 2000000);
            var bankName = CoerceString(ReadValue(row, "bank_name"), 120);
            var branchName = CoerceString(ReadValue(row, "branch_name"), 120);
            var accountNumber = CoerceString(ReadValue(row, "account_number"), 32);
            var ifscCode = CoerceString(ReadValue(row, "ifsc_code"), 20);
            var accountHolderName = CoerceString(ReadValue(row, "account_holder_name"), 160);
            var familyMemberCount = CoerceString(ReadValue(row, "family_member_count"), 6);
            var rationCardNumber = CoerceString(ReadValue(row, "ration_card_number"), 50);
            var voterIdNumber = CoerceString(ReadValue(row, "voter_id_number"), 50);
            var rationCardType = CoerceString(ReadValue(row, "ration_card_type"), 20);
            var rationCardTypeOther = CoerceString(ReadValue(row, "ration_card_type_other"), 120);
            var email = CoerceString(ReadValue(row, "email"), 120);
            var livelihoodSource = CoerceString(ReadValue(row, "livelihood_source"), 120);
            var isDivyangSingleWoman = CoerceString(ReadValue(row, "is_divyang_single_woman"), 10);
            var residenceType = CoerceString(ReadValue(row, "residence_type"), 30);
            var landStatus = CoerceString(ReadValue(row, "land_status"), 30);
            var landAreaAcre = CoerceString(ReadValue(row, "land_area_acre"), 30);
            var isBpl = CoerceString(ReadValue(row, "is_bpl"), 10);
            var totalChildren = CoerceString(ReadValue(row, "total_children"), 20);
            var boysCount = CoerceString(ReadValue(row, "boys_count"), 20);
            var girlsCount = CoerceString(ReadValue(row, "girls_count"), 20);
            var childrenBelow18Count = CoerceString(ReadValue(row, "children_below18_count"), 20);
            var childrenInEducation = CoerceString(ReadValue(row, "children_in_education"), 10);
            var getsBalSangopanScholarship = CoerceString(ReadValue(row, "gets_bal_sangopan_scholarship"), 10);
            var education = CoerceString(ReadValue(row, "education"), 200);
            var educationQualification = CoerceString(ReadValue(row, "education_qualification"), 200);
            var currentOccupation = CoerceString(ReadValue(row, "current_occupation"), 500);
            var monthlyIncome = CoerceString(ReadValue(row, "monthly_income"), 24);
            var womanMonthlyIncome = CoerceString(ReadValue(row, "woman_monthly_income"), 24);
            var familyAnnualIncome = CoerceString(ReadValue(row, "family_annual_income"), 24);
            var isSelfHelpGroupMember = CoerceString(ReadValue(row, "is_self_help_group_member"), 10);
            var requiredSupportOther = CoerceString(ReadValue(row, "required_support_other"), 1000);
            var isAvailingGovtScheme = CoerceString(ReadValue(row, "is_availing_govt_scheme"), 10);
            var govtSchemeName = CoerceString(ReadValue(row, "govt_scheme_name"), 500);
            var wantsSelfEmployment = CoerceString(ReadValue(row, "wants_self_employment"), 10);
            var expectedBusinessType = CoerceString(ReadValue(row, "expected_business_type"), 500);
            var skillsJson = CoerceString(ReadValue(row, "skills_json"), 2000000);
            var requiredSupportJson = CoerceString(ReadValue(row, "required_support_json"), 2000000);

            if (!string.IsNullOrWhiteSpace(officePrabhag))
            {
                profile["officePrabhag"] = officePrabhag;
                compactProfile["o"] = officePrabhag;
            }
            if (!string.IsNullOrWhiteSpace(fullAddress))
            {
                profile["fullAddress"] = fullAddress;
                compactProfile["a"] = fullAddress;
            }
            if (!string.IsNullOrWhiteSpace(cityAreaPrabhag))
            {
                profile["cityAreaPrabhag"] = cityAreaPrabhag;
                compactProfile["c"] = cityAreaPrabhag;
            }
            if (!string.IsNullOrWhiteSpace(pinCode))
            {
                profile["pinCode"] = pinCode;
                compactProfile["p"] = pinCode;
            }
            if (!string.IsNullOrWhiteSpace(proofName))
            {
                details["singleWomanProofName"] = proofName;
                compactDetails["spn"] = proofName;
            }
            if (!string.IsNullOrWhiteSpace(proofNo))
            {
                details["proofDocumentNumber"] = proofNo;
                compactDetails["pn"] = proofNo;
            }
            if (proofDateRaw != null)
            {
                var proofDate = CoerceDateIso(proofDateRaw);
                if (!string.IsNullOrWhiteSpace(proofDate))
                {
                    details["proofDocumentDate"] = proofDate;
                    compactDetails["pd"] = proofDate;
                }
            }
            if (!string.IsNullOrWhiteSpace(majorNeedSupport))
            {
                details["majorNeedSupport"] = majorNeedSupport;
                compactDetails["mns"] = majorNeedSupport;
            }
            if (!string.IsNullOrWhiteSpace(declarationPlace))
            {
                details["declarationPlace"] = declarationPlace;
                compactDetails["dp"] = declarationPlace;
            }
            if (declarationDateRaw != null)
            {
                var declarationDate = CoerceDateIso(declarationDateRaw);
                if (!string.IsNullOrWhiteSpace(declarationDate))
                {
                    details["declarationDate"] = declarationDate;
                    compactDetails["dd"] = declarationDate;
                }
            }
            if (!string.IsNullOrWhiteSpace(applicantSignatureName))
            {
                details["applicantSignatureName"] = applicantSignatureName;
                compactDetails["asn"] = applicantSignatureName;
            }
            if (!string.IsNullOrWhiteSpace(requiredSupportSummary))
            {
                var requiredSupport = new List<string>();
                foreach (var token in requiredSupportSummary.Split('|'))
                {
                    var value = CoerceString(token, 40);
                    if (!string.IsNullOrWhiteSpace(value)) requiredSupport.Add(value);
                }
                if (requiredSupport.Count > 0)
                {
                    details["requiredSupport"] = requiredSupport;
                    compactDetails["rsup"] = requiredSupport;
                }
            }

            if (!string.IsNullOrWhiteSpace(bankName))
            {
                details["bankName"] = bankName;
                compactDetails["bn"] = bankName;
            }
            if (!string.IsNullOrWhiteSpace(branchName))
            {
                details["branchName"] = branchName;
                compactDetails["brn"] = branchName;
            }
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                details["accountNumber"] = accountNumber;
                compactDetails["an"] = accountNumber;
            }
            if (!string.IsNullOrWhiteSpace(ifscCode))
            {
                details["ifscCode"] = ifscCode;
                compactDetails["if"] = ifscCode;
            }
            if (!string.IsNullOrWhiteSpace(accountHolderName))
            {
                details["accountHolderName"] = accountHolderName;
                compactDetails["ah"] = accountHolderName;
            }
            if (!string.IsNullOrWhiteSpace(familyMemberCount))
            {
                details["familyMemberCount"] = familyMemberCount;
                compactDetails["fm"] = familyMemberCount;
            }

            if (!string.IsNullOrWhiteSpace(rationCardNumber))
            {
                details["rationCardNumber"] = rationCardNumber;
                compactDetails["rn"] = rationCardNumber;
            }
            if (!string.IsNullOrWhiteSpace(voterIdNumber))
            {
                details["voterIdNumber"] = voterIdNumber;
                compactDetails["vn"] = voterIdNumber;
            }
            if (!string.IsNullOrWhiteSpace(rationCardType))
            {
                details["rationCardType"] = rationCardType;
                compactDetails["rt"] = rationCardType;
            }
            if (!string.IsNullOrWhiteSpace(rationCardTypeOther))
            {
                details["rationCardTypeOther"] = rationCardTypeOther;
                compactDetails["rto"] = rationCardTypeOther;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                details["email"] = email;
                compactDetails["e"] = email;
            }
            if (!string.IsNullOrWhiteSpace(livelihoodSource))
            {
                details["livelihoodSource"] = livelihoodSource;
                compactDetails["ls"] = livelihoodSource;
            }
            if (!string.IsNullOrWhiteSpace(isDivyangSingleWoman))
            {
                details["isDivyangSingleWoman"] = isDivyangSingleWoman;
                compactDetails["dw"] = isDivyangSingleWoman;
            }
            if (!string.IsNullOrWhiteSpace(residenceType))
            {
                details["residenceType"] = residenceType;
                compactDetails["rs"] = residenceType;
            }
            if (!string.IsNullOrWhiteSpace(landStatus))
            {
                details["landStatus"] = landStatus;
                compactDetails["lds"] = landStatus;
            }
            if (!string.IsNullOrWhiteSpace(landAreaAcre))
            {
                details["landAreaAcre"] = landAreaAcre;
                compactDetails["la"] = landAreaAcre;
            }
            if (!string.IsNullOrWhiteSpace(isBpl))
            {
                details["isBpl"] = isBpl;
                compactDetails["bpl"] = isBpl;
            }
            if (!string.IsNullOrWhiteSpace(totalChildren))
            {
                details["totalChildren"] = totalChildren;
                compactDetails["tc"] = totalChildren;
            }
            if (!string.IsNullOrWhiteSpace(boysCount))
            {
                details["boysCount"] = boysCount;
                compactDetails["bc"] = boysCount;
            }
            if (!string.IsNullOrWhiteSpace(girlsCount))
            {
                details["girlsCount"] = girlsCount;
                compactDetails["gc"] = girlsCount;
            }
            if (!string.IsNullOrWhiteSpace(childrenBelow18Count))
            {
                details["childrenBelow18Count"] = childrenBelow18Count;
                compactDetails["cb18"] = childrenBelow18Count;
            }
            if (!string.IsNullOrWhiteSpace(childrenInEducation))
            {
                details["childrenInEducation"] = childrenInEducation;
                compactDetails["cedu"] = childrenInEducation;
            }
            if (!string.IsNullOrWhiteSpace(getsBalSangopanScholarship))
            {
                details["getsBalSangopanScholarship"] = getsBalSangopanScholarship;
                compactDetails["bss"] = getsBalSangopanScholarship;
            }
            if (!string.IsNullOrWhiteSpace(education))
            {
                details["education"] = education;
                compactDetails["ed"] = education;
            }
            if (!string.IsNullOrWhiteSpace(educationQualification))
            {
                details["educationQualification"] = educationQualification;
                compactDetails["eq"] = educationQualification;
            }
            if (!string.IsNullOrWhiteSpace(currentOccupation))
            {
                details["currentOccupation"] = currentOccupation;
                compactDetails["oc"] = currentOccupation;
            }
            if (!string.IsNullOrWhiteSpace(monthlyIncome))
            {
                details["monthlyIncome"] = monthlyIncome;
                compactDetails["mi"] = monthlyIncome;
            }
            if (!string.IsNullOrWhiteSpace(womanMonthlyIncome))
            {
                details["womanMonthlyIncome"] = womanMonthlyIncome;
                compactDetails["wmi"] = womanMonthlyIncome;
            }
            if (!string.IsNullOrWhiteSpace(familyAnnualIncome))
            {
                details["familyAnnualIncome"] = familyAnnualIncome;
                compactDetails["fai"] = familyAnnualIncome;
            }
            if (!string.IsNullOrWhiteSpace(isSelfHelpGroupMember))
            {
                details["isSelfHelpGroupMember"] = isSelfHelpGroupMember;
                compactDetails["shg"] = isSelfHelpGroupMember;
            }
            if (!string.IsNullOrWhiteSpace(requiredSupportOther))
            {
                details["requiredSupportOther"] = requiredSupportOther;
                compactDetails["rso"] = requiredSupportOther;
            }
            if (!string.IsNullOrWhiteSpace(isAvailingGovtScheme))
            {
                details["isAvailingGovtScheme"] = isAvailingGovtScheme;
                compactDetails["ags"] = isAvailingGovtScheme;
            }
            if (!string.IsNullOrWhiteSpace(govtSchemeName))
            {
                details["govtSchemeName"] = govtSchemeName;
                compactDetails["gsn"] = govtSchemeName;
            }
            if (!string.IsNullOrWhiteSpace(wantsSelfEmployment))
            {
                details["wantsSelfEmployment"] = wantsSelfEmployment;
                compactDetails["wse"] = wantsSelfEmployment;
            }
            if (!string.IsNullOrWhiteSpace(expectedBusinessType))
            {
                details["expectedBusinessType"] = expectedBusinessType;
                compactDetails["eb"] = expectedBusinessType;
            }

            if (!string.IsNullOrWhiteSpace(skillsJson))
            {
                try
                {
                    var skills = JsonConvert.DeserializeObject<JArray>(skillsJson);
                    if (skills != null)
                    {
                        details["skills"] = skills;
                        compactDetails["sk"] = skills;
                    }
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(requiredSupportJson))
            {
                try
                {
                    var requiredSupport = JsonConvert.DeserializeObject<JArray>(requiredSupportJson);
                    if (requiredSupport != null)
                    {
                        details["requiredSupport"] = requiredSupport;
                        compactDetails["rsup"] = requiredSupport;
                    }
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(skillsSummary))
            {
                var skills = new List<string>();
                foreach (var token in skillsSummary.Split('|'))
                {
                    var value = CoerceString(token, 40);
                    if (!string.IsNullOrWhiteSpace(value)) skills.Add(value);
                }
                if (skills.Count > 0)
                {
                    details["skills"] = skills;
                    compactDetails["sk"] = skills;
                }
            }

            if (!string.IsNullOrWhiteSpace(childrenJson))
            {
                try
                {
                    var children = JsonConvert.DeserializeObject<JArray>(childrenJson);
                    if (children != null && children.Count > 0)
                    {
                        details["children"] = children;
                        compactDetails["ch"] = children;
                    }
                }
                catch
                {
                }
            }

            if (profile.Count > 0) other["profile"] = profile;
            if (compactProfile.Count > 0) other["p"] = compactProfile;
            if (details.Count > 0) other["details"] = details;
            if (compactDetails.Count > 0) other["d"] = compactDetails;

            var serialized = JsonConvert.SerializeObject(other);
            row["other_info"] = serialized;
            row["otherInfo"] = serialized;
        }

        private void HydrateSearchResponse(object data)
        {
            if (data == null)
            {
                return;
            }

            var rows = data as List<Dictionary<string, object>>;
            if (rows != null)
            {
                var maxEnrichmentRows = rows.Count > 40 ? 40 : rows.Count;
                for (var i = 0; i < rows.Count; i++)
                {
                    var dict = rows[i];
                    if (dict == null)
                    {
                        continue;
                    }

                    var needsEnrichment = string.IsNullOrWhiteSpace(CoerceString(ReadValue(dict, "other_info"), 8000))
                        || string.IsNullOrWhiteSpace(CoerceString(ReadValue(dict, "bank_name"), 200))
                        || string.IsNullOrWhiteSpace(CoerceString(ReadValue(dict, "ifsc_code"), 50))
                        || string.IsNullOrWhiteSpace(CoerceString(ReadValue(dict, "family_member_count"), 20));

                    if (needsEnrichment && i < maxEnrichmentRows)
                    {
                        var identifier = CoerceString(ReadValue(dict, "reg_id"), 30);
                        if (!string.IsNullOrWhiteSpace(identifier))
                        {
                            var resolved = _repository.ResolveRegistration(identifier);
                            if (resolved.Success)
                            {
                                var resolvedRow = resolved.Data as Dictionary<string, object>;
                                if (resolvedRow != null)
                                {
                                    foreach (var pair in resolvedRow)
                                    {
                                        dict[pair.Key] = pair.Value;
                                    }
                                }
                            }
                        }
                    }

                    HydrateOtherInfoFromDedicatedColumns(dict);
                }

                return;
            }

            var root = data as Dictionary<string, object>;
            if (root == null)
            {
                return;
            }

            foreach (var entry in root.Values)
            {
                var nestedRows = entry as IEnumerable<object>;
                if (nestedRows == null)
                {
                    continue;
                }

                foreach (var rowObj in nestedRows)
                {
                    var row = rowObj as Dictionary<string, object>;
                    if (row != null)
                    {
                        HydrateOtherInfoFromDedicatedColumns(row);
                    }
                }

                break;
            }
        }

        private static string CoerceDateIso(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd");
            }

            var raw = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            DateTime parsed;
            return DateTime.TryParse(raw, out parsed) ? parsed.ToString("yyyy-MM-dd") : string.Empty;
        }
    }
}
