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
        public IHttpActionResult DeleteRegistration(string identifier, [FromBody] WcwcStatusUpdateRequest request)
        {
            var safeRequest = request ?? new WcwcStatusUpdateRequest();
            if (string.IsNullOrWhiteSpace(safeRequest.Remarks))
            {
                safeRequest.Remarks = "Department delete action without explicit reason";
            }

            var result = _service.DeleteRegistration(identifier, safeRequest);
            return CreateResponse(result);
        }

        [HttpPatch]
        [Route("register/{identifier}/status")]
        [AllowAnonymous]
        public IHttpActionResult UpdateStatus(string identifier, [FromBody] WcwcStatusUpdateRequest request)
        {
            var result = _service.UpdateStatus(identifier, request);
            return CreateResponse(result);
        }

        [HttpPost]
        [Route("register/{identifier}/status/sms")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> SendStatusSms(string identifier, [FromBody] WcwcStatusSmsRequest request)
        {
            var result = await _service.SendStatusSmsAsync(identifier, request);
            return CreateResponse(result);
        }

        [HttpGet]
        [Route("application-status/{registrationNumber}")]
        [AllowAnonymous]
        public IHttpActionResult GetApplicationStatus(string registrationNumber)
        {
            var result = _service.GetApplicationStatus(registrationNumber);
            return CreateResponse(result);
        }

        [HttpGet]
        [Route("report")]
        [AllowAnonymous]
        public IHttpActionResult GetReport(
            [FromUri] string reportType = "ALL",
            [FromUri] string fromDate = null,
            [FromUri] string toDate = null,
            [FromUri] string applicationMode = null)
        {
            DateTime? from = null;
            DateTime? to = null;
            DateTime parsed;
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out parsed))
                from = parsed;
            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out parsed))
                to = parsed;

            var result = _service.GetReport(reportType, from, to, applicationMode);
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
                : response.ErrorCode == "DUPLICATE_REGISTRATION_NUMBER"
                    ? HttpStatusCode.Conflict
                : response.ErrorCode == "MOBILE_NOT_FOUND" || response.ErrorCode == "STATUS_NOT_FOUND"
                    ? HttpStatusCode.BadRequest
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
            var key = (name ?? string.Empty).Trim();
            var normalized = key.ToLowerInvariant();

            switch (normalized)
            {
                case "surname": request.Surname = value; break;
                case "firstname": request.FirstName = value; break;
                case "fathername": request.FatherName = value; break;
                case "mothername": request.MotherName = value; break;
                case "education": request.Education = value; break;
                case "aadhaarnumber": request.AadhaarNumber = value; break;
                case "rationcardnumber": request.RationCardNumber = value; break;
                case "rationcardcolor":
                case "rationcardtype":
                case "rationtype":
                    request.RationCardColor = value;
                    break;
                case "dob": request.Dob = value; break;
                case "maritalstatus": request.MaritalStatus = value; break;
                case "religion": request.Religion = value; break;
                case "caste": request.Caste = value; break;
                case "familyrelation": request.FamilyRelation = value; break;
                case "fulladdress": request.FullAddress = value; break;
                case "livesincorporationarea": request.LivesInCorporationArea = value; break;
                case "wardnumber": request.WardNumber = value; break;
                case "prabhagsamiti": request.PrabhagSamiti = value; break;
                case "uphc": request.Uphc = value; break;
                case "pincode": request.Pincode = value; break;
                case "constituency": request.Constituency = value; break;
                case "mobilenumber": request.MobileNumber = value; break;
                case "alternatephone": request.AlternatePhone = value; break;
                case "bankname": request.BankName = value; break;
                case "branchname": request.BranchName = value; break;
                case "accountnumber": request.AccountNumber = value; break;
                case "ifsccode": request.IfscCode = value; break;
                case "bplnumber": request.BplNumber = value; break;
                case "bplyear": request.BplYear = value; break;
                case "hascertificate": request.HasCertificate = value; break;
                case "certificatenumber": request.CertificateNumber = value; break;
                case "certificatedate": request.CertificateDate = value; break;
                case "certificatetype": request.CertificateType = value; break;
                case "disabilitypercentage": request.DisabilityPercentage = value; break;
                case "hasudid": request.HasUdid = value; break;
                case "udidnumber": request.UdidNumber = value; break;
                case "hasstpass": request.HasStPass = value; break;
                case "stpassnumber": request.StPassNumber = value; break;
                case "hasrailwaypass": request.HasRailwayPass = value; break;
                case "railwaypassnumber": request.RailwayPassNumber = value; break;
                case "hasmsrtcpass": request.HasMsrtcPass = value; break;
                case "msrtcpassnumber": request.MsrtcPassNumber = value; break;
                case "isemployed": request.IsEmployed = value; break;
                case "employmenttype": request.EmploymentType = value; break;
                case "occupation": request.Occupation = value; break;
                case "hasgovtbenefit": request.HasGovtBenefit = value; break;
                case "govtbenefitscheme": request.GovtBenefitScheme = value; break;
                case "hasmcbenefit": request.HasMcBenefit = value; break;
                case "mcbenefitdetails": request.McBenefitDetails = value; break;
                case "hassgnpension": request.HasSgnPension = value; break;
                case "hasownhouse": request.HasOwnHouse = value; break;
                case "wantshousingbenefit": request.WantsHousingBenefit = value; break;
                case "hasownland": request.HasOwnLand = value; break;
                case "hasguardianship": request.HasGuardianship = value; break;
                case "guardianname": request.GuardianName = value; break;
                case "guardianaddress": request.GuardianAddress = value; break;
                case "guardianphone": request.GuardianPhone = value; break;
                case "needsassistivedevice": request.NeedsAssistiveDevice = value; break;
                case "documentssubmitted": request.DocumentsSubmitted = value; break;
                case "applicantsigndate": request.ApplicantSignDate = value; break;
                case "surveyorname": request.SurveyorName = value; break;
                case "surveyordesignation": request.SurveyorDesignation = value; break;
                case "surveyormobile": request.SurveyorMobile = value; break;
                case "termsaccepted": request.TermsAccepted = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); break;
                case "applicationmode": request.ApplicationMode = value; break;
                case "submissionchannel": request.SubmissionChannel = value; break;
                case "publicsubmittername": request.PublicSubmitterName = value; break;
                case "publicsubmittermobile": request.PublicSubmitterMobile = value; break;
                case "publiccentername": request.PublicCenterName = value; break;
                case "manualapplicationno": request.ManualApplicationNo = value; break;
                case "createdby": request.CreatedBy = value; break;
                case "operatoruserid": request.OperatorUserId = ParseNullableInt(value); break;
                case "disabilitytypes":
                case "disabilityselections":
                    request.DisabilitySelections = ParseStringArray(value);
                    break;
                case "assistivedevices":
                case "assistivedeviceselections":
                    request.AssistiveDeviceSelections = ParseStringArray(value);
                    break;
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