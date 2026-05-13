using System;
using System.Net;
using System.Web.Http;
using SmkcApi.Models.BudgetBook;
using SmkcApi.Repositories.BudgetBook;

namespace SmkcApi.Controllers.BudgetBook
{
    [RoutePrefix("api/accounts/budget-book")]
    public class BudgetBookController : ApiController
    {
        private const int DEFAULT_ULB_CODE = 1;

        private readonly IBudgetBookRepository _repo;

        public BudgetBookController(IBudgetBookRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        // ── GET api/accounts/budget-book/departments ─────────────────────────────

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

        // ── GET api/accounts/budget-book/subheads?finYear=2025-2026 ─────────────

        [HttpGet]
        [Route("subheads")]
        [AllowAnonymous]
        public IHttpActionResult GetSubheads([FromUri] string finYear)
        {
            if (string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear is required." });
            try
            {
                var data = _repo.GetSubheads(DEFAULT_ULB_CODE, finYear);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/accounts/budget-book/remaining?acSubhead=E-4219&finYear=2025-2026

        [HttpGet]
        [Route("remaining")]
        [AllowAnonymous]
        public IHttpActionResult GetRemaining([FromUri] string acSubhead, [FromUri] string finYear)
        {
            if (string.IsNullOrWhiteSpace(acSubhead) || string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "acSubhead and finYear are required." });
            try
            {
                var data = _repo.GetRemainingBudget(DEFAULT_ULB_CODE, acSubhead, finYear);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── POST api/accounts/budget-book/primary ───────────────────────────────

        [HttpPost]
        [Route("primary")]
        [AllowAnonymous]
        public IHttpActionResult SavePrimary([FromBody] PrimaryBudgetEntryRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.AcSubhead))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "AcSubhead is required." });
            if (string.IsNullOrWhiteSpace(request.FinYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "FinYear is required." });
            if (request.ProposedAmount <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "ProposedAmount must be greater than zero." });
            if (string.IsNullOrWhiteSpace(request.EnteredBy))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "EnteredBy is required." });

            try
            {
                var result = _repo.SavePrimaryEntry(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/accounts/budget-book/primary/{bookEntryNo} ──────────────────

        [HttpGet]
        [Route("primary/{bookEntryNo:long}")]
        [AllowAnonymous]
        public IHttpActionResult GetPrimary(long bookEntryNo)
        {
            try
            {
                var entry = _repo.GetPrimaryEntry(DEFAULT_ULB_CODE, bookEntryNo);
                if (entry == null)
                    return NotFound();
                return Ok(new { success = true, data = entry });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── POST api/accounts/budget-book/final ─────────────────────────────────

        [HttpPost]
        [Route("final")]
        [AllowAnonymous]
        public IHttpActionResult SaveFinal([FromBody] FinalBudgetEntryRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });
            if (request.BookEntryNo <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "BookEntryNo is required." });
            if (request.FinalProposedAmount <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "FinalProposedAmount must be greater than zero." });
            if (string.IsNullOrWhiteSpace(request.EnteredBy))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "EnteredBy is required." });

            try
            {
                var result = _repo.SaveFinalEntry(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/accounts/budget-book/list?finYear=2025-2026&deptCode=995&pageNo=1&pageSize=50

        [HttpGet]
        [Route("list")]
        [AllowAnonymous]
        public IHttpActionResult ListEntries(
            [FromUri] string finYear,
            [FromUri] int? deptCode = null,
            [FromUri] int pageNo = 1,
            [FromUri] int pageSize = 20,
            [FromUri] string search = null,
            [FromUri] string status = null)
        {
            if (string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear is required." });
            if (pageNo < 1) pageNo = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            try
            {
                int totalCount;
                var entries = _repo.ListEntries(DEFAULT_ULB_CODE, finYear, deptCode, pageNo, pageSize, out totalCount, search, status);
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                return Ok(new { success = true, data = entries, totalCount, totalPages, pageNo, pageSize });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}
