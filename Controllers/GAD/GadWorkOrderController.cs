using System;
using System.Net;
using System.Web.Http;
using SmkcApi.Models.GAD;
using SmkcApi.Repositories.GAD;

namespace SmkcApi.Controllers.GAD
{
    [RoutePrefix("api/gad/work-order")]
    public class GadWorkOrderController : ApiController
    {
        private readonly IGadWorkOrderRepository _repo;
        private const int DEFAULT_ULB_CODE = 1;

        public GadWorkOrderController(IGadWorkOrderRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException("repo");
        }

        // GET api/gad/work-order/validate-nasti?nastiNo=700744&finYear=2026-2027
        [HttpGet, Route("validate-nasti"), AllowAnonymous]
        public IHttpActionResult ValidateNasti([FromUri] int nastiNo, [FromUri] string finYear = null)
        {
            if (nastiNo <= 0)
                return BadRequest("nastiNo must be a positive integer.");
            try
            {
                var result = _repo.ValidateNastiForWorkOrder(DEFAULT_ULB_CODE, nastiNo, finYear ?? string.Empty);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/gad/work-order/sanction-authorities
        [HttpGet, Route("sanction-authorities"), AllowAnonymous]
        public IHttpActionResult GetSanctionAuthorities()
        {
            try
            {
                var data = _repo.GetSanctionAuthorities(DEFAULT_ULB_CODE);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/gad/work-order/signing-officers?deptCode=16
        [HttpGet, Route("signing-officers"), AllowAnonymous]
        public IHttpActionResult GetSigningOfficers([FromUri] int deptCode)
        {
            if (deptCode <= 0)
                return BadRequest("deptCode must be a positive integer.");
            try
            {
                var data = _repo.GetSigningOfficers(DEFAULT_ULB_CODE, deptCode);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/gad/work-order/next-number?deptCode=16&finYear=2026-2027
        [HttpGet, Route("next-number"), AllowAnonymous]
        public IHttpActionResult GetNextNumber([FromUri] int deptCode, [FromUri] string finYear)
        {
            if (deptCode <= 0 || string.IsNullOrWhiteSpace(finYear))
                return BadRequest("deptCode and finYear are required.");
            try
            {
                var data = _repo.GetNextWorkOrderNumber(DEFAULT_ULB_CODE, deptCode, finYear);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST api/gad/work-order/generate
        [HttpPost, Route("generate"), AllowAnonymous]
        public IHttpActionResult Generate([FromBody] GenerateWorkOrderRequest req)
        {
            if (req == null)
                return BadRequest("Request body is required.");
            if (string.IsNullOrWhiteSpace(req.NastiNo))
                return BadRequest("NastiNo is required.");
            if (req.DeptCode <= 0)
                return BadRequest("DeptCode is required.");
            if (string.IsNullOrWhiteSpace(req.FinYear))
                return BadRequest("FinYear is required.");
            if (req.AuthorityOfficials <= 0)
                return BadRequest("AuthorityOfficials is required.");
            if (req.SigningAuth <= 0)
                return BadRequest("SigningAuth is required.");
            try
            {
                var result = _repo.GenerateWorkOrder(DEFAULT_ULB_CODE, req);
                if (result.Success)
                    return Ok(new { success = true, data = result });
                return Content(HttpStatusCode.BadRequest, new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/gad/work-order/detail?workOrderNo=&deptCode=&finYear=
        [HttpGet, Route("detail"), AllowAnonymous]
        public IHttpActionResult GetDetail([FromUri] long workOrderNo, [FromUri] int deptCode, [FromUri] string finYear)
        {
            if (workOrderNo <= 0 || deptCode <= 0 || string.IsNullOrWhiteSpace(finYear))
                return BadRequest("workOrderNo, deptCode, and finYear are required.");
            try
            {
                var data = _repo.GetWorkOrderDetail(DEFAULT_ULB_CODE, workOrderNo, deptCode, finYear);
                if (data == null)
                    return NotFound();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/gad/work-order/list?finYear=2026-2027&userId=
        [HttpGet, Route("list"), AllowAnonymous]
        public IHttpActionResult GetList([FromUri] string finYear, [FromUri] string userId = null)
        {
            if (string.IsNullOrWhiteSpace(finYear))
                return BadRequest("finYear is required.");
            try
            {
                var data = _repo.GetWorkOrderList(DEFAULT_ULB_CODE, finYear, userId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
