using System;

namespace SmkcApi.Models.DepositManager
{
    public class CreateRequirementRequest
    {
        public string SchemeName { get; set; }
        public string DepositType { get; set; }
        public decimal Amount { get; set; }
        public int DepositPeriod { get; set; }

        // Internal UTC value used by DB and validation
        public DateTime ValidityPeriodUtc { get; set; }

        // JSON-facing property: accepts `validityPeriod` from client and converts to UTC
        public DateTime ValidityPeriod
        {
            get => ValidityPeriodUtc;
            set => ValidityPeriodUtc = value.ToUniversalTime();
        }

        public string Description { get; set; }
        public string CreatedBy { get; set; }

        public ApiResponse Validate()
        {
            if (string.IsNullOrWhiteSpace(SchemeName) || SchemeName.Length < 3)
                return new ApiResponse { Success = false, Message = "Invalid schemeName" };
            if (DepositPeriod <= 0) return new ApiResponse { Success = false, Message = "depositPeriod must be > 0" };
            if (Amount <= 0) return new ApiResponse { Success = false, Message = "amount must be > 0" };
            if (ValidityPeriodUtc <= DateTime.UtcNow) return new ApiResponse { Success = false, Message = "validityPeriod must be future date" };
            if (DepositType != "callable" && DepositType != "non-callable")
                return new ApiResponse { Success = false, Message = "Invalid depositType" };
            return new ApiResponse { Success = true, Message = "OK" };
        }
    }

    public class PublishRequirementRequest
    {
        public string AuthorizedBy { get; set; }
    }

    public class SubmitQuoteRequest
    {
        public string RequirementId { get; set; }
        public string BankId { get; set; }
        public decimal InterestRate { get; set; }
        public string Remarks { get; set; }
        public ConsentDocumentDto ConsentDocument { get; set; }

        public ApiResponse Validate()
        {
            if (string.IsNullOrWhiteSpace(RequirementId) || string.IsNullOrWhiteSpace(BankId))
                return new ApiResponse { Success = false, Message = "requirementId and bankId are required" };
            if (InterestRate <= 0 || InterestRate > 20)
                return new ApiResponse { Success = false, Message = "interestRate must be > 0 and <= 20" };
            if (ConsentDocument == null) return new ApiResponse { Success = false, Message = "consentDocument is required" };

            // New flow support:
            // 1) legacy/direct upload on submit -> FileName + FileData + FileSize
            // 2) pre-uploaded consent (Google Drive endpoint) -> FileName only
            if (string.IsNullOrWhiteSpace(ConsentDocument.FileName))
                return new ApiResponse { Success = false, Message = "consentDocument fileName is required" };

            var hasInlineFileData = !string.IsNullOrWhiteSpace(ConsentDocument.FileData);
            if (hasInlineFileData)
            {
                if (ConsentDocument.FileSize <= 0 || ConsentDocument.FileSize > 5242880)
                    return new ApiResponse { Success = false, Message = "consentDocument fileSize must be <= 5MB" };
            }

            return new ApiResponse { Success = true, Message = "OK" };
        }
    }

    public class CreateBankRequest
    {
        public string Name { get; set; }
        public string BranchAddress { get; set; }
        public string Address { get; set; }
        public string Micr { get; set; }
        public string Ifsc { get; set; }
        public string Email { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNo { get; set; }
        public string Phone { get; set; }

        public ApiResponse Validate()
        {
            if (string.IsNullOrWhiteSpace(Name) || Name.Length < 3)
                return new ApiResponse { Success = false, Message = "name min length 3" };
            return new ApiResponse { Success = true, Message = "OK" };
        }
    }

    public class ConsentDocumentDto
    {
        public string FileName { get; set; }
        public string FileData { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
