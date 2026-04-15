using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models;
using SmkcApi.Models.WomenChildWelfare;

namespace SmkcApi.Repositories.WomenChildWelfare
{
    public class WomenChildWelfareRepository : IWomenChildWelfareRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        public WomenChildWelfareRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException("connectionFactory");
        }

        public WcwcRegistrationSubmitResponse SaveRegistration(WcwcRegistrationUpsertRequest request)
        {
            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand("SP_WCWC_SAVE_REGISTRATION", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;

                cmd.Parameters.Add("P_REGISTRATION_ID", OracleDbType.Int32).Value = NullableInt(request.RegistrationId);
                cmd.Parameters.Add("P_APPLICATION_MODE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.ApplicationMode);
                cmd.Parameters.Add("P_SUBMISSION_CHANNEL", OracleDbType.Varchar2).Value = ValueOrDbNull(request.SubmissionChannel);
                cmd.Parameters.Add("P_SURNAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Surname);
                cmd.Parameters.Add("P_FIRST_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.FirstName);
                cmd.Parameters.Add("P_FATHER_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.FatherName);
                cmd.Parameters.Add("P_MOTHER_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.MotherName);
                cmd.Parameters.Add("P_EDUCATION", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Education);
                cmd.Parameters.Add("P_AADHAAR_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.AadhaarNumber);
                cmd.Parameters.Add("P_DOB", OracleDbType.Date).Value = NullableDate(ParseDate(request.Dob));
                cmd.Parameters.Add("P_MARITAL_STATUS", OracleDbType.Varchar2).Value = ValueOrDbNull(request.MaritalStatus);
                cmd.Parameters.Add("P_RELIGION", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Religion);
                cmd.Parameters.Add("P_CASTE_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Caste);
                cmd.Parameters.Add("P_FAMILY_RELATION", OracleDbType.Varchar2).Value = ValueOrDbNull(request.FamilyRelation);
                cmd.Parameters.Add("P_FULL_ADDRESS", OracleDbType.Clob).Value = ValueOrDbNull(request.FullAddress);
                cmd.Parameters.Add("P_WARD_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.WardNumber);
                cmd.Parameters.Add("P_PRABHAG_SAMITI", OracleDbType.Varchar2).Value = ValueOrDbNull(request.PrabhagSamiti);
                cmd.Parameters.Add("P_UPHC", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Uphc);
                cmd.Parameters.Add("P_PINCODE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Pincode);
                cmd.Parameters.Add("P_CONSTITUENCY", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Constituency);
                cmd.Parameters.Add("P_MOBILE_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.MobileNumber);
                cmd.Parameters.Add("P_ALTERNATE_PHONE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.AlternatePhone);
                cmd.Parameters.Add("P_BANK_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.BankName);
                cmd.Parameters.Add("P_BRANCH_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.BranchName);
                cmd.Parameters.Add("P_ACCOUNT_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.AccountNumber);
                cmd.Parameters.Add("P_IFSC_CODE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.IfscCode);
                cmd.Parameters.Add("P_BPL_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.BplNumber);
                cmd.Parameters.Add("P_BPL_YEAR", OracleDbType.Varchar2).Value = ValueOrDbNull(request.BplYear);
                cmd.Parameters.Add("P_HAS_CERTIFICATE", OracleDbType.Char).Value = ValueOrDbNull(request.HasCertificate);
                cmd.Parameters.Add("P_CERTIFICATE_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.CertificateNumber);
                cmd.Parameters.Add("P_CERTIFICATE_DATE", OracleDbType.Date).Value = NullableDate(ParseDate(request.CertificateDate));
                cmd.Parameters.Add("P_CERTIFICATE_TYPE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.CertificateType);
                cmd.Parameters.Add("P_DISABILITY_PERCENTAGE", OracleDbType.Decimal).Value = NullableDecimal(ParseDecimal(request.DisabilityPercentage));
                cmd.Parameters.Add("P_HAS_UDID", OracleDbType.Char).Value = ValueOrDbNull(request.HasUdid);
                cmd.Parameters.Add("P_UDID_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.UdidNumber);
                cmd.Parameters.Add("P_HAS_ST_PASS", OracleDbType.Char).Value = ValueOrDbNull(request.HasStPass);
                cmd.Parameters.Add("P_ST_PASS_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.StPassNumber);
                cmd.Parameters.Add("P_HAS_RAILWAY_PASS", OracleDbType.Char).Value = ValueOrDbNull(request.HasRailwayPass);
                cmd.Parameters.Add("P_RAILWAY_PASS_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.RailwayPassNumber);
                cmd.Parameters.Add("P_HAS_MSRTC_PASS", OracleDbType.Char).Value = ValueOrDbNull(request.HasMsrtcPass);
                cmd.Parameters.Add("P_MSRTC_PASS_NUMBER", OracleDbType.Varchar2).Value = ValueOrDbNull(request.MsrtcPassNumber);
                cmd.Parameters.Add("P_IS_EMPLOYED", OracleDbType.Char).Value = ValueOrDbNull(request.IsEmployed);
                cmd.Parameters.Add("P_EMPLOYMENT_TYPE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.EmploymentType);
                cmd.Parameters.Add("P_OCCUPATION", OracleDbType.Varchar2).Value = ValueOrDbNull(request.Occupation);
                cmd.Parameters.Add("P_HAS_GOVT_BENEFIT", OracleDbType.Char).Value = ValueOrDbNull(request.HasGovtBenefit);
                cmd.Parameters.Add("P_GOVT_BENEFIT_SCHEME", OracleDbType.Clob).Value = ValueOrDbNull(request.GovtBenefitScheme);
                cmd.Parameters.Add("P_HAS_MC_BENEFIT", OracleDbType.Char).Value = ValueOrDbNull(request.HasMcBenefit);
                cmd.Parameters.Add("P_MC_BENEFIT_DETAILS", OracleDbType.Clob).Value = ValueOrDbNull(request.McBenefitDetails);
                cmd.Parameters.Add("P_HAS_SGN_PENSION", OracleDbType.Char).Value = ValueOrDbNull(request.HasSgnPension);
                cmd.Parameters.Add("P_HAS_OWN_HOUSE", OracleDbType.Char).Value = ValueOrDbNull(request.HasOwnHouse);
                cmd.Parameters.Add("P_WANTS_HOUSING_BENEFIT", OracleDbType.Char).Value = ValueOrDbNull(request.WantsHousingBenefit);
                cmd.Parameters.Add("P_HAS_OWN_LAND", OracleDbType.Char).Value = ValueOrDbNull(request.HasOwnLand);
                cmd.Parameters.Add("P_HAS_GUARDIANSHIP", OracleDbType.Char).Value = ValueOrDbNull(request.HasGuardianship);
                cmd.Parameters.Add("P_GUARDIAN_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.GuardianName);
                cmd.Parameters.Add("P_GUARDIAN_ADDRESS", OracleDbType.Clob).Value = ValueOrDbNull(request.GuardianAddress);
                cmd.Parameters.Add("P_GUARDIAN_PHONE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.GuardianPhone);
                cmd.Parameters.Add("P_NEEDS_ASSISTIVE_DEVICE", OracleDbType.Char).Value = ValueOrDbNull(request.NeedsAssistiveDevice);
                cmd.Parameters.Add("P_DOCUMENTS_SUBMITTED", OracleDbType.Char).Value = ValueOrDbNull(request.DocumentsSubmitted);
                cmd.Parameters.Add("P_TERMS_ACCEPTED", OracleDbType.Char).Value = request.TermsAccepted ? "Y" : "N";
                cmd.Parameters.Add("P_APPLICANT_SIGN_DATE", OracleDbType.Date).Value = NullableDate(ParseDate(request.ApplicantSignDate));
                cmd.Parameters.Add("P_SURVEYOR_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.SurveyorName);
                cmd.Parameters.Add("P_SURVEYOR_DESIGNATION", OracleDbType.Varchar2).Value = ValueOrDbNull(request.SurveyorDesignation);
                cmd.Parameters.Add("P_SURVEYOR_MOBILE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.SurveyorMobile);
                cmd.Parameters.Add("P_PUBLIC_SUBMITTER_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.PublicSubmitterName);
                cmd.Parameters.Add("P_PUBLIC_SUBMITTER_MOBILE", OracleDbType.Varchar2).Value = ValueOrDbNull(request.PublicSubmitterMobile);
                cmd.Parameters.Add("P_PUBLIC_CENTER_NAME", OracleDbType.Varchar2).Value = ValueOrDbNull(request.PublicCenterName);
                cmd.Parameters.Add("P_MANUAL_APPLICATION_NO", OracleDbType.Varchar2).Value = ValueOrDbNull(request.ManualApplicationNo);
                cmd.Parameters.Add("P_OPERATOR_USER_ID", OracleDbType.Int32).Value = NullableInt(request.OperatorUserId);
                cmd.Parameters.Add("P_CREATED_BY", OracleDbType.Varchar2).Value = ValueOrDbNull(request.CreatedBy);
                cmd.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_REGISTRATION_ID", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_REGISTRATION_NO", OracleDbType.Varchar2, 100).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                EnsureSuccess(cmd.Parameters["O_SUCCESS"].Value, cmd.Parameters["O_MESSAGE"].Value);

                return new WcwcRegistrationSubmitResponse
                {
                    RegistrationId = ConvertToInt(cmd.Parameters["O_REGISTRATION_ID"].Value),
                    RegistrationNumber = Convert.ToString(cmd.Parameters["O_REGISTRATION_NO"].Value),
                    Status = request.RegistrationId.HasValue ? "UPDATED" : "SUBMITTED",
                    ApplicationMode = request.ApplicationMode,
                    SubmissionChannel = request.SubmissionChannel
                };
            }
        }

        public void ReplaceDisabilities(int registrationId, string disabilityIdsCsv)
        {
            ExecuteSimpleProcedure("SP_WCWC_REPLACE_REG_DISABILITIES", registrationId, disabilityIdsCsv, "P_DISABILITY_ID_LIST");
        }

        public void ReplaceAssistiveDevices(int registrationId, string assistiveDeviceIdsCsv)
        {
            ExecuteSimpleProcedure("SP_WCWC_REPLACE_REG_DEVICES", registrationId, assistiveDeviceIdsCsv, "P_DEVICE_ID_LIST");
        }

        public void UpsertDocument(int registrationId, string documentCode, string fileName, string mimeType, long fileSize, int? uploadedByOperatorId)
        {
            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand("SP_WCWC_UPSERT_REG_DOCUMENT", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                cmd.Parameters.Add("P_REGISTRATION_ID", OracleDbType.Int32).Value = registrationId;
                cmd.Parameters.Add("P_DOCUMENT_CODE", OracleDbType.Varchar2).Value = documentCode;
                cmd.Parameters.Add("P_FILE_NAME", OracleDbType.Varchar2).Value = fileName;
                cmd.Parameters.Add("P_MIME_TYPE", OracleDbType.Varchar2).Value = ValueOrDbNull(mimeType);
                cmd.Parameters.Add("P_FILE_SIZE", OracleDbType.Int32).Value = fileSize;
                cmd.Parameters.Add("P_FILE_BLOB", OracleDbType.Blob).Value = DBNull.Value;
                cmd.Parameters.Add("P_UPLOADED_BY_OPERATOR_ID", OracleDbType.Int32).Value = NullableInt(uploadedByOperatorId);
                cmd.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                EnsureSuccess(cmd.Parameters["O_SUCCESS"].Value, cmd.Parameters["O_MESSAGE"].Value);
            }
        }

        public WcwcRegistrationDetailsResponse GetRegistration(string registrationIdOrNumber)
        {
            var registrationId = ResolveRegistrationId(registrationIdOrNumber);

            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand("SP_WCWC_GET_REGISTRATION", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                cmd.Parameters.Add("P_REGISTRATION_ID", OracleDbType.Int32).Value = registrationId;
                cmd.Parameters.Add("P_REGISTRATION_NO", OracleDbType.Varchar2).Value = DBNull.Value;
                cmd.Parameters.Add("O_REGISTRATION_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_DISABILITY_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_DEVICE_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_DOCUMENT_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                var response = new WcwcRegistrationDetailsResponse();
                using (var reader = ((OracleRefCursor)cmd.Parameters["O_REGISTRATION_DATA"].Value).GetDataReader())
                {
                    if (reader.Read())
                    {
                        response.Registration = MapRegistration(reader);
                    }
                }

                using (var reader = ((OracleRefCursor)cmd.Parameters["O_DISABILITY_DATA"].Value).GetDataReader())
                {
                    while (reader.Read())
                    {
                        response.DisabilityTypeIds.Add(Convert.ToInt32(reader["DISABILITY_TYPE_ID"]));
                        response.DisabilityTypes.Add(Convert.ToString(reader["DISABILITY_NAME_EN"]));
                    }
                }

                using (var reader = ((OracleRefCursor)cmd.Parameters["O_DEVICE_DATA"].Value).GetDataReader())
                {
                    while (reader.Read())
                    {
                        response.AssistiveDeviceIds.Add(Convert.ToInt32(reader["ASSISTIVE_DEVICE_ID"]));
                        response.AssistiveDevices.Add(Convert.ToString(reader["DEVICE_NAME"]));
                    }
                }

                using (var reader = ((OracleRefCursor)cmd.Parameters["O_DOCUMENT_DATA"].Value).GetDataReader())
                {
                    while (reader.Read())
                    {
                        response.Documents.Add(new WcwcDocumentRecord
                        {
                            RegistrationId = Convert.ToInt32(reader["REGISTRATION_ID"]),
                            DocumentCode = Convert.ToString(reader["DOCUMENT_CODE"]),
                            FileName = Convert.ToString(reader["FILE_NAME"]),
                            MimeType = Convert.ToString(reader["MIME_TYPE"]),
                            FileSize = Convert.ToInt64(reader["FILE_SIZE"]),
                            UploadedByOperatorId = reader["UPLOADED_BY_OPERATOR_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["UPLOADED_BY_OPERATOR_ID"]),
                            UploadedAt = Convert.ToDateTime(reader["UPLOADED_AT"]),
                            UpdatedAt = Convert.ToDateTime(reader["UPDATED_AT"])
                        });
                    }
                }

                if (response.Registration == null)
                {
                    throw new InvalidOperationException("Registration not found");
                }

                return response;
            }
        }

        public PagedResult<WcwcRegistrationListItem> GetRegistrations(int page, int pageSize, string searchText, string searchField, string status, string applicationMode, int? operatorUserId)
        {
            var result = new PagedResult<WcwcRegistrationListItem>
            {
                PageNumber = page,
                PageSize = pageSize
            };
            var filter = BuildFilter(searchText, searchField, status, applicationMode, operatorUserId);
            var countSql = "SELECT COUNT(*) FROM VW_WCWC_REGISTRATION_SUMMARY" + filter.Sql;
            var dataSql = @"SELECT * FROM (
                                SELECT inner_query.*, ROWNUM rn FROM (
                                    SELECT * FROM VW_WCWC_REGISTRATION_SUMMARY" + filter.Sql + @" ORDER BY CREATED_AT DESC
                                ) inner_query
                                WHERE ROWNUM <= :maxRow
                            ) WHERE rn > :minRow";

            using (var conn = _connectionFactory.CreateWcwc())
            {
                conn.Open();
                using (var countCmd = new OracleCommand(countSql, conn))
                {
                    countCmd.BindByName = true;
                    AddFilterParameters(countCmd, filter);
                    result.TotalCount = ConvertToInt(countCmd.ExecuteScalar());
                }

                result.TotalPages = result.PageSize > 0 ? (int)Math.Ceiling((double)result.TotalCount / result.PageSize) : 0;
                result.HasNextPage = result.PageNumber < result.TotalPages;
                result.HasPreviousPage = result.PageNumber > 1;

                using (var dataCmd = new OracleCommand(dataSql, conn))
                {
                    dataCmd.BindByName = true;
                    AddFilterParameters(dataCmd, filter);
                    dataCmd.Parameters.Add("maxRow", OracleDbType.Int32).Value = page * pageSize;
                    dataCmd.Parameters.Add("minRow", OracleDbType.Int32).Value = (page - 1) * pageSize;

                    using (var reader = dataCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Data.Add(MapSummary(reader));
                        }
                    }
                }
            }

            return result;
        }

        public WcwcOperatorLoginResponse LoginOperator(string userName, string passwordHash)
        {
            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand("SP_WCWC_OPERATOR_LOGIN", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                cmd.Parameters.Add("P_USER_NAME", OracleDbType.Varchar2).Value = userName;
                cmd.Parameters.Add("P_PASSWORD_HASH", OracleDbType.Varchar2).Value = passwordHash;
                cmd.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_OPERATOR_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                EnsureSuccess(cmd.Parameters["O_SUCCESS"].Value, cmd.Parameters["O_MESSAGE"].Value);

                using (var reader = ((OracleRefCursor)cmd.Parameters["O_OPERATOR_DATA"].Value).GetDataReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("Operator not found");
                    }

                    return new WcwcOperatorLoginResponse
                    {
                        OperatorUserId = Convert.ToInt32(reader["OPERATOR_USER_ID"]),
                        OperatorCode = Convert.ToString(reader["OPERATOR_CODE"]),
                        UserName = Convert.ToString(reader["USER_NAME"]),
                        FullName = Convert.ToString(reader["FULL_NAME"]),
                        MobileNo = Convert.ToString(reader["MOBILE_NO"]),
                        RoleCode = Convert.ToString(reader["ROLE_CODE"]),
                        DepartmentName = Convert.ToString(reader["DEPARTMENT_NAME"]),
                        LastLoginAt = reader["LAST_LOGIN_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LAST_LOGIN_AT"])
                    };
                }
            }
        }

        public void UpdateStatus(string registrationIdOrNumber, string newStatus, int? changedByOperatorId, string remarks)
        {
            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand("SP_WCWC_UPDATE_REG_STATUS", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                cmd.Parameters.Add("P_REGISTRATION_ID", OracleDbType.Int32).Value = ResolveRegistrationId(registrationIdOrNumber);
                cmd.Parameters.Add("P_NEW_STATUS", OracleDbType.Varchar2).Value = newStatus;
                cmd.Parameters.Add("P_CHANGED_BY_OPERATOR_ID", OracleDbType.Int32).Value = NullableInt(changedByOperatorId);
                cmd.Parameters.Add("P_REMARKS", OracleDbType.Varchar2).Value = ValueOrDbNull(remarks);
                cmd.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                EnsureSuccess(cmd.Parameters["O_SUCCESS"].Value, cmd.Parameters["O_MESSAGE"].Value);
            }
        }

        public WcwcDocumentRecord GetDocument(string registrationIdOrNumber, string documentCode, string fileName)
        {
            var sql = @"SELECT d.REGISTRATION_ID,
                               r.REGISTRATION_NO,
                               d.DOCUMENT_CODE,
                               d.FILE_NAME,
                               d.MIME_TYPE,
                               d.FILE_SIZE,
                               d.UPLOADED_BY_OPERATOR_ID,
                               d.UPLOADED_AT,
                               d.UPDATED_AT
                          FROM TBL_WCWC_REG_DOCUMENTS d
                          JOIN TBL_WCWC_REGISTRATIONS r ON r.REGISTRATION_ID = d.REGISTRATION_ID
                         WHERE d.REGISTRATION_ID = :registrationId
                           AND d.DOCUMENT_CODE = :documentCode" + (string.IsNullOrWhiteSpace(fileName) ? string.Empty : " AND d.FILE_NAME = :fileName");

            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("registrationId", OracleDbType.Int32).Value = ResolveRegistrationId(registrationIdOrNumber);
                cmd.Parameters.Add("documentCode", OracleDbType.Varchar2).Value = documentCode;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    cmd.Parameters.Add("fileName", OracleDbType.Varchar2).Value = fileName;
                }

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new WcwcDocumentRecord
                    {
                        RegistrationId = Convert.ToInt32(reader["REGISTRATION_ID"]),
                        RegistrationNo = Convert.ToString(reader["REGISTRATION_NO"]),
                        DocumentCode = Convert.ToString(reader["DOCUMENT_CODE"]),
                        FileName = Convert.ToString(reader["FILE_NAME"]),
                        MimeType = Convert.ToString(reader["MIME_TYPE"]),
                        FileSize = Convert.ToInt64(reader["FILE_SIZE"]),
                        UploadedByOperatorId = reader["UPLOADED_BY_OPERATOR_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["UPLOADED_BY_OPERATOR_ID"]),
                        UploadedAt = Convert.ToDateTime(reader["UPLOADED_AT"]),
                        UpdatedAt = Convert.ToDateTime(reader["UPDATED_AT"])
                    };
                }
            }
        }

        private void ExecuteSimpleProcedure(string procedureName, int registrationId, string csvValues, string csvParameterName)
        {
            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand(procedureName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                cmd.Parameters.Add("P_REGISTRATION_ID", OracleDbType.Int32).Value = registrationId;
                cmd.Parameters.Add(csvParameterName, OracleDbType.Varchar2).Value = ValueOrDbNull(csvValues);
                cmd.Parameters.Add("O_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                EnsureSuccess(cmd.Parameters["O_SUCCESS"].Value, cmd.Parameters["O_MESSAGE"].Value);
            }
        }

        private int ResolveRegistrationId(string registrationIdOrNumber)
        {
            int parsedId;
            if (int.TryParse(registrationIdOrNumber, out parsedId))
            {
                return parsedId;
            }

            using (var conn = _connectionFactory.CreateWcwc())
            using (var cmd = new OracleCommand("SELECT REGISTRATION_ID FROM TBL_WCWC_REGISTRATIONS WHERE REGISTRATION_NO = :registrationNo", conn))
            {
                cmd.Parameters.Add("registrationNo", OracleDbType.Varchar2).Value = registrationIdOrNumber;
                conn.Open();
                var value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    throw new InvalidOperationException("Registration not found");
                }
                return ConvertToInt(value);
            }
        }

        private static void EnsureSuccess(object successValue, object messageValue)
        {
            if (ConvertToInt(successValue) != 1)
            {
                throw new InvalidOperationException(Convert.ToString(messageValue) ?? "Database operation failed");
            }
        }

        private static object ValueOrDbNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static object NullableInt(int? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static object NullableDate(DateTime? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static object NullableDecimal(decimal? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
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

        private static WcwcRegistrationListItem MapRegistration(IDataRecord reader)
        {
            return new WcwcRegistrationListItem
            {
                RegistrationId = Convert.ToInt32(reader["REGISTRATION_ID"]),
                RegistrationNo = Convert.ToString(reader["REGISTRATION_NO"]),
                ApplicationMode = Convert.ToString(reader["APPLICATION_MODE"]),
                SubmissionChannel = Convert.ToString(reader["SUBMISSION_CHANNEL"]),
                Status = Convert.ToString(reader["STATUS"]),
                Surname = Convert.ToString(reader["SURNAME"]),
                FirstName = Convert.ToString(reader["FIRST_NAME"]),
                FatherName = Convert.ToString(reader["FATHER_NAME"]),
                MobileNumber = Convert.ToString(reader["MOBILE_NUMBER"]),
                AadhaarNumber = Convert.ToString(reader["AADHAAR_NUMBER"]),
                Dob = reader["DOB"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DOB"]),
                DisabilityPercentage = reader["DISABILITY_PERCENTAGE"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["DISABILITY_PERCENTAGE"]),
                HasCertificate = Convert.ToString(reader["HAS_CERTIFICATE"]),
                HasUdid = Convert.ToString(reader["HAS_UDID"]),
                NeedsAssistiveDevice = Convert.ToString(reader["NEEDS_ASSISTIVE_DEVICE"]),
                OperatorUserId = reader["OPERATOR_USER_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["OPERATOR_USER_ID"]),
                CreatedAt = Convert.ToDateTime(reader["CREATED_AT"]),
                UpdatedAt = Convert.ToDateTime(reader["UPDATED_AT"])
            };
        }

        private static WcwcRegistrationListItem MapSummary(IDataRecord reader)
        {
            return new WcwcRegistrationListItem
            {
                RegistrationId = Convert.ToInt32(reader["REGISTRATION_ID"]),
                RegistrationNo = Convert.ToString(reader["REGISTRATION_NO"]),
                ApplicationMode = Convert.ToString(reader["APPLICATION_MODE"]),
                SubmissionChannel = Convert.ToString(reader["SUBMISSION_CHANNEL"]),
                Status = Convert.ToString(reader["STATUS"]),
                Surname = Convert.ToString(reader["SURNAME"]),
                FirstName = Convert.ToString(reader["FIRST_NAME"]),
                FatherName = Convert.ToString(reader["FATHER_NAME"]),
                MobileNumber = Convert.ToString(reader["MOBILE_NUMBER"]),
                AadhaarNumber = Convert.ToString(reader["AADHAAR_NUMBER"]),
                Dob = reader["DOB"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DOB"]),
                DisabilityPercentage = reader["DISABILITY_PERCENTAGE"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["DISABILITY_PERCENTAGE"]),
                HasCertificate = Convert.ToString(reader["HAS_CERTIFICATE"]),
                HasUdid = Convert.ToString(reader["HAS_UDID"]),
                NeedsAssistiveDevice = Convert.ToString(reader["NEEDS_ASSISTIVE_DEVICE"]),
                OperatorUserId = reader["OPERATOR_USER_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["OPERATOR_USER_ID"]),
                OperatorName = Convert.ToString(reader["OPERATOR_NAME"]),
                DisabilityNames = Convert.ToString(reader["DISABILITY_NAMES"]),
                AssistiveDevices = Convert.ToString(reader["ASSISTIVE_DEVICES"]),
                CreatedAt = Convert.ToDateTime(reader["CREATED_AT"]),
                UpdatedAt = Convert.ToDateTime(reader["UPDATED_AT"])
            };
        }

        private static FilterDefinition BuildFilter(string searchText, string searchField, string status, string applicationMode, int? operatorUserId)
        {
            var filters = new List<string>();
            var parameters = new List<OracleParameter>();

            if (!string.IsNullOrWhiteSpace(status))
            {
                filters.Add("STATUS = :status");
                parameters.Add(new OracleParameter("status", OracleDbType.Varchar2) { Value = status.Trim().ToUpperInvariant() });
            }

            if (!string.IsNullOrWhiteSpace(applicationMode))
            {
                filters.Add("APPLICATION_MODE = :applicationMode");
                parameters.Add(new OracleParameter("applicationMode", OracleDbType.Varchar2) { Value = applicationMode.Trim().ToUpperInvariant() });
            }

            if (operatorUserId.HasValue)
            {
                filters.Add("OPERATOR_USER_ID = :operatorUserId");
                parameters.Add(new OracleParameter("operatorUserId", OracleDbType.Int32) { Value = operatorUserId.Value });
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var pattern = "%" + searchText.Trim().ToUpperInvariant() + "%";
                if (!string.IsNullOrWhiteSpace(searchField) && !searchField.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    string column;
                    switch (searchField.Trim().ToLowerInvariant())
                    {
                        case "registrationnumber":
                        case "registrationno":
                            column = "UPPER(REGISTRATION_NO)";
                            break;
                        case "aadhaarnumber":
                            column = "AADHAAR_NUMBER";
                            break;
                        case "mobilenumber":
                            column = "MOBILE_NUMBER";
                            break;
                        case "name":
                            column = "UPPER(SURNAME || ' ' || FIRST_NAME || ' ' || FATHER_NAME)";
                            break;
                        default:
                            column = string.Empty;
                            break;
                    }

                    if (!string.IsNullOrEmpty(column))
                    {
                        filters.Add(column + " LIKE :searchText");
                        parameters.Add(new OracleParameter("searchText", OracleDbType.Varchar2) { Value = pattern });
                    }
                }
                else
                {
                    filters.Add("(UPPER(REGISTRATION_NO) LIKE :searchText OR UPPER(SURNAME) LIKE :searchText OR UPPER(FIRST_NAME) LIKE :searchText OR AADHAAR_NUMBER LIKE :searchTextRaw OR MOBILE_NUMBER LIKE :searchTextRaw)");
                    parameters.Add(new OracleParameter("searchText", OracleDbType.Varchar2) { Value = pattern });
                    parameters.Add(new OracleParameter("searchTextRaw", OracleDbType.Varchar2) { Value = "%" + searchText.Trim() + "%" });
                }
            }

            return new FilterDefinition
            {
                Sql = filters.Count > 0 ? " WHERE " + string.Join(" AND ", filters) : string.Empty,
                Parameters = parameters
            };
        }

        private static void AddFilterParameters(OracleCommand command, FilterDefinition filter)
        {
            foreach (var parameter in filter.Parameters)
            {
                command.Parameters.Add(parameter.ParameterName, parameter.OracleDbType).Value = parameter.Value;
            }
        }

        private class FilterDefinition
        {
            public string Sql { get; set; }
            public List<OracleParameter> Parameters { get; set; }
        }
    }
}