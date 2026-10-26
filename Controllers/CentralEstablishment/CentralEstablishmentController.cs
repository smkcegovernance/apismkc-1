using System;
using System.Net;
using System.Web.Http;
using SmkcApi.Models.CentralEstablishment;
using SmkcApi.Repositories.CentralEstablishment;

namespace SmkcApi.Controllers.CentralEstablishment
{
    [RoutePrefix("api/central-establishment-cell")]
    public class CentralEstablishmentController : ApiController
    {
        private readonly ICentralEstablishmentRepository _repository;

        public CentralEstablishmentController(ICentralEstablishmentRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException("repository");
        }

        [HttpGet]
        [Route("dashboard")]
        [AllowAnonymous]
        public IHttpActionResult GetDashboard()
        {
            try
            {
                var summary = _repository.GetDashboardSummary();
                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("department-strength")]
        [AllowAnonymous]
        public IHttpActionResult GetDepartmentStrength()
        {
            try
            {
                var rows = _repository.GetDepartmentStrength();
                return Ok(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("officer-hierarchy")]
        [AllowAnonymous]
        public IHttpActionResult GetOfficerHierarchy()
        {
            try
            {
                var rows = _repository.GetOfficerHierarchy();
                return Ok(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("employee-directory")]
        [AllowAnonymous]
        public IHttpActionResult GetEmployeeDirectory([FromUri] string q = null, [FromUri] int pageSize = 50)
        {
            try
            {
                var rows = _repository.GetEmployeeDirectory(q, pageSize);
                return Ok(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("postings")]
        [AllowAnonymous]
        public IHttpActionResult GetPostings([FromUri] string q = null, [FromUri] int pageSize = 50)
        {
            try
            {
                var rows = _repository.GetPostingTransactions(q, pageSize);
                return Ok(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("order-register")]
        [AllowAnonymous]
        public IHttpActionResult GetOrderRegister([FromUri] string q = null, [FromUri] int pageSize = 50)
        {
            try
            {
                var rows = _repository.GetOrderRegister(q, pageSize);
                return Ok(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("employee/{employeeId}")]
        [AllowAnonymous]
        public IHttpActionResult GetEmployeeDetail(string employeeId)
        {
            try
            {
                var detail = _repository.GetEmployeeDetail(employeeId);
                return Ok(new { success = true, data = detail });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("employee-action/department-transfer")]
        [AllowAnonymous]
        public IHttpActionResult SaveDepartmentTransfer([FromBody] EmsEmployeeActionRequest request)
        {
            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "Department transfer request body is required." });
            }

            try
            {
                var result = _repository.ProcessDepartmentTransfer(request);
                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest,
                        new { success = false, message = result.Message, data = result });
                }

                return Ok(new { success = true, message = result.Message, data = result });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("employee-action/designation-change")]
        [AllowAnonymous]
        public IHttpActionResult SaveDesignationChange([FromBody] EmsEmployeeActionRequest request)
        {
            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "Designation change request body is required." });
            }

            try
            {
                var result = _repository.ProcessDesignationChange(request);
                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest,
                        new { success = false, message = result.Message, data = result });
                }

                return Ok(new { success = true, message = result.Message, data = result });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("employee-action/incharge-assignment")]
        [AllowAnonymous]
        public IHttpActionResult SaveInChargeAssignment([FromBody] EmsEmployeeActionRequest request)
        {
            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "In-charge assignment request body is required." });
            }

            try
            {
                var result = _repository.ProcessInChargeAssignment(request);
                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest,
                        new { success = false, message = result.Message, data = result });
                }

                return Ok(new { success = true, message = result.Message, data = result });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}
