using System;
using System.Net;
using System.Net.Http;
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
            SWRRegistrationRequest request;
            try
            {
                var body = await Request.Content.ReadAsStringAsync();
                request = JsonConvert.DeserializeObject<SWRRegistrationRequest>(body);
            }
            catch
            {
                return Content(HttpStatusCode.BadRequest,
                    SWRApiResponse.CreateError("Invalid JSON body", "INVALID_BODY"));
            }

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
            SWRRegistrationRequest request;
            try
            {
                var body = await Request.Content.ReadAsStringAsync();
                request = JsonConvert.DeserializeObject<SWRRegistrationRequest>(body);
            }
            catch
            {
                return Content(HttpStatusCode.BadRequest,
                    SWRApiResponse.CreateError("Invalid JSON body", "INVALID_BODY"));
            }

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

        // ---------------------------------------------------------------
        //  Helper
        // ---------------------------------------------------------------
        private IHttpActionResult CreateResponse(SWRApiResponse response)
        {
            if (response.Success) return Ok(response);

            var code = response.ErrorCode == "REG_NOT_FOUND"
                ? HttpStatusCode.NotFound
                : response.ErrorCode == "VALIDATION_ERROR"
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError;

            return Content(code, response);
        }
    }
}
