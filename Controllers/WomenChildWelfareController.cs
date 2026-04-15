using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using SmkcApi.Models;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    [RoutePrefix("api/women-child-welfare")]
    public class WomenChildWelfareController : ApiController
    {
        private readonly IWcwcDisabilityService _service;

        public WomenChildWelfareController(IWcwcDisabilityService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("health")]
        [AllowAnonymous]
        public IHttpActionResult Health()
        {
            return Ok(new
            {
                success = true,
                message = "WomenChildWelfareController is accessible",
                timestamp = DateTime.UtcNow,
                targetFramework = ".NET Framework 4.5"
            });
        }

        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Register()
        {
            var request = await ParseMultipartRequest();
            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest, WcwcApiResponse.CreateError("Multipart form-data request is required", "INVALID_CONTENT"));
            }

            var result = _service.Register(request);
            return CreateResponse(result);
        }

        [HttpPut]
        [Route("register/{identifier}")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Update(string identifier)
        {
            var request = await ParseMultipartRequest();
            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest, WcwcApiResponse.CreateError("Multipart form-data request is required", "INVALID_CONTENT"));
            }

            var result = _service.Update(identifier, request);
            return CreateResponse(result);
        }

        [HttpGet]
        [Route("register/{identifier}")]
        [AllowAnonymous]
        public IHttpActionResult GetRegistration(string identifier)
        {
            var result = _service.GetRegistration(identifier);
            return CreateResponse(result);
        }

        [HttpGet]
        [Route("register")]
        [AllowAnonymous]
        public IHttpActionResult Search([FromUri] string q = null, [FromUri] string status = null, [FromUri] string applicationMode = null, [FromUri] int? operatorUserId = null)
        {
            var result = _service.Search(q, status, applicationMode, operatorUserId);
            return CreateResponse(result);
        }

        [HttpGet]
        [Route("register/search")]
        [AllowAnonymous]
        public IHttpActionResult SearchLegacy([FromUri] string q = null, [FromUri] string field = null)
        {
            var result = _service.Search(q, null, null, null);
            return CreateResponse(result);
        }

        [HttpDelete]
        [Route("register/{identifier}")]
        [AllowAnonymous]
        public IHttpActionResult DeleteRegistration(string identifier)
        {
            var result = _service.Cancel(identifier, new WcwcStatusUpdateRequest { Remarks = "Cancelled from API delete endpoint" });
            return CreateResponse(result);
        }

        [HttpPost]
        [Route("operator/login")]
        [AllowAnonymous]
        public IHttpActionResult OperatorLogin([FromBody] WcwcOperatorLoginRequest request)
        {
            var result = _service.OperatorLogin(request);
            return CreateResponse(result);
        }

        [HttpPost]
        [Route("documents/upload")]
        [AllowAnonymous]
        public IHttpActionResult UploadDocument([FromBody] WcwcDocumentUploadRequest request)
        {
            var result = _service.UploadDocument(request);
            return CreateResponse(result);
        }

        [HttpGet]
        [Route("documents/download")]
        [AllowAnonymous]
        public HttpResponseMessage DownloadDocument(string registrationNumber, string documentCode, string fileName, bool inline = false)
        {
            var result = _service.DownloadDocument(registrationNumber, documentCode, fileName);
            if (!result.Success)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, result);
            }

            var data = result.Data as dynamic;
            var fileData = data.fileData as string;
            var bytes = Convert.FromBase64String(fileData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Web.MimeMapping.GetMimeMapping(fileName));
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(inline ? "inline" : "attachment")
            {
                FileName = fileName
            };

            return response;
        }

        private IHttpActionResult CreateResponse(WcwcApiResponse response)
        {
            if (response.Success)
            {
                return Ok(response);
            }

            var statusCode = response.ErrorCode == "REGISTRATION_NOT_FOUND" || response.ErrorCode == "DOCUMENT_NOT_FOUND"
                ? HttpStatusCode.NotFound
                : HttpStatusCode.BadRequest;

            return Content(statusCode, response);
        }

        private async Task<WcwcRegistrationUpsertRequest> ParseMultipartRequest()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return null;
            }

            var provider = await Request.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider());
            WcwcRegistrationUpsertRequest request = null;

            foreach (var content in provider.Contents)
            {
                var contentDisposition = content.Headers.ContentDisposition;
                var name = Unquote(contentDisposition.Name);
                if (string.IsNullOrWhiteSpace(contentDisposition.FileName))
                {
                    if (IsJsonPayloadPart(name, content))
                    {
                        request = DeserializePayload(await ReadFormValueAsync(content), request);
                        continue;
                    }

                    request = EnsureRequest(request);
                    var value = await ReadFormValueAsync(content);
                    MapFormField(request, name, value);
                    continue;
                }

                var bytes = await content.ReadAsByteArrayAsync();
                if (bytes == null || bytes.Length == 0)
                {
                    continue;
                }

                if (IsJsonPayloadPart(name, content))
                {
                    request = DeserializePayload(ResolveEncoding(content).GetString(bytes), request);
                    continue;
                }

                request = EnsureRequest(request);

                request.Files.Add(new WcwcUploadedFile
                {
                    FieldName = name,
                    FileName = Unquote(contentDisposition.FileName),
                    ContentType = content.Headers.ContentType != null ? content.Headers.ContentType.MediaType : null,
                    Bytes = bytes,
                    FileSize = bytes.LongLength
                });
            }

            return request ?? new WcwcRegistrationUpsertRequest();
        }

        private static bool IsJsonPayloadPart(string name, HttpContent content)
        {
            if (string.Equals(name, "payload", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "payloadJson", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var mediaType = content.Headers.ContentType != null ? content.Headers.ContentType.MediaType : null;
            return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
        }

        private static WcwcRegistrationUpsertRequest DeserializePayload(string payload, WcwcRegistrationUpsertRequest existing)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return existing ?? new WcwcRegistrationUpsertRequest();
            }

            var deserialized = JsonConvert.DeserializeObject<WcwcRegistrationUpsertRequest>(payload) ?? new WcwcRegistrationUpsertRequest();
            if (existing != null && existing.Files != null && existing.Files.Count > 0)
            {
                deserialized.Files.AddRange(existing.Files);
            }

            return deserialized;
        }

        private static WcwcRegistrationUpsertRequest EnsureRequest(WcwcRegistrationUpsertRequest request)
        {
            return request ?? new WcwcRegistrationUpsertRequest();
        }

        private static async Task<string> ReadFormValueAsync(HttpContent content)
        {
            var bytes = await content.ReadAsByteArrayAsync();
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            return ResolveEncoding(content).GetString(bytes);
        }

        private static Encoding ResolveEncoding(HttpContent content)
        {
            var charset = content.Headers.ContentType != null ? content.Headers.ContentType.CharSet : null;
            if (string.IsNullOrWhiteSpace(charset))
            {
                return Encoding.UTF8;
            }

            charset = charset.Trim().Trim('"');

            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8;
            }
        }

        private static void MapFormField(WcwcRegistrationUpsertRequest request, string name, string value)
        {
            switch (name)
            {
                case "surname": request.Surname = value; break;
                case "firstName": request.FirstName = value; break;
                case "fatherName": request.FatherName = value; break;
                case "motherName": request.MotherName = value; break;
                case "education": request.Education = value; break;
                case "aadhaarNumber": request.AadhaarNumber = value; break;
                case "dob": request.Dob = value; break;
                case "maritalStatus": request.MaritalStatus = value; break;
                case "religion": request.Religion = value; break;
                case "caste": request.Caste = value; break;
                case "familyRelation": request.FamilyRelation = value; break;
                case "fullAddress": request.FullAddress = value; break;
                case "wardNumber": request.WardNumber = value; break;
                case "prabhagSamiti": request.PrabhagSamiti = value; break;
                case "uphc": request.Uphc = value; break;
                case "pincode": request.Pincode = value; break;
                case "constituency": request.Constituency = value; break;
                case "mobileNumber": request.MobileNumber = value; break;
                case "alternatePhone": request.AlternatePhone = value; break;
                case "bankName": request.BankName = value; break;
                case "branchName": request.BranchName = value; break;
                case "accountNumber": request.AccountNumber = value; break;
                case "ifscCode": request.IfscCode = value; break;
                case "bplNumber": request.BplNumber = value; break;
                case "bplYear": request.BplYear = value; break;
                case "hasCertificate": request.HasCertificate = value; break;
                case "certificateNumber": request.CertificateNumber = value; break;
                case "certificateDate": request.CertificateDate = value; break;
                case "certificateType": request.CertificateType = value; break;
                case "disabilityPercentage": request.DisabilityPercentage = value; break;
                case "hasUDID": request.HasUdid = value; break;
                case "udidNumber": request.UdidNumber = value; break;
                case "hasSTPass": request.HasStPass = value; break;
                case "stPassNumber": request.StPassNumber = value; break;
                case "hasRailwayPass": request.HasRailwayPass = value; break;
                case "railwayPassNumber": request.RailwayPassNumber = value; break;
                case "hasMSRTCPass": request.HasMsrtcPass = value; break;
                case "msrtcPassNumber": request.MsrtcPassNumber = value; break;
                case "isEmployed": request.IsEmployed = value; break;
                case "employmentType": request.EmploymentType = value; break;
                case "occupation": request.Occupation = value; break;
                case "hasGovtBenefit": request.HasGovtBenefit = value; break;
                case "govtBenefitScheme": request.GovtBenefitScheme = value; break;
                case "hasMCBenefit": request.HasMcBenefit = value; break;
                case "mcBenefitDetails": request.McBenefitDetails = value; break;
                case "hasSGNPension": request.HasSgnPension = value; break;
                case "hasOwnHouse": request.HasOwnHouse = value; break;
                case "wantsHousingBenefit": request.WantsHousingBenefit = value; break;
                case "hasOwnLand": request.HasOwnLand = value; break;
                case "hasGuardianship": request.HasGuardianship = value; break;
                case "guardianName": request.GuardianName = value; break;
                case "guardianAddress": request.GuardianAddress = value; break;
                case "guardianPhone": request.GuardianPhone = value; break;
                case "needsAssistiveDevice": request.NeedsAssistiveDevice = value; break;
                case "documentsSubmitted": request.DocumentsSubmitted = value; break;
                case "applicantSignDate": request.ApplicantSignDate = value; break;
                case "surveyorName": request.SurveyorName = value; break;
                case "surveyorDesignation": request.SurveyorDesignation = value; break;
                case "surveyorMobile": request.SurveyorMobile = value; break;
                case "termsAccepted": request.TermsAccepted = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); break;
                case "applicationMode": request.ApplicationMode = value; break;
                case "submissionChannel": request.SubmissionChannel = value; break;
                case "publicSubmitterName": request.PublicSubmitterName = value; break;
                case "publicSubmitterMobile": request.PublicSubmitterMobile = value; break;
                case "publicCenterName": request.PublicCenterName = value; break;
                case "manualApplicationNo": request.ManualApplicationNo = value; break;
                case "createdBy": request.CreatedBy = value; break;
                case "operatorUserId": request.OperatorUserId = ParseNullableInt(value); break;
                case "disabilityTypes": request.DisabilitySelections = ParseStringArray(value); break;
                case "assistiveDevices": request.AssistiveDeviceSelections = ParseStringArray(value); break;
            }
        }

        private static int? ParseNullableInt(string value)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : (int?)null;
        }

        private static List<string> ParseStringArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            if (value.TrimStart().StartsWith("["))
            {
                return JsonConvert.DeserializeObject<List<string>>(value) ?? new List<string>();
            }

            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList();
        }

        private static string Unquote(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? value : value.Trim('"');
        }
    }
}