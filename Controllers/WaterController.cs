using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using SmkcApi.Models;
using SmkcApi.Security;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    [RoutePrefix("api/water")]
    //[ShaAuthentication] // Removed: API key authentication disabled
    //[IPWhitelist]
    [RateLimit(maxRequests: 120, timeWindowMinutes: 1)]
    public class WaterController : ApiController
    {
        private readonly IWaterService _waterService;
        private readonly ISmsService _smsService;

        // inject both services
        public WaterController(IWaterService waterService, ISmsService smsService)
        {
            _waterService = waterService;
            _smsService = smsService;
        }

        [HttpPost, Route("sms/send")]
        public async Task<IHttpActionResult> SendSms([FromBody] SmsSendRequest req)
        {
            if (req == null) return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body required", "MISSING_BODY"));

            // Validate input: either connection or ward/div
            if (string.IsNullOrWhiteSpace(req.ConnectionNumber) &&
                (string.IsNullOrWhiteSpace(req.WardCode) || string.IsNullOrWhiteSpace(req.DivCode)))
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Provide connectionNumber OR wardCode+divCode", "INVALID_PARAMS"));
            }

            var results = await _smsService.SendBulkSmsAsync(req);
            return Ok(ApiResponse<List<SmsSendResult>>.CreateSuccess(results, "SMS dispatch attempted"));
        }

        // New endpoint for water connection bill SMS with customer details
        [HttpPost, Route("sms/bill/send")]
        public async Task<IHttpActionResult> SendWaterBillSms([FromBody] WaterBillSmsSendRequest req)
        {
            if (req == null) return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body required", "MISSING_BODY"));

            // Validate input: either connection or ward/div
            if (string.IsNullOrWhiteSpace(req.ConnectionNumber) &&
                (string.IsNullOrWhiteSpace(req.WardCode) || string.IsNullOrWhiteSpace(req.DivCode)))
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Provide connectionNumber OR wardCode+divCode", "INVALID_PARAMS"));
            }

            var results = await _smsService.SendWaterBillSmsAsync(req);
            return Ok(ApiResponse<List<SmsSendResult>>.CreateSuccess(results, "Water bill SMS dispatch attempted"));
        }

        // Bill Fetch: POST /api/water/bill/fetch
        [HttpPost, Route("bill/fetch")]
        public async Task<IHttpActionResult> BillFetch([FromBody] BillFetchRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConsumerNo))
                return Content(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("consumerNo is required", "INVALID_CONSUMER_NO"));

            var result = await _waterService.FetchBillAsync(req);

            if (result == null)
                return NotFound();

            return Ok(ApiResponse<BillFetchResponse>.CreateSuccess(result, "Bill fetched"));
        }
    }
}
