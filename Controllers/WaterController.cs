using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
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
        private readonly IWaterService          _waterService;
        private readonly ISmsService            _smsService;
        private readonly IWaterDashboardService _dashboardService;

        public WaterController(IWaterService waterService, ISmsService smsService, IWaterDashboardService dashboardService)
        {
            _waterService     = waterService;
            _smsService       = smsService;
            _dashboardService = dashboardService;
        }

        // ── Dashboard: Revenue & Collection ──────────────────────────────────

        /// <summary>
        /// GET /api/water/dashboard/revenue?finyr=2026-2027&amp;wardCode=0&amp;divCode=0
        /// </summary>
        [HttpGet, Route("dashboard/revenue")]
        public async Task<IHttpActionResult> GetRevenueDashboard(
            string finyr     = "2026-2027",
            string wardCode  = "0",
            string divCode   = "0")
        {
            var data = await _dashboardService.GetRevenueDashboardAsync(finyr, wardCode, divCode);
            return Ok(ApiResponse<WaterRevenueDashboard>.CreateSuccess(data, "Revenue dashboard loaded"));
        }

        /// <summary>
        /// GET /api/water/dashboard/connections?wardCode=0&amp;divCode=0
        /// </summary>
        [HttpGet, Route("dashboard/connections")]
        public async Task<IHttpActionResult> GetConnectionsDashboard(string wardCode = "0", string divCode = "0")
        {
            var data = await _dashboardService.GetConnectionDashboardAsync(wardCode, divCode);
            return Ok(ApiResponse<WaterConnectionDashboard>.CreateSuccess(data, "Connections dashboard loaded"));
        }

        /// <summary>
        /// GET /api/water/dashboard/divisions?wardCode=0
        /// </summary>
        [HttpGet, Route("dashboard/divisions")]
        public async Task<IHttpActionResult> GetDivisions(string wardCode = "0")
        {
            var data = await _dashboardService.GetDivisionsAsync(wardCode);
            return Ok(ApiResponse<List<DivisionItem>>.CreateSuccess(data, "Divisions loaded"));
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

        // Payment Success Authorization: POST /api/water/collection/success
        // Also supports the Razorpay callback query string parameters sent to the success page.
        [HttpGet, HttpPost, Route("collection/success")]
        public async Task<IHttpActionResult> AuthorizeCollection()
        {
            var req = new WaterCollectionSuccessRequest();

            if (Request != null && Request.RequestUri != null)
            {
                foreach (var pair in Request.GetQueryNameValuePairs())
                {
                    var key = pair.Key ?? string.Empty;
                    var value = pair.Value ?? string.Empty;

                    switch (key)
                    {
                        case "order_id":
                            req.OrderId = value;
                            break;
                        case "razorpay_payment_id":
                        case "payment_id":
                            req.RazorpayPaymentId = value;
                            break;
                        case "razorpay_payment_link_id":
                        case "payment_link_id":
                            req.RazorpayPaymentLinkId = value;
                            break;
                        case "razorpay_payment_link_reference_id":
                        case "payment_link_reference_id":
                            req.RazorpayPaymentLinkReferenceId = value;
                            break;
                        case "razorpay_payment_link_status":
                        case "payment_link_status":
                            req.RazorpayPaymentLinkStatus = value;
                            req.Status = value;
                            break;
                        case "razorpay_signature":
                        case "signature":
                            req.RazorpaySignature = value;
                            break;
                    }
                }
            }

            if (Request != null && Request.Content != null && Request.Content.Headers.ContentLength.GetValueOrDefault() > 0)
            {
                var bodyText = await Request.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(bodyText))
                {
                    try
                    {
                        var bodyObj = JObject.Parse(bodyText);

                        SetLongFromToken(bodyObj, "connectionNo", v => req.ConnectionNo = v);
                        SetLongFromToken(bodyObj, "bookNo", v => req.BookNo = v);
                        SetLongFromToken(bodyObj, "srNo", v => req.SrNo = v);
                        SetLongFromToken(bodyObj, "receiptNo", v => req.ReceiptNo = v);
                        SetDecimalFromToken(bodyObj, "totalPaid", v => req.TotalPaid = v);
                        SetLongFromToken(bodyObj, "transactionNo", v => req.TransactionNo = v);

                        SetStringFromToken(bodyObj, "order_id", v => req.OrderId = v);
                        SetStringFromToken(bodyObj, "razorpay_payment_id", v => req.RazorpayPaymentId = v);
                        SetStringFromToken(bodyObj, "payment_id", v => req.RazorpayPaymentId = v);
                        SetStringFromToken(bodyObj, "razorpay_payment_link_id", v => req.RazorpayPaymentLinkId = v);
                        SetStringFromToken(bodyObj, "payment_link_id", v => req.RazorpayPaymentLinkId = v);
                        SetStringFromToken(bodyObj, "razorpay_payment_link_reference_id", v => req.RazorpayPaymentLinkReferenceId = v);
                        SetStringFromToken(bodyObj, "payment_link_reference_id", v => req.RazorpayPaymentLinkReferenceId = v);
                        SetStringFromToken(bodyObj, "razorpay_payment_link_status", v => req.RazorpayPaymentLinkStatus = v);
                        SetStringFromToken(bodyObj, "payment_link_status", v => req.RazorpayPaymentLinkStatus = v);
                        SetStringFromToken(bodyObj, "razorpay_signature", v => req.RazorpaySignature = v);
                        SetStringFromToken(bodyObj, "signature", v => req.RazorpaySignature = v);
                        SetStringFromToken(bodyObj, "status", v => req.Status = v);
                    }
                    catch (Exception ex)
                    {
                        // Ignore malformed JSON and continue with query-string-based fallback values.
                        System.Diagnostics.Trace.TraceWarning("[WaterController] collection/success body parse failed. Body: " + bodyText + ". Error: " + ex);
                    }
                }
            }

            var secretKey = ConfigurationManager.AppSettings["Razorpay_KeySecret"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                System.Diagnostics.Trace.TraceError("[WaterController] Razorpay_KeySecret is missing in Web.config.");
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.CreateError("Razorpay secret is not configured", "RAZORPAY_CONFIG_MISSING"));
            }
            req.RazorpayPaymentId = (req.RazorpayPaymentId ?? string.Empty).Trim();
            req.RazorpayPaymentLinkId = (req.RazorpayPaymentLinkId ?? string.Empty).Trim();
            req.RazorpayPaymentLinkReferenceId = (req.RazorpayPaymentLinkReferenceId ?? string.Empty).Trim();
            req.RazorpayPaymentLinkStatus = (req.RazorpayPaymentLinkStatus ?? string.Empty).Trim();
            req.RazorpaySignature = (req.RazorpaySignature ?? string.Empty).Trim();

            var hasPaymentId = !string.IsNullOrWhiteSpace(req.RazorpayPaymentId);
            var hasSignature = !string.IsNullOrWhiteSpace(req.RazorpaySignature);
            var signatureIsValid = !hasPaymentId && !hasSignature;

            if (hasPaymentId || hasSignature)
            {
                if (!hasPaymentId || !hasSignature)
                {
                    signatureIsValid = false;
                }
                else
                {
                    var statusForSignature = !string.IsNullOrWhiteSpace(req.RazorpayPaymentLinkStatus)
                        ? req.RazorpayPaymentLinkStatus
                        : (req.Status ?? string.Empty).Trim();

                    var payloadToVerify = BuildRazorpayPaymentLinkSignaturePayload(
                        req.RazorpayPaymentLinkId,
                        req.RazorpayPaymentLinkReferenceId,
                        statusForSignature,
                        req.RazorpayPaymentId);

                    System.Diagnostics.Trace.TraceInformation(
                        "[WaterController] Signature verification request. PaymentId: " + req.RazorpayPaymentId +
                        ", PaymentLinkId: " + req.RazorpayPaymentLinkId +
                        ", ReferenceId: " + req.RazorpayPaymentLinkReferenceId +
                        ", Status: " + statusForSignature +
                        ", PayloadForSignature: " + payloadToVerify +
                        ", ReceivedSignature: " + req.RazorpaySignature);

                    if (string.IsNullOrWhiteSpace(payloadToVerify))
                    {
                        signatureIsValid = false;
                    }
                    else
                    {
                        var expectedSignature = GenerateRazorpaySignature(payloadToVerify, secretKey);
                        signatureIsValid = string.Equals(expectedSignature, req.RazorpaySignature, StringComparison.OrdinalIgnoreCase);

                        // Backward compatibility guard:
                        // some legacy integrations verify as payment_link_id|payment_id.
                        if (!signatureIsValid && !string.IsNullOrWhiteSpace(req.RazorpayPaymentLinkId))
                        {
                            var legacyPayload = req.RazorpayPaymentLinkId + "|" + req.RazorpayPaymentId;
                            var legacySignature = GenerateRazorpaySignature(legacyPayload, secretKey);
                            signatureIsValid = string.Equals(legacySignature, req.RazorpaySignature, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }

            if (!signatureIsValid)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "[WaterController] Invalid Razorpay callback signature. PaymentId: " + req.RazorpayPaymentId +
                    ", PaymentLinkId: " + req.RazorpayPaymentLinkId +
                    ", ReferenceId: " + req.RazorpayPaymentLinkReferenceId +
                    ", Status: " + req.RazorpayPaymentLinkStatus);

                return Content(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    message = "Invalid Razorpay signature.",
                    status = "failed",
                    razorpay_payment_id = req.RazorpayPaymentId,
                    razorpay_payment_link_id = req.RazorpayPaymentLinkId,
                    razorpay_payment_link_reference_id = req.RazorpayPaymentLinkReferenceId,
                    razorpay_payment_link_status = req.RazorpayPaymentLinkStatus
                });
            }

            System.Diagnostics.Trace.TraceInformation(
                "[WaterController] Signature verification success. PaymentId: " + req.RazorpayPaymentId +
                ", PaymentLinkId: " + req.RazorpayPaymentLinkId +
                ", ReferenceId: " + req.RazorpayPaymentLinkReferenceId +
                ", Status: " + req.RazorpayPaymentLinkStatus);

            // The Razorpay callback includes payment status in the query string.
            // If the callback status is not paid, do not call the Oracle collection auth routine.
            var paymentStatus = !string.IsNullOrWhiteSpace(req.RazorpayPaymentLinkStatus)
                ? req.RazorpayPaymentLinkStatus
                : req.Status;

            if (!string.IsNullOrWhiteSpace(paymentStatus) &&
                !string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(paymentStatus, "captured", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    success = false,
                    message = "Payment not successful. Current payment status: " + paymentStatus,
                    status = paymentStatus,
                    razorpay_payment_id = req.RazorpayPaymentId,
                    razorpay_payment_link_reference_id = req.RazorpayPaymentLinkReferenceId,
                    razorpay_payment_link_status = req.RazorpayPaymentLinkStatus
                });
            }

            // If reference id is provided, try to infer connection number from it.
            if (req.ConnectionNo <= 0 && !string.IsNullOrWhiteSpace(req.RazorpayPaymentLinkReferenceId))
            {
                var match = System.Text.RegularExpressions.Regex.Match(req.RazorpayPaymentLinkReferenceId, @"\d+");
                if (match.Success)
                {
                    long parsedConnectionNo;
                    if (long.TryParse(match.Value, out parsedConnectionNo))
                    {
                        req.ConnectionNo = parsedConnectionNo;
                    }
                }
            }

            // When calling this endpoint from the callback page, the payment callback usually contains
            // the payment status, payment id, and reference id. The actual collection auth needs the
            // connection/account details too (connectionNo, totalPaid, transactionNo, etc.), which the page
            // should send along in the request body.
            if (req.ConnectionNo <= 0 || req.BookNo <= 0 || req.SrNo <= 0 || req.ReceiptNo <= 0 ||
                req.TotalPaid <= 0 || req.TransactionNo <= 0)
            {
                return Ok(new
                {
                    success = false,
                    message = "Payment succeeded, but collection details are incomplete. Please send connectionNo, bookNo, srNo, receiptNo, totalPaid and transactionNo before calling collection authorization.",
                    status = paymentStatus ?? "paid",
                    razorpay_payment_id = req.RazorpayPaymentId,
                    razorpay_payment_link_reference_id = req.RazorpayPaymentLinkReferenceId,
                    razorpay_payment_link_status = req.RazorpayPaymentLinkStatus,
                    required_fields = new[]
                    {
                        "connectionNo",
                        "bookNo",
                        "srNo",
                        "receiptNo",
                        "totalPaid",
                        "transactionNo"
                    }
                });
            }

            try
            {
                var result = await _waterService.AuthorizeCollectionAsync(req);
                if (result.Authorized)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Payment successful and collection authorized.",
                        status = paymentStatus ?? "paid",
                        razorpay_payment_id = req.RazorpayPaymentId,
                        razorpay_payment_link_reference_id = req.RazorpayPaymentLinkReferenceId,
                        razorpay_payment_link_status = req.RazorpayPaymentLinkStatus,
                        data = result
                    });
                }

                return Ok(new
                {
                    success = false,
                    message = result.Message,
                    status = paymentStatus ?? "paid",
                    razorpay_payment_id = req.RazorpayPaymentId,
                    razorpay_payment_link_reference_id = req.RazorpayPaymentLinkReferenceId,
                    razorpay_payment_link_status = req.RazorpayPaymentLinkStatus,
                    data = result
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "[WaterController] Collection authorization failed. ConnNo: " + req.ConnectionNo +
                    ", TxnNo: " + req.TransactionNo + ", PaymentId: " + req.RazorpayPaymentId + ", Error: " + ex);

                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.CreateError("Failed to authorize collection", "COLLECTION_AUTH_ERROR"));
            }
        }

        private static void SetStringFromToken(JObject payload, string tokenName, Action<string> setter)
        {
            if (payload == null || string.IsNullOrWhiteSpace(tokenName)) return;

            var token = payload[tokenName];
            if (token != null && token.Type != JTokenType.Null)
            {
                var value = token.Value<string>();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    setter(value);
                }
            }
        }

        private static void SetLongFromToken(JObject payload, string tokenName, Action<long> setter)
        {
            if (payload == null || string.IsNullOrWhiteSpace(tokenName)) return;

            var token = payload[tokenName];
            if (token != null && token.Type != JTokenType.Null)
            {
                long value;
                if (long.TryParse(token.Value<string>(), out value))
                {
                    setter(value);
                }
            }
        }

        private static void SetDecimalFromToken(JObject payload, string tokenName, Action<decimal> setter)
        {
            if (payload == null || string.IsNullOrWhiteSpace(tokenName)) return;

            var token = payload[tokenName];
            if (token != null && token.Type != JTokenType.Null)
            {
                decimal value;
                if (decimal.TryParse(token.Value<string>(), out value))
                {
                    setter(value);
                }
            }
        }

        private static string BuildRazorpayPaymentLinkSignaturePayload(
            string paymentLinkId,
            string paymentLinkReferenceId,
            string paymentLinkStatus,
            string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentLinkId) ||
                string.IsNullOrWhiteSpace(paymentId))
            {
                return string.Empty;
            }

            return (paymentLinkId ?? string.Empty) + "|" +
                   (paymentLinkReferenceId ?? string.Empty) + "|" +
                   (paymentLinkStatus ?? string.Empty) + "|" +
                   (paymentId ?? string.Empty);
        }

        private static string GenerateRazorpaySignature(string payload, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // Razorpay payment link creation: POST /api/water/payment-link/create
        [HttpPost, Route("payment-link/create")]
        public async Task<IHttpActionResult> CreatePaymentLink([FromBody] RazorpayPaymentLinkRequest req)
        {
            if (req == null)
                return Content(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("Request body required", "MISSING_BODY"));

            if (string.IsNullOrWhiteSpace(req.Name) ||
                string.IsNullOrWhiteSpace(req.Contact) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                req.Amount <= 0)
            {
                return Content(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError(
                        "name, contact, email and amount are required",
                        "INVALID_PAYMENT_REQUEST"));
            }

            try
            {
                var keyId = ConfigurationManager.AppSettings["Razorpay_KeyId"];
                var secret = ConfigurationManager.AppSettings["Razorpay_KeySecret"];
                var baseUrl = ConfigurationManager.AppSettings["Razorpay_PaymentLinkBaseUrl"] ?? "https://api.razorpay.com/v1/payment_links/";
                var callbackUrl = !string.IsNullOrWhiteSpace(req.CallbackUrl)
                    ? req.CallbackUrl
                    : (ConfigurationManager.AppSettings["Razorpay_CallbackUrl"] ?? "https://smkc.gov.in/razorpayresponse.aspx");

                if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(secret))
                {
                    return Content(HttpStatusCode.InternalServerError,
                        ApiResponse<object>.CreateError("Razorpay key configuration missing", "RAZORPAY_CONFIG_MISSING"));
                }

                var connectionNumber = !string.IsNullOrWhiteSpace(req.ConnectionNumber)
                    ? req.ConnectionNumber
                    : req.ConnectionNo.ToString();

                var amountInPaise = req.AmountInPaise > 0
                    ? req.AmountInPaise
                    : Convert.ToInt64(Math.Round(req.Amount * 100m));

                var referenceId = !string.IsNullOrWhiteSpace(req.ReferenceId)
                    ? req.ReferenceId
                    : ("WT-" + connectionNumber + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));

                var description = !string.IsNullOrWhiteSpace(req.Description)
                    ? req.Description
                    : ("Water Tax Payment for Connection #" + connectionNumber);

                // Razorpay requires expire_by to be at least 15 minutes in the future.
                // Use a small safety buffer to avoid edge-case failures during request timing.
                var expireBy = (long)(DateTime.UtcNow.AddMinutes(16)
                    .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .TotalSeconds);

                var payload = new JObject
                {
                    ["amount"] = amountInPaise,
                    ["currency"] = string.IsNullOrWhiteSpace(req.Currency) ? "INR" : req.Currency,
                    ["accept_partial"] = false,
                    ["first_min_partial_amount"] = 0,
                    ["expire_by"] = expireBy,
                    ["reference_id"] = referenceId,
                    ["description"] = description,
                    ["customer"] = new JObject
                    {
                        ["name"] = req.Name,
                        ["contact"] = req.Contact,
                        ["email"] = req.Email
                    },
                    ["notify"] = new JObject
                    {
                        ["sms"] = true,
                        ["email"] = true
                    },
                    ["reminder_enable"] = true,
                    ["notes"] = new JObject
                    {
                        ["policy_name"] = "Water tax payment",
                        ["connection_no"] = connectionNumber
                    },
                    ["callback_url"] = callbackUrl,
                    ["callback_method"] = "get"
                };

                using (var client = new HttpClient())
                {
                    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(keyId + ":" + secret));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var requestContent = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(baseUrl, requestContent);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return Content(response.StatusCode,
                            ApiResponse<object>.CreateError("Razorpay error: " + responseText, "RAZORPAY_PAYMENT_LINK_FAILED"));
                    }

                    var razorResponse = JObject.Parse(responseText);
                    return Ok(razorResponse);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[WaterController] Razorpay payment link creation failed. Connection: " + req.ConnectionNo + ", Error: " + ex);
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.CreateError("Failed to create payment link", "PAYMENT_LINK_ERROR"));
            }
        }
    }
}
