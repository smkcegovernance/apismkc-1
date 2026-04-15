using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface IWcwcDisabilityRepository
    {
        WcwcOperationResult<WcwcRegistrationSaveData> SaveRegistration(WcwcRegistrationUpsertRequest request, int? registrationId);
        WcwcOperationResult<object> ReplaceDisabilities(int registrationId, IEnumerable<int> disabilityTypeIds);
        WcwcOperationResult<object> ReplaceDevices(int registrationId, IEnumerable<int> assistiveDeviceIds);
        WcwcOperationResult<object> UpsertDocument(int registrationId, string documentCode, string fileName, string mimeType, long fileSize, int? uploadedByOperatorId);
        WcwcOperationResult<object> GetRegistration(int? registrationId, string registrationNumber);
        WcwcOperationResult<object> SearchRegistrations(string searchText, string status, string applicationMode, int? operatorUserId);
        WcwcOperationResult<object> UpdateStatus(int registrationId, string status, int? operatorUserId, string remarks);
        WcwcOperationResult<object> OperatorLogin(string userName, string passwordHash);
        WcwcOperationResult<object> ResolveRegistration(string identifier);
    }

    public class WcwcDisabilityRepository : IWcwcDisabilityRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        public WcwcDisabilityRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public WcwcOperationResult<WcwcRegistrationSaveData> SaveRegistration(WcwcRegistrationUpsertRequest request, int? registrationId)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                using (var command = new OracleCommand("SP_WCWC_SAVE_REGISTRATION", connection))
                {
                    connection.Open();
                    InitializeCommand(command);

                    AddNullable(command, "P_REGISTRATION_ID", OracleDbType.Decimal, registrationId);
                    AddNullable(command, "P_APPLICATION_MODE", OracleDbType.Varchar2, request.ApplicationMode);
                    AddNullable(command, "P_SUBMISSION_CHANNEL", OracleDbType.Varchar2, request.SubmissionChannel);
                    AddNullable(command, "P_SURNAME", OracleDbType.NVarchar2, request.Surname);
                    AddNullable(command, "P_FIRST_NAME", OracleDbType.NVarchar2, request.FirstName);
                    AddNullable(command, "P_FATHER_NAME", OracleDbType.NVarchar2, request.FatherName);
                    AddNullable(command, "P_MOTHER_NAME", OracleDbType.NVarchar2, request.MotherName);
                    AddNullable(command, "P_EDUCATION", OracleDbType.NVarchar2, request.Education);
                    AddNullable(command, "P_AADHAAR_NUMBER", OracleDbType.Varchar2, request.AadhaarNumber);
                    AddNullable(command, "P_DOB", OracleDbType.Date, ParseDate(request.Dob));
                    AddNullable(command, "P_MARITAL_STATUS", OracleDbType.NVarchar2, request.MaritalStatus);
                    AddNullable(command, "P_RELIGION", OracleDbType.NVarchar2, request.Religion);
                    AddNullable(command, "P_CASTE_NAME", OracleDbType.NVarchar2, request.Caste);
                    AddNullable(command, "P_FAMILY_RELATION", OracleDbType.NVarchar2, request.FamilyRelation);
                    AddNullable(command, "P_FULL_ADDRESS", OracleDbType.NVarchar2, request.FullAddress);
                    AddNullable(command, "P_WARD_NUMBER", OracleDbType.NVarchar2, request.WardNumber);
                    AddNullable(command, "P_PRABHAG_SAMITI", OracleDbType.NVarchar2, request.PrabhagSamiti);
                    AddNullable(command, "P_UPHC", OracleDbType.NVarchar2, request.Uphc);
                    AddNullable(command, "P_PINCODE", OracleDbType.Varchar2, request.Pincode);
                    AddNullable(command, "P_CONSTITUENCY", OracleDbType.NVarchar2, request.Constituency);
                    AddNullable(command, "P_MOBILE_NUMBER", OracleDbType.Varchar2, request.MobileNumber);
                    AddNullable(command, "P_ALTERNATE_PHONE", OracleDbType.Varchar2, request.AlternatePhone);
                    AddNullable(command, "P_BANK_NAME", OracleDbType.NVarchar2, request.BankName);
                    AddNullable(command, "P_BRANCH_NAME", OracleDbType.NVarchar2, request.BranchName);
                    AddNullable(command, "P_ACCOUNT_NUMBER", OracleDbType.Varchar2, request.AccountNumber);
                    AddNullable(command, "P_IFSC_CODE", OracleDbType.Varchar2, request.IfscCode);
                    AddNullable(command, "P_BPL_NUMBER", OracleDbType.NVarchar2, request.BplNumber);
                    AddNullable(command, "P_BPL_YEAR", OracleDbType.Varchar2, request.BplYear);
                    AddNullable(command, "P_HAS_CERTIFICATE", OracleDbType.Char, request.HasCertificate);
                    AddNullable(command, "P_CERTIFICATE_NUMBER", OracleDbType.NVarchar2, request.CertificateNumber);
                    AddNullable(command, "P_CERTIFICATE_DATE", OracleDbType.Date, ParseDate(request.CertificateDate));
                    AddNullable(command, "P_CERTIFICATE_TYPE", OracleDbType.NVarchar2, request.CertificateType);
                    AddNullable(command, "P_DISABILITY_PERCENTAGE", OracleDbType.Decimal, ParseDecimal(request.DisabilityPercentage));
                    AddNullable(command, "P_HAS_UDID", OracleDbType.Char, request.HasUdid);
                    AddNullable(command, "P_UDID_NUMBER", OracleDbType.NVarchar2, request.UdidNumber);
                    AddNullable(command, "P_HAS_ST_PASS", OracleDbType.Char, request.HasStPass);
                    AddNullable(command, "P_ST_PASS_NUMBER", OracleDbType.NVarchar2, request.StPassNumber);
                    AddNullable(command, "P_HAS_RAILWAY_PASS", OracleDbType.Char, request.HasRailwayPass);
                    AddNullable(command, "P_RAILWAY_PASS_NUMBER", OracleDbType.NVarchar2, request.RailwayPassNumber);
                    AddNullable(command, "P_HAS_MSRTC_PASS", OracleDbType.Char, request.HasMsrtcPass);
                    AddNullable(command, "P_MSRTC_PASS_NUMBER", OracleDbType.NVarchar2, request.MsrtcPassNumber);
                    AddNullable(command, "P_IS_EMPLOYED", OracleDbType.Char, request.IsEmployed);
                    AddNullable(command, "P_EMPLOYMENT_TYPE", OracleDbType.NVarchar2, request.EmploymentType);
                    AddNullable(command, "P_OCCUPATION", OracleDbType.NVarchar2, request.Occupation);
                    AddNullable(command, "P_HAS_GOVT_BENEFIT", OracleDbType.Char, request.HasGovtBenefit);
                    AddNullable(command, "P_GOVT_BENEFIT_SCHEME", OracleDbType.NVarchar2, request.GovtBenefitScheme);
                    AddNullable(command, "P_HAS_MC_BENEFIT", OracleDbType.Char, request.HasMcBenefit);
                    AddNullable(command, "P_MC_BENEFIT_DETAILS", OracleDbType.NVarchar2, request.McBenefitDetails);
                    AddNullable(command, "P_HAS_SGN_PENSION", OracleDbType.Char, request.HasSgnPension);
                    AddNullable(command, "P_HAS_OWN_HOUSE", OracleDbType.Char, request.HasOwnHouse);
                    AddNullable(command, "P_WANTS_HOUSING_BENEFIT", OracleDbType.Char, request.WantsHousingBenefit);
                    AddNullable(command, "P_HAS_OWN_LAND", OracleDbType.Char, request.HasOwnLand);
                    AddNullable(command, "P_HAS_GUARDIANSHIP", OracleDbType.Char, request.HasGuardianship);
                    AddNullable(command, "P_GUARDIAN_NAME", OracleDbType.NVarchar2, request.GuardianName);
                    AddNullable(command, "P_GUARDIAN_ADDRESS", OracleDbType.NVarchar2, request.GuardianAddress);
                    AddNullable(command, "P_GUARDIAN_PHONE", OracleDbType.Varchar2, request.GuardianPhone);
                    AddNullable(command, "P_NEEDS_ASSISTIVE_DEVICE", OracleDbType.Char, request.NeedsAssistiveDevice);
                    AddNullable(command, "P_DOCUMENTS_SUBMITTED", OracleDbType.Char, request.DocumentsSubmitted);
                    AddNullable(command, "P_TERMS_ACCEPTED", OracleDbType.Char, request.TermsAccepted ? "Y" : "N");
                    AddNullable(command, "P_APPLICANT_SIGN_DATE", OracleDbType.Date, ParseDate(request.ApplicantSignDate));
                    AddNullable(command, "P_SURVEYOR_NAME", OracleDbType.NVarchar2, request.SurveyorName);
                    AddNullable(command, "P_SURVEYOR_DESIGNATION", OracleDbType.NVarchar2, request.SurveyorDesignation);
                    AddNullable(command, "P_SURVEYOR_MOBILE", OracleDbType.Varchar2, request.SurveyorMobile);
                    AddNullable(command, "P_PUBLIC_SUBMITTER_NAME", OracleDbType.NVarchar2, request.PublicSubmitterName);
                    AddNullable(command, "P_PUBLIC_SUBMITTER_MOBILE", OracleDbType.Varchar2, request.PublicSubmitterMobile);
                    AddNullable(command, "P_PUBLIC_CENTER_NAME", OracleDbType.NVarchar2, request.PublicCenterName);
                    AddNullable(command, "P_MANUAL_APPLICATION_NO", OracleDbType.NVarchar2, request.ManualApplicationNo);
                    AddNullable(command, "P_OPERATOR_USER_ID", OracleDbType.Decimal, request.OperatorUserId);
                    AddNullable(command, "P_CREATED_BY", OracleDbType.Varchar2, request.CreatedBy);

                    command.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_REGISTRATION_ID", OracleDbType.Decimal).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_REGISTRATION_NO", OracleDbType.Varchar2, 100).Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    var success = ConvertToInt(command.Parameters["O_SUCCESS"].Value) == 1;
                    var message = Convert.ToString(command.Parameters["O_MESSAGE"].Value);
                    if (!success)
                    {
                        return new WcwcOperationResult<WcwcRegistrationSaveData> { Success = false, Message = message, ErrorCode = "SAVE_FAILED" };
                    }

                    return new WcwcOperationResult<WcwcRegistrationSaveData>
                    {
                        Success = true,
                        Message = message,
                        Data = new WcwcRegistrationSaveData
                        {
                            RegistrationId = ConvertToInt(command.Parameters["O_REGISTRATION_ID"].Value),
                            RegistrationNumber = Convert.ToString(command.Parameters["O_REGISTRATION_NO"].Value)
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new WcwcOperationResult<WcwcRegistrationSaveData> { Success = false, Message = ex.Message, ErrorCode = "SAVE_EXCEPTION" };
            }
        }

        public WcwcOperationResult<object> ReplaceDisabilities(int registrationId, IEnumerable<int> disabilityTypeIds)
        {
            return ExecuteListProcedure("SP_WCWC_REPLACE_REG_DISABILITIES", "P_DISABILITY_ID_LIST", registrationId, disabilityTypeIds);
        }

        public WcwcOperationResult<object> ReplaceDevices(int registrationId, IEnumerable<int> assistiveDeviceIds)
        {
            return ExecuteListProcedure("SP_WCWC_REPLACE_REG_DEVICES", "P_DEVICE_ID_LIST", registrationId, assistiveDeviceIds);
        }

        public WcwcOperationResult<object> UpsertDocument(int registrationId, string documentCode, string fileName, string mimeType, long fileSize, int? uploadedByOperatorId)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                using (var command = new OracleCommand("SP_WCWC_UPSERT_REG_DOCUMENT", connection))
                {
                    connection.Open();
                    InitializeCommand(command);
                    AddNullable(command, "P_REGISTRATION_ID", OracleDbType.Decimal, registrationId);
                    AddNullable(command, "P_DOCUMENT_CODE", OracleDbType.Varchar2, documentCode);
                    AddNullable(command, "P_FILE_NAME", OracleDbType.NVarchar2, fileName);
                    AddNullable(command, "P_MIME_TYPE", OracleDbType.Varchar2, mimeType);
                    AddNullable(command, "P_FILE_SIZE", OracleDbType.Decimal, fileSize);
                    command.Parameters.Add("P_FILE_BLOB", OracleDbType.Blob).Value = DBNull.Value;
                    AddNullable(command, "P_UPLOADED_BY_OPERATOR_ID", OracleDbType.Decimal, uploadedByOperatorId);
                    command.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    return new WcwcOperationResult<object>
                    {
                        Success = ConvertToInt(command.Parameters["O_SUCCESS"].Value) == 1,
                        Message = Convert.ToString(command.Parameters["O_MESSAGE"].Value)
                    };
                }
            }
            catch (Exception ex)
            {
                return new WcwcOperationResult<object> { Success = false, Message = ex.Message, ErrorCode = "DOCUMENT_UPSERT_EXCEPTION" };
            }
        }

        public WcwcOperationResult<object> GetRegistration(int? registrationId, string registrationNumber)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                using (var command = new OracleCommand("SP_WCWC_GET_REGISTRATION", connection))
                {
                    connection.Open();
                    InitializeCommand(command);
                    AddNullable(command, "P_REGISTRATION_ID", OracleDbType.Decimal, registrationId);
                    AddNullable(command, "P_REGISTRATION_NO", OracleDbType.Varchar2, registrationNumber);
                    command.Parameters.Add("O_REGISTRATION_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_DISABILITY_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_DEVICE_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_DOCUMENT_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    var data = new Dictionary<string, object>
                    {
                        { "registration", ReadRefCursor(command.Parameters["O_REGISTRATION_DATA"].Value) },
                        { "disabilities", ReadRefCursor(command.Parameters["O_DISABILITY_DATA"].Value) },
                        { "devices", ReadRefCursor(command.Parameters["O_DEVICE_DATA"].Value) },
                        { "documents", ReadRefCursor(command.Parameters["O_DOCUMENT_DATA"].Value) }
                    };

                    return new WcwcOperationResult<object> { Success = true, Message = "Registration retrieved successfully", Data = data };
                }
            }
            catch (Exception ex)
            {
                return new WcwcOperationResult<object> { Success = false, Message = ex.Message, ErrorCode = "GET_REGISTRATION_EXCEPTION" };
            }
        }

        public WcwcOperationResult<object> SearchRegistrations(string searchText, string status, string applicationMode, int? operatorUserId)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                using (var command = new OracleCommand("SP_WCWC_SEARCH_REGISTRATIONS", connection))
                {
                    connection.Open();
                    InitializeCommand(command);
                    AddNullable(command, "P_SEARCH_TEXT", OracleDbType.NVarchar2, searchText);
                    AddNullable(command, "P_STATUS", OracleDbType.Varchar2, status);
                    AddNullable(command, "P_APPLICATION_MODE", OracleDbType.Varchar2, applicationMode);
                    AddNullable(command, "P_OPERATOR_USER_ID", OracleDbType.Decimal, operatorUserId);
                    command.Parameters.Add("O_RESULTS", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();
                    return new WcwcOperationResult<object>
                    {
                        Success = true,
                        Message = "Registrations retrieved successfully",
                        Data = ReadRefCursor(command.Parameters["O_RESULTS"].Value)
                    };
                }
            }
            catch (Exception ex)
            {
                return new WcwcOperationResult<object> { Success = false, Message = ex.Message, ErrorCode = "SEARCH_EXCEPTION" };
            }
        }

        public WcwcOperationResult<object> UpdateStatus(int registrationId, string status, int? operatorUserId, string remarks)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                using (var command = new OracleCommand("SP_WCWC_UPDATE_REG_STATUS", connection))
                {
                    connection.Open();
                    InitializeCommand(command);
                    AddNullable(command, "P_REGISTRATION_ID", OracleDbType.Decimal, registrationId);
                    AddNullable(command, "P_NEW_STATUS", OracleDbType.Varchar2, status);
                    AddNullable(command, "P_CHANGED_BY_OPERATOR_ID", OracleDbType.Decimal, operatorUserId);
                    AddNullable(command, "P_REMARKS", OracleDbType.NVarchar2, remarks);
                    command.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();
                    return new WcwcOperationResult<object>
                    {
                        Success = ConvertToInt(command.Parameters["O_SUCCESS"].Value) == 1,
                        Message = Convert.ToString(command.Parameters["O_MESSAGE"].Value)
                    };
                }
            }
            catch (Exception ex)
            {
                return new WcwcOperationResult<object> { Success = false, Message = ex.Message, ErrorCode = "STATUS_EXCEPTION" };
            }
        }

        public WcwcOperationResult<object> OperatorLogin(string userName, string passwordHash)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                using (var command = new OracleCommand("SP_WCWC_OPERATOR_LOGIN", connection))
                {
                    connection.Open();
                    InitializeCommand(command);
                    AddNullable(command, "P_USER_NAME", OracleDbType.Varchar2, userName);
                    AddNullable(command, "P_PASSWORD_HASH", OracleDbType.Varchar2, passwordHash);
                    command.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_OPERATOR_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();
                    return new WcwcOperationResult<object>
                    {
                        Success = ConvertToInt(command.Parameters["O_SUCCESS"].Value) == 1,
                        Message = Convert.ToString(command.Parameters["O_MESSAGE"].Value),
                        Data = ReadRefCursor(command.Parameters["O_OPERATOR_DATA"].Value)
                    };
                }
            }
            catch (Exception ex)
            {
                return new WcwcOperationResult<object> { Success = false, Message = ex.Message, ErrorCode = "LOGIN_EXCEPTION" };
            }
        }

        public WcwcOperationResult<object> ResolveRegistration(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return new WcwcOperationResult<object> { Success = false, Message = "Registration identifier is required", ErrorCode = "IDENTIFIER_REQUIRED" };
            }

            int registrationId;
            if (int.TryParse(identifier, out registrationId))
            {
                return GetRegistration(registrationId, null);
            }

            return GetRegistration(null, identifier);
        }

        private WcwcOperationResult<object> ExecuteListProcedure(string procedureName, string listParameterName, int registrationId, IEnumerable<int> ids)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                using (var command = new OracleCommand(procedureName, connection))
                {
                    connection.Open();
                    InitializeCommand(command);
                    AddNullable(command, "P_REGISTRATION_ID", OracleDbType.Decimal, registrationId);
                    AddNullable(command, listParameterName, OracleDbType.Varchar2, ids == null ? null : string.Join(",", ids));
                    command.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                    command.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    return new WcwcOperationResult<object>
                    {
                        Success = ConvertToInt(command.Parameters["O_SUCCESS"].Value) == 1,
                        Message = Convert.ToString(command.Parameters["O_MESSAGE"].Value)
                    };
                }
            }
            catch (Exception ex)
            {
                return new WcwcOperationResult<object> { Success = false, Message = ex.Message, ErrorCode = "LIST_UPDATE_EXCEPTION" };
            }
        }

        private static List<Dictionary<string, object>> ReadRefCursor(object cursorValue)
        {
            var result = new List<Dictionary<string, object>>();
            var refCursor = cursorValue as OracleRefCursor;
            if (refCursor == null)
            {
                return result;
            }

            using (var reader = refCursor.GetDataReader())
            {
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    }

                    result.Add(row);
                }
            }

            return result;
        }

        private static void InitializeCommand(OracleCommand command)
        {
            command.CommandType = CommandType.StoredProcedure;
            command.BindByName = true;
        }

        private static void AddNullable(OracleCommand command, string name, OracleDbType dbType, object value)
        {
            var parameter = command.Parameters.Add(name, dbType);
            if (value == null)
            {
                parameter.Value = DBNull.Value;
                return;
            }

            var stringValue = value as string;
            if (stringValue != null)
            {
                var normalizedValue = stringValue.Trim();
                if (normalizedValue.Length == 0)
                {
                    parameter.Value = DBNull.Value;
                    return;
                }

                parameter.Value = normalizedValue;
                if (dbType == OracleDbType.NVarchar2 || dbType == OracleDbType.Varchar2)
                {
                    parameter.Size = normalizedValue.Length;
                }

                return;
            }

            parameter.Value = value;
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime parsed;
            return DateTime.TryParse(value, out parsed) ? parsed : (DateTime?)null;
        }

        private static decimal? ParseDecimal(string value)
        {
            decimal parsed;
            return decimal.TryParse(value, out parsed) ? parsed : (decimal?)null;
        }

        private static int ConvertToInt(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return 0;
            }

            if (value is OracleDecimal)
            {
                return ((OracleDecimal)value).ToInt32();
            }

            return Convert.ToInt32(value);
        }
    }
}