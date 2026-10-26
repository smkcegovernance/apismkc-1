using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using SmkcApi.Models;
using SmkcApi.Services;

namespace SmkcApi.Controllers.WomenChildWelfare
{
    /// <summary>
    /// API controller for Single Women Empowerment Program registration
    /// Ekal Mahila Sakshamikaran Karyakram (एकल महिला साक्षमीकरण कार्यक्रम)
    ///
    /// Routes (prefix: api/women-child-welfare/single-women):
    ///   POST   /register           — create new registration
    ///   PUT    /register/{id}      — update existing
    ///   GET    /register/{id}      — fetch by ID or reg number
    ///   GET    /register           — search with ?q=&status=&applicationMode=
    ///   PATCH  /register/{id}/status — update status
    /// </summary>
    [RoutePrefix("api/women-child-welfare/single-women")]
    public class SingleWomenRegistrationController : ApiController
    {
        private readonly ISWRService _service;

        public SingleWomenRegistrationController(ISWRService service)
        {
            _service = service;
        }

        // ---------------------------------------------------------------
        //  Health check
        // ---------------------------------------------------------------
        [HttpGet]
        [Route("health")]
        [AllowAnonymous]
        public IHttpActionResult Health()
        {
            return Ok(new
            {
                success = true,
                message = "SingleWomenRegistrationController is accessible",
                timestamp = DateTime.UtcNow
            });
        }

        // ---------------------------------------------------------------
        //  Register (public — JSON body)
        // ---------------------------------------------------------------
        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Register()
        {
            var request = await ReadRequestAsync();

            if (request == null)
                return Content(HttpStatusCode.BadRequest,
                    SWRApiResponse.CreateError("Request body is required", "INVALID_BODY"));

            // Detect application mode from header (operator sends X-Application-Mode: OPERATOR)
            var modeHeader = Request.Headers.Contains("X-Application-Mode")
                ? Request.Headers.GetValues("X-Application-Mode").GetEnumerator()
                : null;
            if (modeHeader != null && modeHeader.MoveNext())
                request.ApplicationMode = modeHeader.Current;

            if (string.IsNullOrWhiteSpace(request.ApplicationMode))
                request.ApplicationMode = "ONLINE";

            var result = _service.Register(request);
            return CreateResponse(result);
        }

        // ---------------------------------------------------------------
        //  Update
        // ---------------------------------------------------------------
        [HttpPut]
        [Route("register/{identifier}")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Update(string identifier)
        {
            var request = await ReadRequestAsync();

            if (request == null)
                return Content(HttpStatusCode.BadRequest,
                    SWRApiResponse.CreateError("Request body is required", "INVALID_BODY"));

            var result = _service.Update(identifier, request);
            return CreateResponse(result);
        }

        // ---------------------------------------------------------------
        //  Get by identifier
        // ---------------------------------------------------------------
        [HttpGet]
        [Route("register/{identifier}")]
        [AllowAnonymous]
        public IHttpActionResult GetRegistration(string identifier)
        {
            var result = _service.GetRegistration(identifier);
            return CreateResponse(result);
        }

        [HttpGet]
        [Route("documents/download")]
        [AllowAnonymous]
        public HttpResponseMessage DownloadDocument([FromUri] string registrationNumber, [FromUri] string documentCode, [FromUri] string fileName = null)
        {
            var result = _service.DownloadDocument(registrationNumber, documentCode, fileName);
            if (!result.Success)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, result);
            }

            var data = result.Data as dynamic;
            var fileData = data != null ? data.fileData as string : null;
            var resolvedFileName = data != null ? data.fileName as string : null;
            if (string.IsNullOrWhiteSpace(resolvedFileName))
            {
                resolvedFileName = fileName;
            }
            if (string.IsNullOrWhiteSpace(resolvedFileName))
            {
                resolvedFileName = "document.bin";
            }

            if (string.IsNullOrWhiteSpace(fileData))
            {
                return Request.CreateResponse(HttpStatusCode.NotFound,
                    SWRApiResponse.CreateError("Document payload is empty", "DOCUMENT_NOT_FOUND"));
            }

