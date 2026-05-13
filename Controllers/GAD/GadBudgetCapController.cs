using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Models.GAD;
using SmkcApi.Repositories.GAD;

namespace SmkcApi.Controllers.GAD
{
    [AllowAnonymous]
    [RoutePrefix("api/gad/budget-cap")]
    public class GadBudgetCapController : ApiController
    {
        private readonly IGadBudgetCapRepository _repo;

        public GadBudgetCapController(IGadBudgetCapRepository repo)
        {
            _repo = repo;
        }

        // GET api/gad/budget-cap/codes?finYear=2026-2027
        [HttpGet, Route("codes")]
        public IHttpActionResult GetCodes([FromUri] string finYear)
        {
            if (string.IsNullOrWhiteSpace(finYear)) return BadRequest("finYear is required.");
            try
            {
                var data = _repo.GetBudgetCodes(finYear);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/gad/budget-cap?ulbCode=1&finYear=2026-2027
        [HttpGet, Route("")]
        public IHttpActionResult List([FromUri] int ulbCode, [FromUri] string finYear)
        {
            try
            {
                var data = _repo.ListCaps(ulbCode, finYear);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/gad/budget-cap/single?ulbCode=1&acSubhead=E-4219&finYear=2026-2027
        [HttpGet, Route("single")]
        public IHttpActionResult GetSingle([FromUri] int ulbCode, [FromUri] string acSubhead, [FromUri] string finYear)
        {
            try
            {
                var cap = _repo.GetCap(ulbCode, acSubhead, finYear);
                if (cap == null)
                    return Ok(new { exists = false });
                return Ok(new { exists = true, data = cap });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST api/gad/budget-cap
        [HttpPost, Route("")]
        public IHttpActionResult Upsert([FromBody] BudgetCapSaveRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");
            if (string.IsNullOrWhiteSpace(req.AcSubhead)) return BadRequest("AcSubhead is required.");
            if (string.IsNullOrWhiteSpace(req.FinYear))   return BadRequest("FinYear is required.");
            if (string.IsNullOrWhiteSpace(req.ActionBy))  return BadRequest("ActionBy is required.");
            if (req.CapPercentage == null && req.CapAmount == null)
                return BadRequest("Either CapPercentage or CapAmount must be provided.");

            try
            {
                _repo.UpsertCap(req);
                return Ok(new { success = true, message = "बजेट मर्यादा यशस्वीरित्या जतन केली." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // DELETE api/gad/budget-cap?ulbCode=1&acSubhead=E-4219&finYear=2026-2027&actionBy=admin
        [HttpDelete, Route("")]
        public IHttpActionResult Delete([FromUri] int ulbCode, [FromUri] string acSubhead,
                                        [FromUri] string finYear, [FromUri] string actionBy)
        {
            if (string.IsNullOrWhiteSpace(actionBy)) return BadRequest("actionBy is required.");
            try
            {
                _repo.DeleteCap(ulbCode, acSubhead, finYear, actionBy);
                return Ok(new { success = true, message = "बजेट मर्यादा रद्द केली." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // GET api/gad/budget-cap/history?acSubhead=E-4219&finYear=2026-2027
        [HttpGet, Route("history")]
        public IHttpActionResult History([FromUri] string acSubhead, [FromUri] string finYear)
        {
            try
            {
                var hist = _repo.GetHistory(acSubhead, finYear);
                return Ok(hist);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
