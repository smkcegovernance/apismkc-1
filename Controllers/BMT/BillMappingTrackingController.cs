using System;
using System.Net;
using System.Web.Http;
using SmkcApi.Models.BMT;
using SmkcApi.Repositories.BMT;

namespace SmkcApi.Controllers.BMT
{
    [RoutePrefix("api/accounts/bmt")]
    public class BillMappingTrackingController : ApiController
    {
        private const int DEFAULT_ULB_CODE = 1;
        private readonly IBillMappingTrackingRepository _repo;

        public BillMappingTrackingController(IBillMappingTrackingRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException("repo");
        }

        [HttpGet]
        [Route("departments")]
        [AllowAnonymous]
        public IHttpActionResult GetDepartments()
        {
            try
            {
                var data = _repo.GetDepartments(DEFAULT_ULB_CODE);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("budget-heads")]
        [AllowAnonymous]
        public IHttpActionResult GetBudgetHeads([FromUri] string finYear)
        {
            if (string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear is required." });

            try
            {
                var data = _repo.GetBudgetHeads(DEFAULT_ULB_CODE, finYear);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("bills")]
        [AllowAnonymous]
        public IHttpActionResult GetBills([FromUri] string finYear, [FromUri] string acSubhead = null)
        {
            if (string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear is required." });

            try
            {
                var data = _repo.GetEligibleLegacyBills(DEFAULT_ULB_CODE, finYear, acSubhead);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("validate-nasti")]
        [AllowAnonymous]
        public IHttpActionResult ValidateNasti([FromUri] long nastiNo, [FromUri] string finYear = null)
        {
            if (nastiNo <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "nastiNo is required." });

            try
            {
                var data = _repo.ValidateNastiForMapping(DEFAULT_ULB_CODE, nastiNo, finYear);
                if (!data.Success)
                    return Content((HttpStatusCode)422, new { success = false, message = data.Message, data });
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("map-bill")]
        [AllowAnonymous]
        public IHttpActionResult MapBill([FromBody] MapLegacyBillRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            if (request.RegBillNo <= 0 || request.NastiNo <= 0 || request.BillAmount <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "regBillNo, nastiNo and billAmount are required." });

            try
            {
                var result = _repo.MapLegacyBillToNasti(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, new { success = false, message = result.Message });
                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("mark-direct")]
        [AllowAnonymous]
        public IHttpActionResult MarkDirect([FromBody] MarkDirectBillRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            if (request.RegBillNo <= 0 || request.BillAmount <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "regBillNo and billAmount are required." });

            try
            {
                var result = _repo.MarkLegacyBillAsDirect(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, new { success = false, message = result.Message });
                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("mappings")]
        [AllowAnonymous]
        public IHttpActionResult ListMappings([FromUri] string finYear = null, [FromUri] string acSubhead = null, [FromUri] string mapType = null)
        {
            try
            {
                var data = _repo.ListMappings(DEFAULT_ULB_CODE, finYear, acSubhead, mapType);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("direct-bills")]
        [AllowAnonymous]
        public IHttpActionResult CreateDirectBill([FromBody] DirectBillEntryRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.FinYear) || string.IsNullOrWhiteSpace(request.AcSubhead) || string.IsNullOrWhiteSpace(request.AccountBillNo))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear, acSubhead and accountBillNo are required." });

            if (string.IsNullOrWhiteSpace(request.BillType))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "billType is required." });

            if (string.Equals(request.BillType.Trim(), "OTHER", StringComparison.OrdinalIgnoreCase))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "For OTHER, enter a custom bill type manually." });

            try
            {
                var result = _repo.CreateDirectBillEntry(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, new { success = false, message = result.Message });
                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("direct-bills")]
        [AllowAnonymous]
        public IHttpActionResult ListDirectBills([FromUri] string finYear = null, [FromUri] string acSubhead = null)
        {
            try
            {
                var data = _repo.ListDirectBillEntries(DEFAULT_ULB_CODE, finYear, acSubhead);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}