            var bytes = Convert.FromBase64String(fileData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Web.MimeMapping.GetMimeMapping(resolvedFileName));
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = resolvedFileName
            };

            return response;
        }

        // ---------------------------------------------------------------
        //  Search
        // ---------------------------------------------------------------
        [HttpGet]
        [Route("register")]
        [AllowAnonymous]
        public IHttpActionResult Search(
            [FromUri] string q = null,
            [FromUri] string status = null,
            [FromUri] string applicationMode = null)
        {
            var result = _service.Search(q, status, applicationMode);
            return CreateResponse(result);
        }

        // ---------------------------------------------------------------
        //  Update Status
        // ---------------------------------------------------------------
        [HttpPatch]
        [Route("register/{identifier}/status")]
        [AllowAnonymous]
        public IHttpActionResult UpdateStatus(string identifier, [FromBody] SWRStatusUpdateRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest,
                    SWRApiResponse.CreateError("Status update body is required", "INVALID_BODY"));

            var result = _service.UpdateStatus(identifier, request);
            return CreateResponse(result);
        }

        [HttpDelete]
        [Route("register/{identifier}")]
        [AllowAnonymous]
        public IHttpActionResult DeleteRegistration(string identifier, [FromBody] SWRStatusUpdateRequest request)
        {
            var safeRequest = request ?? new SWRStatusUpdateRequest();
            var result = _service.DeleteRegistration(identifier, safeRequest);
            return CreateResponse(result);
        }

        // ---------------------------------------------------------------
        //  Helper
        // ---------------------------------------------------------------
        private IHttpActionResult CreateResponse(SWRApiResponse response)
        {
            if (response.Success) return Ok(response);

            var code = response.ErrorCode == "REG_NOT_FOUND"
                ? HttpStatusCode.NotFound
                : response.ErrorCode == "DUPLICATE_AADHAAR"
                    ? HttpStatusCode.Conflict
                : response.ErrorCode == "VALIDATION_ERROR"
                    ? HttpStatusCode.BadRequest
                    : response.ErrorCode == "DELETE_REASON_REQUIRED"
                        ? HttpStatusCode.BadRequest
                    : response.ErrorCode == "DOCUMENT_STORAGE_UNAVAILABLE"
                        ? HttpStatusCode.ServiceUnavailable
                    : HttpStatusCode.InternalServerError;

            return Content(code, response);
        }

        private async Task<SWRRegistrationRequest> ReadRequestAsync()
        {
            if (Request.Content.IsMimeMultipartContent())
            {
                return await ParseMultipartRequest();
            }

            try
            {
                var body = await Request.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SWRRegistrationRequest>(body);
            }
            catch
            {
                return null;
            }
        }

        private async Task<SWRRegistrationRequest> ParseMultipartRequest()
        {
            var provider = await Request.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider());
            SWRRegistrationRequest request = null;

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
                request.Files.Add(new SWRUploadedFile
                {
                    FieldName = name,
                    FileName = Unquote(contentDisposition.FileName),
                    ContentType = content.Headers.ContentType != null ? content.Headers.ContentType.MediaType : null,
                    Bytes = bytes,
                    FileSize = bytes.LongLength
                });
            }

            return request ?? new SWRRegistrationRequest();
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

        private static SWRRegistrationRequest DeserializePayload(string payload, SWRRegistrationRequest existing)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return existing ?? new SWRRegistrationRequest();
            }

            var deserialized = JsonConvert.DeserializeObject<SWRRegistrationRequest>(payload) ?? new SWRRegistrationRequest();
            if (existing != null && existing.Files != null && existing.Files.Count > 0)
            {
                deserialized.Files.AddRange(existing.Files);
            }

            return deserialized;
        }

        private static SWRRegistrationRequest EnsureRequest(SWRRegistrationRequest request)
        {
            return request ?? new SWRRegistrationRequest();
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
            try
            {
                if (!string.IsNullOrWhiteSpace(charset))
                {
                    return Encoding.GetEncoding(charset);
                }
            }
            catch
            {
            }

            return Encoding.UTF8;
        }

        private static string Unquote(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? value : value.Trim().Trim('"');
        }
    }
}
