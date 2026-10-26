using System;
using System.Collections.Generic;
using SmkcApi.Models.DepositManager;

namespace SmkcApi.Models
{
    // ---------------------------------------------------------------
    //  Single Women Empowerment Registration — Model classes
    //  Ekal Mahila Sakshamikaran Karyakram
    // ---------------------------------------------------------------

    public class SWRApiResponse
    {
        public bool   Success            { get; set; }
        public string Message            { get; set; }
        public string RegistrationNumber { get; set; }
        public object Data               { get; set; }
        public string ErrorCode          { get; set; }
        public DateTime Timestamp        { get; set; }

        public SWRApiResponse() { Timestamp = DateTime.UtcNow; }

        public static SWRApiResponse CreateSuccess(object data, string message, string registrationNumber = null)
        {
            return new SWRApiResponse { Success = true, Message = message, Data = data, RegistrationNumber = registrationNumber };
        }

        public static SWRApiResponse CreateError(string message, string errorCode)
        {
            return new SWRApiResponse { Success = false, Message = message, ErrorCode = errorCode };
        }
    }

    public class SWROperationResult<T>
    {
        public bool   Success   { get; set; }
        public string Message   { get; set; }
        public T      Data      { get; set; }
        public string ErrorCode { get; set; }
    }

    public class SWRRegistrationRequest
    {
        // Application metadata
        public string ApplicationMode  { get; set; }   // ONLINE / OPERATOR

        // Personal Info
        public string FullName         { get; set; }
        public string Dob              { get; set; }   // yyyy-MM-dd
        public string MobileNumber     { get; set; }
        public string AadhaarNumber    { get; set; }

        // Location
        public string District         { get; set; }
        public string Taluka           { get; set; }
        public string Village          { get; set; }
        public string Ward             { get; set; }

        // Category
        public string WomenCategory    { get; set; }   // RURAL/URBAN/DOMESTIC/ADDITIONAL
        public string AgeGroup         { get; set; }   // 18-29/30-44/45-54/55-64+

        // Livelihood
        public string Occupation       { get; set; }
        public string AnnualIncome     { get; set; }
        public string MgnregaYears     { get; set; }

        // Children
        public string ChildrenBelow6   { get; set; }
        public string Children6To14    { get; set; }
        public string OtherInfo        { get; set; }
        public string PrimaryEarner    { get; set; }

        // Dedicated persisted fields (option 2)
        public string OfficePrabhag { get; set; }
        public string FullAddress { get; set; }
        public string CityAreaPrabhag { get; set; }
        public string Education { get; set; }
        public string PinCode { get; set; }
        public string SingleWomanProofName { get; set; }
        public string ProofDocumentNumber { get; set; }
        public string ProofDocumentDate { get; set; }
        public string MajorNeedSupport { get; set; }
        public string DeclarationPlace { get; set; }
        public string DeclarationDate { get; set; }
        public string ApplicantSignatureName { get; set; }
        public string RequiredSupportSummary { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string AccountNumber { get; set; }
        public string IfscCode { get; set; }
        public string AccountHolderName { get; set; }
        public string FamilyMemberCount { get; set; }
        public string RationCardNumber { get; set; }
        public string VoterIdNumber { get; set; }
        public string RationCardType { get; set; }
        public string RationCardTypeOther { get; set; }
        public string Email { get; set; }
        public string LivelihoodSource { get; set; }
        public string IsDivyangSingleWoman { get; set; }
        public string ResidenceType { get; set; }
        public string LandStatus { get; set; }
        public string LandAreaAcre { get; set; }
        public string IsBpl { get; set; }
        public string TotalChildren { get; set; }
        public string BoysCount { get; set; }
        public string GirlsCount { get; set; }
        public string ChildrenBelow18Count { get; set; }
        public string ChildrenInEducation { get; set; }
        public string GetsBalSangopanScholarship { get; set; }
        public string EducationQualification { get; set; }
        public string CurrentOccupation { get; set; }
        public string MonthlyIncome { get; set; }
        public string WomanMonthlyIncome { get; set; }
        public string FamilyAnnualIncome { get; set; }
        public string IsSelfHelpGroupMember { get; set; }
        public string RequiredSupportOther { get; set; }
        public string IsAvailingGovtScheme { get; set; }
        public string GovtSchemeName { get; set; }
        public string WantsSelfEmployment { get; set; }
        public string ExpectedBusinessType { get; set; }
        public string SkillsJson { get; set; }
        public string RequiredSupportJson { get; set; }

        // Schemes — pipe-separated "CODE|Y" or "CODE|N"
        public List<string> Schemes    { get; set; }

        // Uploaded documents (multipart form-data only)
        public List<SWRUploadedFile> Files { get; set; }

        // Operator context
        public int?   OperatorUserId   { get; set; }

        public SWRRegistrationRequest()
        {
            Schemes = new List<string>();
            Files = new List<SWRUploadedFile>();
        }
    }

    public class SWRStatusUpdateRequest
    {
        public string Status         { get; set; }
        public string Remarks        { get; set; }
        public int?   OperatorUserId { get; set; }
    }

    public class SWRSaveData
    {
        public int    RegId             { get; set; }
        public string RegistrationNumber { get; set; }
    }

    public class SWRUploadedFile
    {
        public string FieldName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Bytes { get; set; }
        public long FileSize { get; set; }
    }

    public class SWRDedicatedFields
    {
        public string OfficePrabhag { get; set; }
        public string FullAddress { get; set; }
        public string CityAreaPrabhag { get; set; }
        public string Education { get; set; }
        public string PinCode { get; set; }
        public string SingleWomanProofName { get; set; }
        public string ProofDocumentNumber { get; set; }
        public DateTime? ProofDocumentDate { get; set; }
        public string MajorNeedSupport { get; set; }
        public string DeclarationPlace { get; set; }
        public DateTime? DeclarationDate { get; set; }
        public string ApplicantSignatureName { get; set; }
        public string RequiredSupportSummary { get; set; }
        public string SkillsSummary { get; set; }
        public string ChildrenJson { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string AccountNumber { get; set; }
        public string IfscCode { get; set; }
        public string AccountHolderName { get; set; }
        public string FamilyMemberCount { get; set; }
        public string RationCardNumber { get; set; }
        public string VoterIdNumber { get; set; }
        public string RationCardType { get; set; }
        public string RationCardTypeOther { get; set; }
        public string Email { get; set; }
        public string LivelihoodSource { get; set; }
        public string IsDivyangSingleWoman { get; set; }
        public string ResidenceType { get; set; }
        public string LandStatus { get; set; }
        public string LandAreaAcre { get; set; }
        public string IsBpl { get; set; }
        public string TotalChildren { get; set; }
        public string BoysCount { get; set; }
        public string GirlsCount { get; set; }
        public string ChildrenBelow18Count { get; set; }
        public string ChildrenInEducation { get; set; }
        public string GetsBalSangopanScholarship { get; set; }
        public string EducationQualification { get; set; }
        public string CurrentOccupation { get; set; }
        public string MonthlyIncome { get; set; }
        public string WomanMonthlyIncome { get; set; }
        public string FamilyAnnualIncome { get; set; }
        public string IsSelfHelpGroupMember { get; set; }
        public string RequiredSupportOther { get; set; }
        public string IsAvailingGovtScheme { get; set; }
        public string GovtSchemeName { get; set; }
        public string WantsSelfEmployment { get; set; }
        public string ExpectedBusinessType { get; set; }
        public string SkillsJson { get; set; }
        public string RequiredSupportJson { get; set; }
    }
}
