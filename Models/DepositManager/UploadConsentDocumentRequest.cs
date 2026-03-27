using System;

namespace SmkcApi.Models.DepositManager
{
    /// <summary>
    /// Request model for uploading/creating a consent document
    /// </summary>
    public class UploadConsentDocumentRequest
    {
        /// <summary>
        /// Requirement identifier (e.g., REQ0000000001)
        /// </summary>
        public string RequirementId { get; set; }

        /// <summary>
        /// Bank identifier
        /// </summary>
        public string BankId { get; set; }

        /// <summary>
        /// Quote identifier (optional, for reference)
        /// </summary>
        public string QuoteId { get; set; }

        /// <summary>
        /// File name for the consent document (e.g., consent_document.pdf)
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Base64 encoded file data
        /// Supports both raw base64 and data URI format (data:application/pdf;base64,...)
        /// </summary>
        public string FileData { get; set; }

        /// <summary>
        /// File size in bytes (optional, for validation)
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Content type of the file (default: application/pdf)
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Uploaded by (user identifier, optional)
        /// </summary>
        public string UploadedBy { get; set; }

        /// <summary>
        /// Additional remarks or notes (optional)
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Validate the request parameters
        /// </summary>
        /// <returns>ApiResponse with validation result</returns>
        public ApiResponse Validate()
        {
            // Required field validation
            if (string.IsNullOrWhiteSpace(RequirementId))
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "requirementId is required",
                    Error = "INVALID_PARAMETER",
                    ErrorCode = "REQ_ID_REQUIRED"
                };

            if (string.IsNullOrWhiteSpace(BankId))
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "bankId is required",
                    Error = "INVALID_PARAMETER",
                    ErrorCode = "BANK_ID_REQUIRED"
                };

            if (string.IsNullOrWhiteSpace(FileName))
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "fileName is required",
                    Error = "INVALID_PARAMETER",
                    ErrorCode = "FILE_NAME_REQUIRED"
                };

            if (string.IsNullOrWhiteSpace(FileData))
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "fileData is required",
                    Error = "INVALID_PARAMETER",
                    ErrorCode = "FILE_DATA_REQUIRED"
                };

            // File name validation
            var fileExtension = System.IO.Path.GetExtension(FileName)?.ToLower();
            if (fileExtension != ".pdf")
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "Only PDF files are allowed",
                    Error = "INVALID_FILE_TYPE",
                    ErrorCode = "PDF_ONLY"
                };

            // File size validation (if provided)
            if (FileSize.HasValue)
            {
                if (FileSize.Value <= 0)
                    return new ApiResponse 
                    { 
                        Success = false, 
                        Message = "fileSize must be greater than 0",
                        Error = "INVALID_PARAMETER",
                        ErrorCode = "INVALID_FILE_SIZE"
                    };

                if (FileSize.Value > 5242880) // 5 MB limit
                    return new ApiResponse 
                    { 
                        Success = false, 
                        Message = "fileSize must not exceed 5 MB",
                        Error = "FILE_TOO_LARGE",
                        ErrorCode = "MAX_SIZE_EXCEEDED"
                    };
            }

            // Validate base64 format
            try
            {
                var base64Data = FileData;
                
                // Handle data URI format
                if (base64Data.Contains(","))
                {
                    var commaIndex = base64Data.IndexOf(',');
                    if (base64Data.Substring(0, commaIndex).Contains("base64"))
                    {
                        base64Data = base64Data.Substring(commaIndex + 1);
                    }
                }

                // Try to decode
                var testBytes = Convert.FromBase64String(base64Data);
                
                // Validate decoded size
                if (testBytes.Length == 0)
                    return new ApiResponse 
                    { 
                        Success = false, 
                        Message = "fileData is empty after decoding",
                        Error = "INVALID_FILE_DATA",
                        ErrorCode = "EMPTY_FILE"
                    };

                if (testBytes.Length > 5242880) // 5 MB limit
                    return new ApiResponse 
                    { 
                        Success = false, 
                        Message = "File size must not exceed 5 MB",
                        Error = "FILE_TOO_LARGE",
                        ErrorCode = "MAX_SIZE_EXCEEDED"
                    };
            }
            catch (FormatException)
            {
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "fileData must be valid base64 encoded string",
                    Error = "INVALID_BASE64",
                    ErrorCode = "INVALID_ENCODING"
                };
            }

            return new ApiResponse 
            { 
                Success = true, 
                Message = "Validation passed" 
            };
        }
    }
}
