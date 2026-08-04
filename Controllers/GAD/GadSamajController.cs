using System;
using System.Net;
using System.Web.Http;
using SmkcApi.Models.GAD;
using SmkcApi.Repositories.GAD;

namespace SmkcApi.Controllers.GAD
{
    [RoutePrefix("api/gad/samaj")]
    public class GadSamajController : ApiController
    {
        private const int DEFAULT_ULB_CODE = 1;

        private readonly IGadSamajRepository _repo;

        public GadSamajController(IGadSamajRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException("repo");
        }

        // ── GET api/gad/samaj/validate-nasti?nastiNo=700744 ──────────────────────

        [HttpGet]
        [Route("validate-nasti")]
        [AllowAnonymous]
        public IHttpActionResult ValidateNasti([FromUri] int nastiNo)
        {
            if (nastiNo <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "nastiNo is required and must be a positive integer." });

            try
            {
                var data = _repo.GetOrderByNastiNo(DEFAULT_ULB_CODE, nastiNo);
                if (data == null)
                    return Content(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        notFound = true,
                        message = "Nasti number not found or order is not active."
                    });

                return Ok(new { success = true, notFound = false, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/samaj/sanction-authorities ────────────────────────────────

        [HttpGet]
        [Route("sanction-authorities")]
        [AllowAnonymous]
        public IHttpActionResult GetSanctionAuthorities()
        {
            try
            {
                var data = _repo.GetSanctionAuthorities(DEFAULT_ULB_CODE);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/samaj/signing-officers?deptCode=16 ───────────────────────

        [HttpGet]
        [Route("signing-officers")]
        [AllowAnonymous]
        public IHttpActionResult GetSigningOfficers([FromUri] int deptCode)
        {
            if (deptCode <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "deptCode is required." });

            try
            {
                var data = _repo.GetSigningOfficers(DEFAULT_ULB_CODE, deptCode);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/samaj/next-number?deptCode=16&finYear=2025-2026 ──────────

        [HttpGet]
        [Route("next-number")]
        [AllowAnonymous]
        public IHttpActionResult GetNextNumber([FromUri] int deptCode, [FromUri] string finYear)
        {
            if (deptCode <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "deptCode is required." });
            if (string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear is required." });

            try
            {
                var data = _repo.GetNextSamajNumber(DEFAULT_ULB_CODE, deptCode, finYear);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── POST api/gad/samaj/generate ───────────────────────────────────────────

        [HttpPost]
        [Route("generate")]
        [AllowAnonymous]
        public IHttpActionResult Generate([FromBody] GenerateSamajRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.NastiNo))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "NastiNo is required." });
            if (request.DeptCode <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "DeptCode is required." });
            if (string.IsNullOrWhiteSpace(request.FinYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "FinYear is required." });
            if (string.IsNullOrWhiteSpace(request.ProposalName))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "ProposalName is required." });
            if (string.IsNullOrWhiteSpace(request.EligibleVendorName))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "EligibleVendorName is required." });
            if (request.VendorProposedAmount <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "VendorProposedAmount must be greater than zero." });
            if (string.IsNullOrWhiteSpace(request.WorkPeriod))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "WorkPeriod is required." });
            if (string.IsNullOrWhiteSpace(request.SupervisorName))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "SupervisorName is required." });
            if (string.IsNullOrWhiteSpace(request.EnteredBy))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "EnteredBy is required." });

            try
            {
                var result = _repo.GenerateSamaj(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/samaj/detail?workOrderNo=&deptCode=&finYear= ─────────────

        [HttpGet]
        [Route("detail")]
        [AllowAnonymous]
        public IHttpActionResult GetDetail([FromUri] long workOrderNo, [FromUri] int deptCode, [FromUri] string finYear)
        {
            if (workOrderNo <= 0 || deptCode <= 0 || string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "workOrderNo, deptCode and finYear are required." });

            try
            {
                var detail = _repo.GetSamajDetail(DEFAULT_ULB_CODE, workOrderNo, deptCode, finYear);
                if (detail == null)
                    return Content(HttpStatusCode.NotFound, new { success = false, message = "Samaj record not found." });
                return Ok(new { success = true, data = detail });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/samaj/list?finYear=2025-2026 ─────────────────────────────

        [HttpGet]
        [Route("list")]
        [AllowAnonymous]
        public IHttpActionResult GetList([FromUri] string finYear, [FromUri] string userId = null)
        {
            if (string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear is required." });

            try
            {
                var data = _repo.GetSamajList(DEFAULT_ULB_CODE, finYear, userId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}
