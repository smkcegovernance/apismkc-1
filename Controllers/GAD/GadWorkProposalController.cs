using System;
using System.Net;
using System.Web.Http;
using SmkcApi.Models.GAD;
using SmkcApi.Repositories.GAD;

namespace SmkcApi.Controllers.GAD
{
    [RoutePrefix("api/gad/work-proposals")]
    public class GadWorkProposalController : ApiController
    {
        private const int DEFAULT_ULB_CODE = 1;

        private readonly IGadWorkProposalRepository _repo;

        public GadWorkProposalController(IGadWorkProposalRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException("repo");
        }

        // ── GET api/gad/work-proposals/departments ────────────────────────────────

        [HttpGet]
        [Route("departments")]
        [AllowAnonymous]
        public IHttpActionResult GetDepartments([FromUri] string userId = null)
        {
            try
            {
                var data = _repo.GetDepartments(DEFAULT_ULB_CODE, userId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/work-proposals/account-heads?deptCode=5&finYear=2026-2027 ─

        [HttpGet]
        [Route("account-heads")]
        [AllowAnonymous]
        public IHttpActionResult GetAccountHeads([FromUri] int deptCode, [FromUri] string finYear)
        {
            if (deptCode <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "deptCode is required." });
            if (string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "finYear is required." });

            try
            {
                var data = _repo.GetAccountHeads(DEFAULT_ULB_CODE, deptCode, finYear);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/work-proposals/budget?deptCode=5&acSubhead=E-4219&finYear=2026-2027

        [HttpGet]
        [Route("budget")]
        [AllowAnonymous]
        public IHttpActionResult GetBudgetInfo([FromUri] int deptCode, [FromUri] string acSubhead, [FromUri] string finYear, [FromUri] string proposalType = null)
        {
            if (deptCode < 0 || string.IsNullOrWhiteSpace(acSubhead) || string.IsNullOrWhiteSpace(finYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "deptCode, acSubhead and finYear are required." });

            try
            {
                var data = _repo.GetBudgetInfo(DEFAULT_ULB_CODE, deptCode, acSubhead, finYear, proposalType);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── POST api/gad/work-proposals/under-10l ────────────────────────────────

        [HttpPost]
        [Route("under-10l")]
        [AllowAnonymous]
        public IHttpActionResult SaveUnder10L([FromBody] Under10LProposalRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            // Server-side validation
            if (string.IsNullOrWhiteSpace(request.FinYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "FinYear is required." });
            if (request.DeptCode <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "DeptCode is required." });
            if (string.IsNullOrWhiteSpace(request.WorkName))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "WorkName is required." });
            if (string.IsNullOrWhiteSpace(request.AcSubhead))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "AcSubhead is required." });
            if (request.ProposalCost <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "ProposalCost must be greater than zero." });
            if (request.ProposalCost > 100000m)
                return Content((HttpStatusCode)422, new { success = false, message = "ProposalCost must not exceed ₹1,00,000 for a quotation proposal." });
            if (string.IsNullOrWhiteSpace(request.EnteredBy))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "EnteredBy is required." });

            try
            {
                var result = _repo.SaveUnder10LProposal(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── POST api/gad/work-proposals/other ────────────────────────────────────

        [HttpPost]
        [Route("other")]
        [AllowAnonymous]
        public IHttpActionResult SaveOther([FromBody] Under10LProposalRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.FinYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "FinYear is required." });
            if (request.DeptCode <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "DeptCode is required." });
            if (string.IsNullOrWhiteSpace(request.WorkName))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "WorkName is required." });
            if (string.IsNullOrWhiteSpace(request.AcSubhead))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "AcSubhead is required." });
            if (request.ProposalCost <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "ProposalCost must be greater than zero." });
            if (string.IsNullOrWhiteSpace(request.EnteredBy))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "EnteredBy is required." });

            // Force inward type to "other"
            request.InwardType = "other";

            try
            {
                var result = _repo.SaveUnder10LProposal(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── POST api/gad/work-proposals/over-10l ─────────────────────────────────

        [HttpPost]
        [Route("over-10l")]
        [AllowAnonymous]
        public IHttpActionResult SaveOver10L([FromBody] Over10LProposalRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });

            // Server-side validation
            if (string.IsNullOrWhiteSpace(request.FinYear))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "FinYear is required." });
            if (request.DeptCode <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "DeptCode is required." });
            if (string.IsNullOrWhiteSpace(request.WorkName))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "WorkName is required." });
            if (string.IsNullOrWhiteSpace(request.AcSubhead))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "AcSubhead is required." });
            if (request.ProposalCost <= 100000m)
                return Content((HttpStatusCode)422, new { success = false, message = "ProposalCost must exceed ₹1,00,000 for a tender proposal." });
            if (string.IsNullOrWhiteSpace(request.EnteredBy))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "EnteredBy is required." });

            try
            {
                var result = _repo.SaveOver10LProposal(DEFAULT_ULB_CODE, request);
                if (!result.Success)
                    return Content((HttpStatusCode)422, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/work-proposals/list?userId=X&finYear=Y ───────────────────

        [HttpGet]
        [Route("list")]
        [AllowAnonymous]
        public IHttpActionResult GetList([FromUri] string userId = null, [FromUri] string finYear = null, [FromUri] bool requireAccountRemark = false, [FromUri] string search = null, [FromUri] int pageSize = 20, [FromUri] int pageNo = 1)
        {
            try
            {
                int totalCount;
                var data = _repo.GetProposals(DEFAULT_ULB_CODE, userId, finYear, requireAccountRemark, search, pageSize, pageNo, out totalCount);
                int totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
                return Ok(new { success = true, data, totalCount, totalPages, pageNo, pageSize });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/work-proposals/detail?type=Q&orderNo=1234 ────────────────

        [HttpGet]
        [Route("detail")]
        [AllowAnonymous]
        public IHttpActionResult GetDetail([FromUri] string type, [FromUri] long orderNo)
        {
            if (string.IsNullOrWhiteSpace(type) || (type != "Q" && type != "T"))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "type must be 'Q' or 'T'." });
            if (orderNo <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "orderNo is required." });

            try
            {
                var data = _repo.GetProposalDetail(DEFAULT_ULB_CODE, type, orderNo);
                if (data == null)
                    return Content(HttpStatusCode.NotFound, new { success = false, message = "Proposal not found." });
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // GET api/gad/work-proposals/next-sequence?type=Q
        [HttpGet]
        [Route("next-sequence")]
        [AllowAnonymous]
        public IHttpActionResult GetNextSequence([FromUri] string type = "Q")
        {
            if (type != "Q" && type != "T")
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "type must be 'Q' or 'T'." });
            try
            {
                var nextVal = _repo.GetNextSequence(type);
                return Ok(new { success = true, nextVal });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/work-proposals/for-remark?type=Q&orderNo=1234&remarkType=audit ─

        [HttpGet]
        [Route("for-remark")]
        [AllowAnonymous]
        public IHttpActionResult GetForRemark([FromUri] string type, [FromUri] long orderNo, [FromUri] string remarkType = "audit")
        {
            if (string.IsNullOrWhiteSpace(type) || (type != "Q" && type != "T"))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "type must be 'Q' or 'T'." });
            if (orderNo <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "orderNo is required." });
            if (remarkType != "audit" && remarkType != "account")
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "remarkType must be 'audit' or 'account'." });

            try
            {
                var data = _repo.GetProposalForRemark(DEFAULT_ULB_CODE, type, orderNo, remarkType);
                if (data == null)
                    return Content(HttpStatusCode.NotFound, new { success = false, message = "Proposal not found." });
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── POST api/gad/work-proposals/save-remark ───────────────────────────────

        [HttpPost]
        [Route("save-remark")]
        [AllowAnonymous]
        public IHttpActionResult SaveRemark([FromBody] SaveRemarkRequest request)
        {
            if (request == null)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "Request body is required." });
            if (request.OrderNo <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "OrderNo is required." });
            if (string.IsNullOrWhiteSpace(request.ProposalType) || (request.ProposalType != "Q" && request.ProposalType != "T"))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "ProposalType must be 'Q' or 'T'." });
            if (string.IsNullOrWhiteSpace(request.RemarkType) || (request.RemarkType != "audit" && request.RemarkType != "account"))
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "RemarkType must be 'audit' or 'account'." });

            try
            {
                var saved = _repo.SaveProposalRemark(DEFAULT_ULB_CODE, request);
                if (!saved)
                    return Content((HttpStatusCode)422, new { success = false, message = "अभिप्राय जतन होऊ शकला नाही." });
                return Ok(new { success = true, message = "अभिप्राय यशस्वीपणे जतन केला." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ── GET api/gad/work-proposals/by-nastino?nastiNo=123&finYear=2025-2026 ───

        [HttpGet]
        [Route("by-nastino")]
        [AllowAnonymous]
        public IHttpActionResult GetByNastiNo([FromUri] long nastiNo, [FromUri] string finYear = null)
        {
            if (nastiNo <= 0)
                return Content(HttpStatusCode.BadRequest, new { success = false, message = "nastiNo is required." });

            try
            {
                var data = _repo.GetProposalByNastiNo(DEFAULT_ULB_CODE, nastiNo, finYear);
                if (data == null)
                    return Content(HttpStatusCode.NotFound, new { success = false, message = "या नस्ती क्रमांकाशी कोणताही प्रस्ताव आढळला नाही." });
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}
