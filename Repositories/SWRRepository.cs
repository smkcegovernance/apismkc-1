using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface ISWRRepository
    {
        SWROperationResult<SWRSaveData> SaveRegistration(SWRRegistrationRequest request, int? regId);
        SWROperationResult<object>      UpsertDedicatedFields(int regId, SWRDedicatedFields fields);
        SWROperationResult<object>      ReplaceSchemes(int regId, IList<string> schemePairs);
        SWROperationResult<bool>        AadhaarExists(string aadhaarNumber, int? excludeRegId);
        SWROperationResult<object>      GetRegistration(int? regId, string regNumber);
        SWROperationResult<object>      Search(string searchText, string status, string applicationMode);
        SWROperationResult<object>      UpdateStatus(int regId, string status, string remarks, int? operatorId);
        SWROperationResult<object>      DeleteRegistration(int regId);
        SWROperationResult<object>      ResolveRegistration(string identifier);
    }

    public class SWRRepository : ISWRRepository
    {
        private const int OtherInfoMaxLength = 2000000;
        private const int ChildrenJsonMaxLength = 2000000;

        private readonly IOracleConnectionFactory _connectionFactory;

        public SWRRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public SWROperationResult<object> UpsertDedicatedFields(int regId, SWRDedicatedFields fields)
        {
            if (regId <= 0)
            {
                return Fail<object>("Invalid registration id", "VALIDATION_ERROR");
            }

            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd = new OracleCommand(@"
UPDATE WCWC.TXN_WCWC_SINGLE_WOMEN_REG
   SET OFFICE_PRABHAG = NVL(:P_OFFICE_PRABHAG, OFFICE_PRABHAG),
       FULL_ADDRESS = NVL(:P_FULL_ADDRESS, FULL_ADDRESS),
       CITY_AREA_PRABHAG = NVL(:P_CITY_AREA_PRABHAG, CITY_AREA_PRABHAG),
       PIN_CODE = NVL(:P_PIN_CODE, PIN_CODE),
       SINGLE_WOMAN_PROOF_NAME = NVL(:P_SINGLE_WOMAN_PROOF_NAME, SINGLE_WOMAN_PROOF_NAME),
       PROOF_DOCUMENT_NUMBER = NVL(:P_PROOF_DOCUMENT_NUMBER, PROOF_DOCUMENT_NUMBER),
    PROOF_DOCUMENT_DATE = NVL(:P_PROOF_DOCUMENT_DATE, PROOF_DOCUMENT_DATE),
    MAJOR_NEED_SUPPORT = NVL(:P_MAJOR_NEED_SUPPORT, MAJOR_NEED_SUPPORT),
    DECLARATION_PLACE = NVL(:P_DECLARATION_PLACE, DECLARATION_PLACE),
    DECLARATION_DATE = NVL(:P_DECLARATION_DATE, DECLARATION_DATE),
    APPLICANT_SIGNATURE_NAME = NVL(:P_APPLICANT_SIGNATURE_NAME, APPLICANT_SIGNATURE_NAME),
    REQUIRED_SUPPORT_SUMMARY = NVL(:P_REQUIRED_SUPPORT_SUMMARY, REQUIRED_SUPPORT_SUMMARY),
    RATION_CARD_NUMBER = NVL(:P_RATION_CARD_NUMBER, RATION_CARD_NUMBER),
    VOTER_ID_NUMBER = NVL(:P_VOTER_ID_NUMBER, VOTER_ID_NUMBER),
    RATION_CARD_TYPE = NVL(:P_RATION_CARD_TYPE, RATION_CARD_TYPE),
    RATION_CARD_TYPE_OTHER = NVL(:P_RATION_CARD_TYPE_OTHER, RATION_CARD_TYPE_OTHER),
    EMAIL = NVL(:P_EMAIL, EMAIL),
    LIVELIHOOD_SOURCE = NVL(:P_LIVELIHOOD_SOURCE, LIVELIHOOD_SOURCE),
    IS_DIVYANG_SINGLE_WOMAN = NVL(:P_IS_DIVYANG_SINGLE_WOMAN, IS_DIVYANG_SINGLE_WOMAN),
    RESIDENCE_TYPE = NVL(:P_RESIDENCE_TYPE, RESIDENCE_TYPE),
    LAND_STATUS = NVL(:P_LAND_STATUS, LAND_STATUS),
    LAND_AREA_ACRE = NVL(:P_LAND_AREA_ACRE, LAND_AREA_ACRE),
    IS_BPL = NVL(:P_IS_BPL, IS_BPL),
    TOTAL_CHILDREN = NVL(:P_TOTAL_CHILDREN, TOTAL_CHILDREN),
    BOYS_COUNT = NVL(:P_BOYS_COUNT, BOYS_COUNT),
    GIRLS_COUNT = NVL(:P_GIRLS_COUNT, GIRLS_COUNT),
    CHILDREN_BELOW18_COUNT = NVL(:P_CHILDREN_BELOW18_COUNT, CHILDREN_BELOW18_COUNT),
    CHILDREN_IN_EDUCATION = NVL(:P_CHILDREN_IN_EDUCATION, CHILDREN_IN_EDUCATION),
    GETS_BAL_SANGOPAN_SCHOLARSHIP = NVL(:P_GETS_BAL_SANGOPAN_SCHOLARSHIP, GETS_BAL_SANGOPAN_SCHOLARSHIP),
    EDUCATION = NVL(:P_EDUCATION, EDUCATION),
    EDUCATION_QUALIFICATION = NVL(:P_EDUCATION_QUALIFICATION, EDUCATION_QUALIFICATION),
    CURRENT_OCCUPATION = NVL(:P_CURRENT_OCCUPATION, CURRENT_OCCUPATION),
    MONTHLY_INCOME = NVL(:P_MONTHLY_INCOME, MONTHLY_INCOME),
    WOMAN_MONTHLY_INCOME = NVL(:P_WOMAN_MONTHLY_INCOME, WOMAN_MONTHLY_INCOME),
    FAMILY_ANNUAL_INCOME = NVL(:P_FAMILY_ANNUAL_INCOME, FAMILY_ANNUAL_INCOME),
    IS_SELF_HELP_GROUP_MEMBER = NVL(:P_IS_SELF_HELP_GROUP_MEMBER, IS_SELF_HELP_GROUP_MEMBER),
    REQUIRED_SUPPORT_OTHER = NVL(:P_REQUIRED_SUPPORT_OTHER, REQUIRED_SUPPORT_OTHER),
    IS_AVAILING_GOVT_SCHEME = NVL(:P_IS_AVAILING_GOVT_SCHEME, IS_AVAILING_GOVT_SCHEME),
    GOVT_SCHEME_NAME = NVL(:P_GOVT_SCHEME_NAME, GOVT_SCHEME_NAME),
    WANTS_SELF_EMPLOYMENT = NVL(:P_WANTS_SELF_EMPLOYMENT, WANTS_SELF_EMPLOYMENT),
    EXPECTED_BUSINESS_TYPE = NVL(:P_EXPECTED_BUSINESS_TYPE, EXPECTED_BUSINESS_TYPE),
    SKILLS_JSON = NVL(:P_SKILLS_JSON, SKILLS_JSON),
    REQUIRED_SUPPORT_JSON = NVL(:P_REQUIRED_SUPPORT_JSON, REQUIRED_SUPPORT_JSON),
    SKILLS_SUMMARY = NVL(:P_SKILLS_SUMMARY, SKILLS_SUMMARY),
    CHILDREN_JSON = NVL(:P_CHILDREN_JSON, CHILDREN_JSON),
    BANK_NAME = NVL(:P_BANK_NAME, BANK_NAME),
    BRANCH_NAME = NVL(:P_BRANCH_NAME, BRANCH_NAME),
    ACCOUNT_NUMBER = NVL(:P_ACCOUNT_NUMBER, ACCOUNT_NUMBER),
    IFSC_CODE = NVL(:P_IFSC_CODE, IFSC_CODE),
    ACCOUNT_HOLDER_NAME = NVL(:P_ACCOUNT_HOLDER_NAME, ACCOUNT_HOLDER_NAME),
    FAMILY_MEMBER_COUNT = NVL(:P_FAMILY_MEMBER_COUNT, FAMILY_MEMBER_COUNT)
 WHERE REG_ID = :P_REG_ID", conn))
                {
                    conn.Open();
                    AddNullable(cmd, "P_OFFICE_PRABHAG", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.OfficePrabhag, 80) : null);
                    AddNullable(cmd, "P_FULL_ADDRESS", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.FullAddress, 250) : null);
                    AddNullable(cmd, "P_CITY_AREA_PRABHAG", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.CityAreaPrabhag, 160) : null);
                    AddNullable(cmd, "P_PIN_CODE", OracleDbType.Varchar2, fields != null ? TruncateText(fields.PinCode, 10) : null);
                    AddNullable(cmd, "P_SINGLE_WOMAN_PROOF_NAME", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.SingleWomanProofName, 160) : null);
                    AddNullable(cmd, "P_PROOF_DOCUMENT_NUMBER", OracleDbType.Varchar2, fields != null ? TruncateText(fields.ProofDocumentNumber, 60) : null);
                    AddNullable(cmd, "P_PROOF_DOCUMENT_DATE", OracleDbType.Date, fields != null ? fields.ProofDocumentDate : null);
                    AddNullable(cmd, "P_MAJOR_NEED_SUPPORT", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.MajorNeedSupport, 500) : null);
                    AddNullable(cmd, "P_DECLARATION_PLACE", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.DeclarationPlace, 120) : null);
                    AddNullable(cmd, "P_DECLARATION_DATE", OracleDbType.Date, fields != null ? fields.DeclarationDate : null);
                    AddNullable(cmd, "P_APPLICANT_SIGNATURE_NAME", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.ApplicantSignatureName, 160) : null);
                    AddNullable(cmd, "P_REQUIRED_SUPPORT_SUMMARY", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.RequiredSupportSummary, 500) : null);
                    AddNullable(cmd, "P_RATION_CARD_NUMBER", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.RationCardNumber, 50) : null);
                    AddNullable(cmd, "P_VOTER_ID_NUMBER", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.VoterIdNumber, 50) : null);
                    AddNullable(cmd, "P_RATION_CARD_TYPE", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.RationCardType, 20) : null);
                    AddNullable(cmd, "P_RATION_CARD_TYPE_OTHER", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.RationCardTypeOther, 120) : null);
                    AddNullable(cmd, "P_EMAIL", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.Email, 120) : null);
                    AddNullable(cmd, "P_LIVELIHOOD_SOURCE", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.LivelihoodSource, 120) : null);
                    AddNullable(cmd, "P_IS_DIVYANG_SINGLE_WOMAN", OracleDbType.Varchar2, fields != null ? TruncateText(fields.IsDivyangSingleWoman, 10) : null);
                    AddNullable(cmd, "P_RESIDENCE_TYPE", OracleDbType.Varchar2, fields != null ? TruncateText(fields.ResidenceType, 30) : null);
                    AddNullable(cmd, "P_LAND_STATUS", OracleDbType.Varchar2, fields != null ? TruncateText(fields.LandStatus, 30) : null);
                    AddNullable(cmd, "P_LAND_AREA_ACRE", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.LandAreaAcre, 30) : null);
                    AddNullable(cmd, "P_IS_BPL", OracleDbType.Varchar2, fields != null ? TruncateText(fields.IsBpl, 10) : null);
                    AddNullable(cmd, "P_TOTAL_CHILDREN", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.TotalChildren) : null);
                    AddNullable(cmd, "P_BOYS_COUNT", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.BoysCount) : null);
                    AddNullable(cmd, "P_GIRLS_COUNT", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.GirlsCount) : null);
                    AddNullable(cmd, "P_CHILDREN_BELOW18_COUNT", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.ChildrenBelow18Count) : null);
                    AddNullable(cmd, "P_CHILDREN_IN_EDUCATION", OracleDbType.Varchar2, fields != null ? TruncateText(fields.ChildrenInEducation, 10) : null);
                    AddNullable(cmd, "P_GETS_BAL_SANGOPAN_SCHOLARSHIP", OracleDbType.Varchar2, fields != null ? TruncateText(fields.GetsBalSangopanScholarship, 10) : null);
                    AddNullable(cmd, "P_EDUCATION", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.Education, 200) : null);
                    AddNullable(cmd, "P_EDUCATION_QUALIFICATION", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.EducationQualification, 200) : null);
                    AddNullable(cmd, "P_CURRENT_OCCUPATION", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.CurrentOccupation, 500) : null);
                    AddNullable(cmd, "P_MONTHLY_INCOME", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.MonthlyIncome) : null);
                    AddNullable(cmd, "P_WOMAN_MONTHLY_INCOME", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.WomanMonthlyIncome) : null);
                    AddNullable(cmd, "P_FAMILY_ANNUAL_INCOME", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.FamilyAnnualIncome) : null);
                    AddNullable(cmd, "P_IS_SELF_HELP_GROUP_MEMBER", OracleDbType.Varchar2, fields != null ? TruncateText(fields.IsSelfHelpGroupMember, 10) : null);
                    AddNullable(cmd, "P_REQUIRED_SUPPORT_OTHER", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.RequiredSupportOther, 1000) : null);
                    AddNullable(cmd, "P_IS_AVAILING_GOVT_SCHEME", OracleDbType.Varchar2, fields != null ? TruncateText(fields.IsAvailingGovtScheme, 10) : null);
                    AddNullable(cmd, "P_GOVT_SCHEME_NAME", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.GovtSchemeName, 500) : null);
                    AddNullable(cmd, "P_WANTS_SELF_EMPLOYMENT", OracleDbType.Varchar2, fields != null ? TruncateText(fields.WantsSelfEmployment, 10) : null);
                    AddNullable(cmd, "P_EXPECTED_BUSINESS_TYPE", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.ExpectedBusinessType, 500) : null);
                    AddNullable(cmd, "P_SKILLS_JSON", OracleDbType.NClob, fields != null ? TruncateText(fields.SkillsJson, ChildrenJsonMaxLength) : null);
                    AddNullable(cmd, "P_REQUIRED_SUPPORT_JSON", OracleDbType.NClob, fields != null ? TruncateText(fields.RequiredSupportJson, ChildrenJsonMaxLength) : null);
                    AddNullable(cmd, "P_SKILLS_SUMMARY", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.SkillsSummary, 500) : null);
                    AddNullable(cmd, "P_CHILDREN_JSON", OracleDbType.NClob, fields != null ? TruncateText(fields.ChildrenJson, ChildrenJsonMaxLength) : null);
                    AddNullable(cmd, "P_BANK_NAME", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.BankName, 120) : null);
                    AddNullable(cmd, "P_BRANCH_NAME", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.BranchName, 120) : null);
                    AddNullable(cmd, "P_ACCOUNT_NUMBER", OracleDbType.Varchar2, fields != null ? TruncateText(fields.AccountNumber, 32) : null);
                    AddNullable(cmd, "P_IFSC_CODE", OracleDbType.Varchar2, fields != null ? TruncateText(fields.IfscCode, 20) : null);
                    AddNullable(cmd, "P_ACCOUNT_HOLDER_NAME", OracleDbType.NVarchar2, fields != null ? TruncateText(fields.AccountHolderName, 160) : null);
                    AddNullable(cmd, "P_FAMILY_MEMBER_COUNT", OracleDbType.Decimal, fields != null ? ParseDecimal(fields.FamilyMemberCount) : null);
                    AddNullable(cmd, "P_REG_ID", OracleDbType.Decimal, regId);

                    var affected = cmd.ExecuteNonQuery();
                    if (affected <= 0)
                    {
                        return Fail<object>("Registration not found", "REG_NOT_FOUND");
                    }

                    return new SWROperationResult<object> { Success = true, Message = "Dedicated fields updated" };
                }
            }
            catch (OracleException ex)
            {
                // Migration not applied yet: keep backward compatibility.
                if (ex.Number == 904 || ex.Number == 942)
                {
                    return new SWROperationResult<object> { Success = true, Message = "Dedicated fields columns not present; skipped" };
                }

                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Save (INSERT or UPDATE)
        // ----------------------------------------------------------------
        public SWROperationResult<SWRSaveData> SaveRegistration(SWRRegistrationRequest req, int? regId)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                {
                    conn.Open();
                    try
                    {
                        return SaveRegistrationWithDedicatedParams(conn, req, regId);
                    }
                    catch (OracleException ex)
                    {
                        if (ex.Number == 6550 || ex.Number == 6553 || ex.Number == 904)
                        {
                            return SaveRegistrationLegacy(conn, req, regId);
                        }

                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return Fail<SWRSaveData>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        private SWROperationResult<SWRSaveData> SaveRegistrationLegacy(OracleConnection conn, SWRRegistrationRequest req, int? regId)
        {
            using (var cmd = new OracleCommand("WCWC.SP_WCWC_SAVE_SINGLE_WOMEN_REG", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var safeOtherInfo = TruncateText(req.OtherInfo, OtherInfoMaxLength);

                AddNullable(cmd, "P_REG_ID", OracleDbType.Decimal, regId);
                AddNullable(cmd, "P_APPLICATION_MODE", OracleDbType.Varchar2, req.ApplicationMode);
                AddNullable(cmd, "P_FULL_NAME", OracleDbType.NVarchar2, req.FullName);
                AddNullable(cmd, "P_DOB", OracleDbType.Date, ParseDate(req.Dob));
                AddNullable(cmd, "P_MOBILE_NUMBER", OracleDbType.Varchar2, req.MobileNumber);
                AddNullable(cmd, "P_AADHAAR_NUMBER", OracleDbType.Varchar2, req.AadhaarNumber);
                AddNullable(cmd, "P_DISTRICT", OracleDbType.NVarchar2, req.District);
                AddNullable(cmd, "P_TALUKA", OracleDbType.NVarchar2, req.Taluka);
                AddNullable(cmd, "P_VILLAGE", OracleDbType.NVarchar2, req.Village);
                AddNullable(cmd, "P_WARD", OracleDbType.NVarchar2, req.Ward);
                AddNullable(cmd, "P_WOMEN_CATEGORY", OracleDbType.Varchar2, req.WomenCategory);
                AddNullable(cmd, "P_AGE_GROUP", OracleDbType.Varchar2, req.AgeGroup);
                AddNullable(cmd, "P_OCCUPATION", OracleDbType.NVarchar2, req.Occupation);
                AddNullable(cmd, "P_ANNUAL_INCOME", OracleDbType.Decimal, ParseDecimal(req.AnnualIncome));
                AddNullable(cmd, "P_MGNREGA_YEARS", OracleDbType.Decimal, ParseDecimal(req.MgnregaYears));
                AddNullable(cmd, "P_CHILDREN_BELOW6", OracleDbType.Decimal, ParseDecimal(req.ChildrenBelow6));
                AddNullable(cmd, "P_CHILDREN_6TO14", OracleDbType.Decimal, ParseDecimal(req.Children6To14));
                AddNullable(cmd, "P_OTHER_INFO", OracleDbType.NClob, safeOtherInfo);
                AddNullable(cmd, "P_PRIMARY_EARNER", OracleDbType.NVarchar2, req.PrimaryEarner);
                AddNullable(cmd, "P_OPERATOR_USER_ID", OracleDbType.Decimal, req.OperatorUserId);

                AddOut(cmd, "P_OUT_REG_ID", OracleDbType.Decimal, 22);
                AddOut(cmd, "P_OUT_REG_NUMBER", OracleDbType.Varchar2, 30);
                AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2, 30);
                AddOut(cmd, "P_OUT_MESSAGE", OracleDbType.NVarchar2, 500);

                cmd.ExecuteNonQuery();
                return ReadSaveResult(cmd);
            }
        }

        private SWROperationResult<SWRSaveData> SaveRegistrationWithDedicatedParams(OracleConnection conn, SWRRegistrationRequest req, int? regId)
        {
            using (var cmd = new OracleCommand("WCWC.SP_WCWC_SAVE_SINGLE_WOMEN_REG", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var safeOtherInfo = TruncateText(req.OtherInfo, OtherInfoMaxLength);
                var dedicated = ExtractDedicatedFields(req);

                AddNullable(cmd, "P_REG_ID", OracleDbType.Decimal, regId);
                AddNullable(cmd, "P_APPLICATION_MODE", OracleDbType.Varchar2, req.ApplicationMode);
                AddNullable(cmd, "P_FULL_NAME", OracleDbType.NVarchar2, req.FullName);
                AddNullable(cmd, "P_DOB", OracleDbType.Date, ParseDate(req.Dob));
                AddNullable(cmd, "P_MOBILE_NUMBER", OracleDbType.Varchar2, req.MobileNumber);
                AddNullable(cmd, "P_AADHAAR_NUMBER", OracleDbType.Varchar2, req.AadhaarNumber);
                AddNullable(cmd, "P_DISTRICT", OracleDbType.NVarchar2, req.District);
                AddNullable(cmd, "P_TALUKA", OracleDbType.NVarchar2, req.Taluka);
                AddNullable(cmd, "P_VILLAGE", OracleDbType.NVarchar2, req.Village);
                AddNullable(cmd, "P_WARD", OracleDbType.NVarchar2, req.Ward);
                AddNullable(cmd, "P_WOMEN_CATEGORY", OracleDbType.Varchar2, req.WomenCategory);
                AddNullable(cmd, "P_AGE_GROUP", OracleDbType.Varchar2, req.AgeGroup);
                AddNullable(cmd, "P_OCCUPATION", OracleDbType.NVarchar2, req.Occupation);
                AddNullable(cmd, "P_ANNUAL_INCOME", OracleDbType.Decimal, ParseDecimal(req.AnnualIncome));
                AddNullable(cmd, "P_MGNREGA_YEARS", OracleDbType.Decimal, ParseDecimal(req.MgnregaYears));
                AddNullable(cmd, "P_CHILDREN_BELOW6", OracleDbType.Decimal, ParseDecimal(req.ChildrenBelow6));
                AddNullable(cmd, "P_CHILDREN_6TO14", OracleDbType.Decimal, ParseDecimal(req.Children6To14));
                AddNullable(cmd, "P_OTHER_INFO", OracleDbType.NClob, safeOtherInfo);
                AddNullable(cmd, "P_PRIMARY_EARNER", OracleDbType.NVarchar2, req.PrimaryEarner);
                AddNullable(cmd, "P_OPERATOR_USER_ID", OracleDbType.Decimal, req.OperatorUserId);
                AddNullable(cmd, "P_OFFICE_PRABHAG", OracleDbType.NVarchar2, TruncateText(dedicated.OfficePrabhag, 80));
                AddNullable(cmd, "P_FULL_ADDRESS", OracleDbType.NVarchar2, TruncateText(dedicated.FullAddress, 250));
                AddNullable(cmd, "P_CITY_AREA_PRABHAG", OracleDbType.NVarchar2, TruncateText(dedicated.CityAreaPrabhag, 160));
                AddNullable(cmd, "P_PIN_CODE", OracleDbType.Varchar2, TruncateText(dedicated.PinCode, 10));
                AddNullable(cmd, "P_SINGLE_WOMAN_PROOF_NAME", OracleDbType.NVarchar2, TruncateText(dedicated.SingleWomanProofName, 160));
                AddNullable(cmd, "P_PROOF_DOCUMENT_NUMBER", OracleDbType.Varchar2, TruncateText(dedicated.ProofDocumentNumber, 60));
                AddNullable(cmd, "P_PROOF_DOCUMENT_DATE", OracleDbType.Date, dedicated.ProofDocumentDate);

                AddOut(cmd, "P_OUT_REG_ID", OracleDbType.Decimal, 22);
                AddOut(cmd, "P_OUT_REG_NUMBER", OracleDbType.Varchar2, 30);
                AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2, 30);
                AddOut(cmd, "P_OUT_MESSAGE", OracleDbType.NVarchar2, 500);

                cmd.ExecuteNonQuery();
                return ReadSaveResult(cmd);
            }
        }

        private static SWROperationResult<SWRSaveData> ReadSaveResult(OracleCommand cmd)
        {
            var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
            var message = GetOutString(cmd, "P_OUT_MESSAGE");

            if (statusCode != "SUCCESS")
                return Fail<SWRSaveData>(message, statusCode);

            return new SWROperationResult<SWRSaveData>
            {
                Success = true,
                Message = message,
                Data = new SWRSaveData
                {
                    RegId = GetOutInt(cmd, "P_OUT_REG_ID"),
                    RegistrationNumber = GetOutString(cmd, "P_OUT_REG_NUMBER")
                }
            };
        }

        private static SWRDedicatedFields ExtractDedicatedFields(SWRRegistrationRequest req)
        {
            var result = new SWRDedicatedFields();
            if (req == null)
            {
                return result;
            }

            result.OfficePrabhag = ReadString(req.OfficePrabhag);
            result.FullAddress = ReadString(req.FullAddress);
            result.CityAreaPrabhag = ReadString(req.CityAreaPrabhag);
            result.PinCode = ReadString(req.PinCode);
            result.SingleWomanProofName = ReadString(req.SingleWomanProofName);
            result.ProofDocumentNumber = ReadString(req.ProofDocumentNumber);
            result.RationCardNumber = ReadString(req.RationCardNumber);
            result.VoterIdNumber = ReadString(req.VoterIdNumber);
            result.RationCardType = ReadString(req.RationCardType);
            result.RationCardTypeOther = ReadString(req.RationCardTypeOther);
            result.Email = ReadString(req.Email);
            result.LivelihoodSource = ReadString(req.LivelihoodSource);
            result.IsDivyangSingleWoman = ReadString(req.IsDivyangSingleWoman);
            result.ResidenceType = ReadString(req.ResidenceType);
            result.LandStatus = ReadString(req.LandStatus);
            result.LandAreaAcre = ReadString(req.LandAreaAcre);
            result.IsBpl = ReadString(req.IsBpl);
            result.TotalChildren = ReadString(req.TotalChildren);
            result.BoysCount = ReadString(req.BoysCount);
            result.GirlsCount = ReadString(req.GirlsCount);
            result.ChildrenBelow18Count = ReadString(req.ChildrenBelow18Count);
            result.ChildrenInEducation = ReadString(req.ChildrenInEducation);
            result.GetsBalSangopanScholarship = ReadString(req.GetsBalSangopanScholarship);
            result.Education = ReadString(req.Education);
            result.EducationQualification = ReadString(req.EducationQualification);
            result.CurrentOccupation = ReadString(req.CurrentOccupation);
            result.MonthlyIncome = ReadString(req.MonthlyIncome);
            result.WomanMonthlyIncome = ReadString(req.WomanMonthlyIncome);
            result.FamilyAnnualIncome = ReadString(req.FamilyAnnualIncome);
            result.IsSelfHelpGroupMember = ReadString(req.IsSelfHelpGroupMember);
            result.RequiredSupportOther = ReadString(req.RequiredSupportOther);
            result.IsAvailingGovtScheme = ReadString(req.IsAvailingGovtScheme);
            result.GovtSchemeName = ReadString(req.GovtSchemeName);
            result.WantsSelfEmployment = ReadString(req.WantsSelfEmployment);
            result.ExpectedBusinessType = ReadString(req.ExpectedBusinessType);
            result.SkillsJson = ReadString(req.SkillsJson);
            result.RequiredSupportJson = ReadString(req.RequiredSupportJson);
            DateTime fromReq;
            if (DateTime.TryParse(req.ProofDocumentDate, out fromReq))
            {
                result.ProofDocumentDate = fromReq;
            }

            var otherInfo = req.OtherInfo;
            if (string.IsNullOrWhiteSpace(otherInfo))
            {
                return result;
            }

            try
            {
                var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(otherInfo)
                           ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                var profile = GetObject(root, "profile");
                var details = GetObject(root, "details");
                var compactProfile = GetObject(root, "p");
                var compactDetails = GetObject(root, "d");

                if (string.IsNullOrWhiteSpace(result.OfficePrabhag)) result.OfficePrabhag = ReadString(profile, "officePrabhag");
                if (string.IsNullOrWhiteSpace(result.OfficePrabhag)) result.OfficePrabhag = ReadString(compactProfile, "o");

                if (string.IsNullOrWhiteSpace(result.FullAddress)) result.FullAddress = ReadString(profile, "fullAddress");
                if (string.IsNullOrWhiteSpace(result.FullAddress)) result.FullAddress = ReadString(compactProfile, "a");

                if (string.IsNullOrWhiteSpace(result.CityAreaPrabhag)) result.CityAreaPrabhag = ReadString(profile, "cityAreaPrabhag");
                if (string.IsNullOrWhiteSpace(result.CityAreaPrabhag)) result.CityAreaPrabhag = ReadString(compactProfile, "c");

                if (string.IsNullOrWhiteSpace(result.PinCode)) result.PinCode = ReadString(profile, "pinCode");
                if (string.IsNullOrWhiteSpace(result.PinCode)) result.PinCode = ReadString(compactProfile, "p");

                if (string.IsNullOrWhiteSpace(result.SingleWomanProofName)) result.SingleWomanProofName = ReadString(details, "singleWomanProofName");
                if (string.IsNullOrWhiteSpace(result.SingleWomanProofName)) result.SingleWomanProofName = ReadString(compactDetails, "spn");

                if (string.IsNullOrWhiteSpace(result.ProofDocumentNumber)) result.ProofDocumentNumber = ReadString(details, "proofDocumentNumber");
                if (string.IsNullOrWhiteSpace(result.ProofDocumentNumber)) result.ProofDocumentNumber = ReadString(compactDetails, "pn");

                if (string.IsNullOrWhiteSpace(result.RationCardNumber)) result.RationCardNumber = ReadString(details, "rationCardNumber");
                if (string.IsNullOrWhiteSpace(result.RationCardNumber)) result.RationCardNumber = ReadString(compactDetails, "rn");

                if (string.IsNullOrWhiteSpace(result.VoterIdNumber)) result.VoterIdNumber = ReadString(details, "voterIdNumber");
                if (string.IsNullOrWhiteSpace(result.VoterIdNumber)) result.VoterIdNumber = ReadString(compactDetails, "vn");

                if (string.IsNullOrWhiteSpace(result.RationCardType)) result.RationCardType = ReadString(details, "rationCardType");
                if (string.IsNullOrWhiteSpace(result.RationCardType)) result.RationCardType = ReadString(compactDetails, "rt");

                if (string.IsNullOrWhiteSpace(result.RationCardTypeOther)) result.RationCardTypeOther = ReadString(details, "rationCardTypeOther");
                if (string.IsNullOrWhiteSpace(result.RationCardTypeOther)) result.RationCardTypeOther = ReadString(compactDetails, "rto");

                if (string.IsNullOrWhiteSpace(result.Email)) result.Email = ReadString(details, "email");
                if (string.IsNullOrWhiteSpace(result.Email)) result.Email = ReadString(compactDetails, "e");

                if (string.IsNullOrWhiteSpace(result.LivelihoodSource)) result.LivelihoodSource = ReadString(details, "livelihoodSource");
                if (string.IsNullOrWhiteSpace(result.LivelihoodSource)) result.LivelihoodSource = ReadString(compactDetails, "ls");

                if (string.IsNullOrWhiteSpace(result.IsDivyangSingleWoman)) result.IsDivyangSingleWoman = ReadString(details, "isDivyangSingleWoman");
                if (string.IsNullOrWhiteSpace(result.IsDivyangSingleWoman)) result.IsDivyangSingleWoman = ReadString(compactDetails, "dw");

                if (string.IsNullOrWhiteSpace(result.ResidenceType)) result.ResidenceType = ReadString(details, "residenceType");
                if (string.IsNullOrWhiteSpace(result.ResidenceType)) result.ResidenceType = ReadString(compactDetails, "rs");

                if (string.IsNullOrWhiteSpace(result.LandStatus)) result.LandStatus = ReadString(details, "landStatus");
                if (string.IsNullOrWhiteSpace(result.LandStatus)) result.LandStatus = ReadString(compactDetails, "lds");

                if (string.IsNullOrWhiteSpace(result.LandAreaAcre)) result.LandAreaAcre = ReadString(details, "landAreaAcre");
                if (string.IsNullOrWhiteSpace(result.LandAreaAcre)) result.LandAreaAcre = ReadString(compactDetails, "la");

                if (string.IsNullOrWhiteSpace(result.IsBpl)) result.IsBpl = ReadString(details, "isBpl");
                if (string.IsNullOrWhiteSpace(result.IsBpl)) result.IsBpl = ReadString(compactDetails, "bpl");

                if (string.IsNullOrWhiteSpace(result.TotalChildren)) result.TotalChildren = ReadString(details, "totalChildren");
                if (string.IsNullOrWhiteSpace(result.TotalChildren)) result.TotalChildren = ReadString(compactDetails, "tc");

                if (string.IsNullOrWhiteSpace(result.BoysCount)) result.BoysCount = ReadString(details, "boysCount");
                if (string.IsNullOrWhiteSpace(result.BoysCount)) result.BoysCount = ReadString(compactDetails, "bc");

                if (string.IsNullOrWhiteSpace(result.GirlsCount)) result.GirlsCount = ReadString(details, "girlsCount");
                if (string.IsNullOrWhiteSpace(result.GirlsCount)) result.GirlsCount = ReadString(compactDetails, "gc");

                if (string.IsNullOrWhiteSpace(result.ChildrenBelow18Count)) result.ChildrenBelow18Count = ReadString(details, "childrenBelow18Count");
                if (string.IsNullOrWhiteSpace(result.ChildrenBelow18Count)) result.ChildrenBelow18Count = ReadString(compactDetails, "cb18");

                if (string.IsNullOrWhiteSpace(result.ChildrenInEducation)) result.ChildrenInEducation = ReadString(details, "childrenInEducation");
                if (string.IsNullOrWhiteSpace(result.ChildrenInEducation)) result.ChildrenInEducation = ReadString(compactDetails, "cedu");

                if (string.IsNullOrWhiteSpace(result.GetsBalSangopanScholarship)) result.GetsBalSangopanScholarship = ReadString(details, "getsBalSangopanScholarship");
                if (string.IsNullOrWhiteSpace(result.GetsBalSangopanScholarship)) result.GetsBalSangopanScholarship = ReadString(compactDetails, "bss");

                if (string.IsNullOrWhiteSpace(result.Education)) result.Education = ReadString(details, "education");
                if (string.IsNullOrWhiteSpace(result.Education)) result.Education = ReadString(compactDetails, "ed");

                if (string.IsNullOrWhiteSpace(result.EducationQualification)) result.EducationQualification = ReadString(details, "educationQualification");
                if (string.IsNullOrWhiteSpace(result.EducationQualification)) result.EducationQualification = ReadString(compactDetails, "eq");

                if (string.IsNullOrWhiteSpace(result.CurrentOccupation)) result.CurrentOccupation = ReadString(details, "currentOccupation");
                if (string.IsNullOrWhiteSpace(result.CurrentOccupation)) result.CurrentOccupation = ReadString(compactDetails, "oc");

                if (string.IsNullOrWhiteSpace(result.MonthlyIncome)) result.MonthlyIncome = ReadString(details, "monthlyIncome");
                if (string.IsNullOrWhiteSpace(result.MonthlyIncome)) result.MonthlyIncome = ReadString(compactDetails, "mi");

                if (string.IsNullOrWhiteSpace(result.WomanMonthlyIncome)) result.WomanMonthlyIncome = ReadString(details, "womanMonthlyIncome");
                if (string.IsNullOrWhiteSpace(result.WomanMonthlyIncome)) result.WomanMonthlyIncome = ReadString(compactDetails, "wmi");

                if (string.IsNullOrWhiteSpace(result.FamilyAnnualIncome)) result.FamilyAnnualIncome = ReadString(details, "familyAnnualIncome");
                if (string.IsNullOrWhiteSpace(result.FamilyAnnualIncome)) result.FamilyAnnualIncome = ReadString(compactDetails, "fai");

                if (string.IsNullOrWhiteSpace(result.IsSelfHelpGroupMember)) result.IsSelfHelpGroupMember = ReadString(details, "isSelfHelpGroupMember");
                if (string.IsNullOrWhiteSpace(result.IsSelfHelpGroupMember)) result.IsSelfHelpGroupMember = ReadString(compactDetails, "shg");

                if (string.IsNullOrWhiteSpace(result.RequiredSupportOther)) result.RequiredSupportOther = ReadString(details, "requiredSupportOther");
                if (string.IsNullOrWhiteSpace(result.RequiredSupportOther)) result.RequiredSupportOther = ReadString(compactDetails, "rso");

                if (string.IsNullOrWhiteSpace(result.IsAvailingGovtScheme)) result.IsAvailingGovtScheme = ReadString(details, "isAvailingGovtScheme");
                if (string.IsNullOrWhiteSpace(result.IsAvailingGovtScheme)) result.IsAvailingGovtScheme = ReadString(compactDetails, "ags");

                if (string.IsNullOrWhiteSpace(result.GovtSchemeName)) result.GovtSchemeName = ReadString(details, "govtSchemeName");
                if (string.IsNullOrWhiteSpace(result.GovtSchemeName)) result.GovtSchemeName = ReadString(compactDetails, "gsn");

                if (string.IsNullOrWhiteSpace(result.WantsSelfEmployment)) result.WantsSelfEmployment = ReadString(details, "wantsSelfEmployment");
                if (string.IsNullOrWhiteSpace(result.WantsSelfEmployment)) result.WantsSelfEmployment = ReadString(compactDetails, "wse");

                if (string.IsNullOrWhiteSpace(result.ExpectedBusinessType)) result.ExpectedBusinessType = ReadString(details, "expectedBusinessType");
                if (string.IsNullOrWhiteSpace(result.ExpectedBusinessType)) result.ExpectedBusinessType = ReadString(compactDetails, "eb");

                if (string.IsNullOrWhiteSpace(result.SkillsJson))
                {
                    var skillsRaw = ReadString(details, "skills");
                    if (string.IsNullOrWhiteSpace(skillsRaw)) skillsRaw = ReadString(compactDetails, "sk");
                    result.SkillsJson = skillsRaw;
                }

                if (string.IsNullOrWhiteSpace(result.RequiredSupportJson))
                {
                    var supportRaw = ReadString(details, "requiredSupport");
                    if (string.IsNullOrWhiteSpace(supportRaw)) supportRaw = ReadString(compactDetails, "rsup");
                    result.RequiredSupportJson = supportRaw;
                }

                var proofDateRaw = ReadString(details, "proofDocumentDate");
                if (string.IsNullOrWhiteSpace(proofDateRaw)) proofDateRaw = ReadString(compactDetails, "pd");
                DateTime proofDate;
                if (!result.ProofDocumentDate.HasValue)
                {
                    result.ProofDocumentDate = DateTime.TryParse(proofDateRaw, out proofDate) ? proofDate : (DateTime?)null;
                }
            }
            catch
            {
            }

            return result;
        }

        private static IDictionary<string, object> GetObject(IDictionary<string, object> source, string key)
        {
            if (source == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            object raw;
            if (!source.TryGetValue(key, out raw) || raw == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (raw is JObject) return ((JObject)raw).ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var dict = raw as IDictionary<string, object>;
            return dict ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        private static string ReadString(IDictionary<string, object> source, string key)
        {
            if (source == null) return string.Empty;
            object value;
            if (!source.TryGetValue(key, out value) || value == null) return string.Empty;
            var s = value.ToString();
            return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();
        }

        private static string ReadString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        // ----------------------------------------------------------------
        //  Replace Schemes
        // ----------------------------------------------------------------
        public SWROperationResult<object> ReplaceSchemes(int regId, IList<string> schemePairs)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_REPLACE_SWR_SCHEMES", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    var p = cmd.Parameters.Add("P_REG_ID", OracleDbType.Decimal);
                    p.Value = regId;

                    // Pass schemes as comma-delimited CSV: "CODE|Y,CODE2|N,..."
                    // Procedure SP_WCWC_REPLACE_SWR_SCHEMES parses this internally.
                    var csvParam = cmd.Parameters.Add("P_SCHEMES_CSV", OracleDbType.Varchar2, 32767);
                    csvParam.Value = schemePairs != null && schemePairs.Count > 0
                        ? string.Join(",", schemePairs)
                        : string.Empty;

                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    return statusCode == "SUCCESS"
                        ? new SWROperationResult<object> { Success = true, Message = message }
                        : Fail<object>(message, statusCode);
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        public SWROperationResult<bool> AadhaarExists(string aadhaarNumber, int? excludeRegId)
        {
            var normalizedAadhaar = ReadString(aadhaarNumber);
            if (string.IsNullOrWhiteSpace(normalizedAadhaar))
            {
                return new SWROperationResult<bool> { Success = true, Data = false, Message = "Aadhaar not provided" };
            }

            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd = new OracleCommand(@"
SELECT COUNT(1)
  FROM WCWC.TXN_WCWC_SINGLE_WOMEN_REG
 WHERE AADHAAR_NUMBER = :P_AADHAAR_NUMBER
   AND (:P_EXCLUDE_REG_ID IS NULL OR REG_ID <> :P_EXCLUDE_REG_ID)", conn))
                {
                    conn.Open();
                    AddNullable(cmd, "P_AADHAAR_NUMBER", OracleDbType.Varchar2, normalizedAadhaar);
                    AddNullable(cmd, "P_EXCLUDE_REG_ID", OracleDbType.Decimal, excludeRegId);

                    var countObj = cmd.ExecuteScalar();
                    var count = 0;
                    if (countObj != null && countObj != DBNull.Value)
                    {
                        if (countObj is OracleDecimal)
                        {
                            count = (int)((OracleDecimal)countObj).Value;
                        }
                        else
                        {
                            int.TryParse(countObj.ToString(), out count);
                        }
                    }

                    return new SWROperationResult<bool>
                    {
                        Success = true,
                        Data = count > 0,
                        Message = count > 0 ? "Duplicate Aadhaar found" : "Aadhaar is unique"
                    };
                }
            }
            catch (Exception ex)
            {
                return Fail<bool>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Get Registration
        // ----------------------------------------------------------------
        public SWROperationResult<object> GetRegistration(int? regId, string regNumber)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_GET_SINGLE_WOMEN_REG", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    AddNullable(cmd, "P_REG_ID",     OracleDbType.Decimal, regId);
                    AddNullable(cmd, "P_REG_NUMBER",  OracleDbType.Varchar2, regNumber);

                    AddOutCursor(cmd, "P_OUT_CURSOR");
                    AddOutCursor(cmd, "P_OUT_SCH_CURSOR");
                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    if (statusCode != "SUCCESS")
                        return Fail<object>(message, statusCode == "NOT_FOUND" ? "REG_NOT_FOUND" : statusCode);

                    var reg     = ReadSingleRow((OracleRefCursor)((OracleParameter)cmd.Parameters["P_OUT_CURSOR"]).Value);
                    var schemes = ReadRows((OracleRefCursor)((OracleParameter)cmd.Parameters["P_OUT_SCH_CURSOR"]).Value);

                    if (reg == null) return Fail<object>("Registration not found", "REG_NOT_FOUND");

                    reg["schemes"] = schemes;

                    return new SWROperationResult<object> { Success = true, Message = message, Data = reg };
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Search
        // ----------------------------------------------------------------
        public SWROperationResult<object> Search(string searchText, string status, string applicationMode)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_SEARCH_SINGLE_WOMEN", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    AddNullable(cmd, "P_SEARCH_TEXT",      OracleDbType.NVarchar2, searchText);
                    AddNullable(cmd, "P_STATUS",           OracleDbType.Varchar2,  status);
                    AddNullable(cmd, "P_APPLICATION_MODE", OracleDbType.Varchar2,  applicationMode);

                    AddOutCursor(cmd, "P_OUT_CURSOR");
                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    if (statusCode != "SUCCESS")
                        return Fail<object>(message, statusCode);

                    var rows = ReadRows((OracleRefCursor)((OracleParameter)cmd.Parameters["P_OUT_CURSOR"]).Value);
                    return new SWROperationResult<object> { Success = true, Message = message, Data = rows };
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Update Status
        // ----------------------------------------------------------------
        public SWROperationResult<object> UpdateStatus(int regId, string status, string remarks, int? operatorId)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_UPDATE_SWR_STATUS", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    var p = cmd.Parameters.Add("P_REG_ID", OracleDbType.Decimal);
                    p.Value = regId;
                    AddNullable(cmd, "P_STATUS",      OracleDbType.Varchar2,  status);
                    AddNullable(cmd, "P_REMARKS",     OracleDbType.NVarchar2, remarks);
                    AddNullable(cmd, "P_OPERATOR_ID", OracleDbType.Decimal,   operatorId);

                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    return statusCode == "SUCCESS"
                        ? new SWROperationResult<object> { Success = true, Message = message }
                        : Fail<object>(message, statusCode);
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        public SWROperationResult<object> DeleteRegistration(int regId)
        {
            OracleTransaction transaction = null;
            try
            {
                using (var conn = _connectionFactory.Create())
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // Optional metadata table: ignore if this deployment has not created it.
                    try
                    {
                        using (var docDelete = new OracleCommand("DELETE FROM WCWC.TXN_WCWC_SWR_DOCUMENTS WHERE REG_ID = :P_REG_ID", conn))
                        {
                            docDelete.Transaction = transaction;
                            AddNullable(docDelete, "P_REG_ID", OracleDbType.Decimal, regId);
                            docDelete.ExecuteNonQuery();
                        }
                    }
                    catch (OracleException ex)
                    {
                        if (ex.Number != 942)
                        {
                            throw;
                        }
                    }

                    using (var schemeDelete = new OracleCommand("DELETE FROM WCWC.TXN_WCWC_SWR_SCHEMES WHERE REG_ID = :P_REG_ID", conn))
                    {
                        schemeDelete.Transaction = transaction;
                        AddNullable(schemeDelete, "P_REG_ID", OracleDbType.Decimal, regId);
                        schemeDelete.ExecuteNonQuery();
                    }

                    int deletedRows;
                    using (var regDelete = new OracleCommand("DELETE FROM WCWC.TXN_WCWC_SINGLE_WOMEN_REG WHERE REG_ID = :P_REG_ID", conn))
                    {
                        regDelete.Transaction = transaction;
                        AddNullable(regDelete, "P_REG_ID", OracleDbType.Decimal, regId);
                        deletedRows = regDelete.ExecuteNonQuery();
                    }

                    if (deletedRows <= 0)
                    {
                        transaction.Rollback();
                        return Fail<object>("Registration not found for deletion", "REG_NOT_FOUND");
                    }

                    transaction.Commit();
                    return new SWROperationResult<object> { Success = true, Message = "Registration deleted permanently" };
                }
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
                return Fail<object>("Database delete error: " + ex.Message, "DELETE_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Resolve identifier (number or numeric ID)
        // ----------------------------------------------------------------
        public SWROperationResult<object> ResolveRegistration(string identifier)
        {
            int id;
            if (int.TryParse(identifier, out id))
            {
                var r = GetRegistration(id, null);
                return r;
            }
            else
            {
                return GetRegistration(null, identifier);
            }
        }

        // ----------------------------------------------------------------
        //  Helpers
        // ----------------------------------------------------------------
        private static void AddNullable(OracleCommand cmd, string name, OracleDbType type, object value)
        {
            var p = cmd.Parameters.Add(name, type);
            p.Value = value ?? (object)DBNull.Value;
        }

        private static void AddOut(OracleCommand cmd, string name, OracleDbType type, int size = 200)
        {
            var p = cmd.Parameters.Add(name, type, size);
            p.Direction = ParameterDirection.Output;
        }

        private static void AddOutCursor(OracleCommand cmd, string name)
        {
            var p = cmd.Parameters.Add(name, OracleDbType.RefCursor);
            p.Direction = ParameterDirection.Output;
        }

        private static string GetOutString(OracleCommand cmd, string name)
        {
            var val = ((OracleParameter)cmd.Parameters[name]).Value;
            if (val == null || val is DBNull) return null;
            return val.ToString();
        }

        private static int GetOutInt(OracleCommand cmd, string name)
        {
            var val = ((OracleParameter)cmd.Parameters[name]).Value;
            if (val == null || val is DBNull) return 0;
            if (val is OracleDecimal od) return (int)od.Value;
            int result;
            return int.TryParse(val.ToString(), out result) ? result : 0;
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            DateTime dt;
            return DateTime.TryParse(value, out dt) ? dt : (DateTime?)null;
        }

        private static decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            decimal d;
            return decimal.TryParse(value, out d) ? d : (decimal?)null;
        }

        private static string TruncateText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var trimmed = value.Trim();
            if (trimmed.Length <= maxLength) return trimmed;
            return trimmed.Substring(0, maxLength);
        }

        private static SWROperationResult<T> Fail<T>(string message, string errorCode)
        {
            return new SWROperationResult<T> { Success = false, Message = message, ErrorCode = errorCode };
        }

        private static Dictionary<string, object> ReadSingleRow(OracleRefCursor cursor)
        {
            if (cursor == null) return null;
            using (var reader = cursor.GetDataReader())
            {
                if (!reader.Read()) return null;
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i).ToLowerInvariant()] = reader.IsDBNull(i) ? null : NormalizeDbValue(reader.GetValue(i));
                return row;
            }
        }

        private static List<Dictionary<string, object>> ReadRows(OracleRefCursor cursor)
        {
            var list = new List<Dictionary<string, object>>();
            if (cursor == null) return list;
            using (var reader = cursor.GetDataReader())
            {
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i).ToLowerInvariant()] = reader.IsDBNull(i) ? null : NormalizeDbValue(reader.GetValue(i));
                    list.Add(row);
                }
            }
            return list;
        }

        private static object NormalizeDbValue(object value)
        {
            if (value == null || value is DBNull)
            {
                return null;
            }

            var nclob = value as OracleClob;
            if (nclob != null)
            {
                return nclob.IsNull ? null : nclob.Value;
            }

            return value;
        }
    }
}
