using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net; // added
using SmkcApi.Models;
using SmkcApi.Security;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    [RoutePrefix("api/transactions")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 200, timeWindowMinutes: 1)]
    public class TransactionController : ApiController
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        }

        /// <summary>
        /// Get transaction details by transaction ID
        /// </summary>
        /// <param name="transactionId">The transaction ID</param>
        /// <returns>Transaction details</returns>
        [HttpGet]
        [Route("{transactionId}")]
        public async Task<IHttpActionResult> GetTransaction(string transactionId)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionId))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Transaction ID is required", "INVALID_TRANSACTION_ID"));
                }

                var transaction = await _transactionService.GetTransactionAsync(transactionId);
                
                if (transaction == null)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<Transaction>.CreateSuccess(transaction, "Transaction retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("GetTransaction", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Process a new transaction
        /// </summary>
        /// <param name="request">Transaction request</param>
        /// <returns>Transaction response</returns>
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> ProcessTransaction([FromBody] TransactionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var response = await _transactionService.ProcessTransactionAsync(request);
                
                return Ok(ApiResponse<TransactionResponse>.CreateSuccess(response, "Transaction processed successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("ProcessTransaction", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get transaction history for an account
        /// </summary>
        /// <param name="request">Transaction history request</param>
        /// <returns>Paginated transaction history</returns>
        [HttpPost]
        [Route("history")]
        public async Task<IHttpActionResult> GetTransactionHistory([FromBody] TransactionHistoryRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var history = await _transactionService.GetTransactionHistoryAsync(request);
                
                return Ok(ApiResponse<PagedResult<Transaction>>.CreateSuccess(history, "Transaction history retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("GetTransactionHistory", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Reverse a transaction
        /// </summary>
        /// <param name="transactionId">The transaction ID to reverse</param>
        /// <param name="request">Reversal request with reason</param>
        /// <returns>Reversal response</returns>
        [HttpPost]
        [Route("{transactionId}/reverse")]
        public async Task<IHttpActionResult> ReverseTransaction(string transactionId, [FromBody] TransactionReversalRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionId))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Transaction ID is required", "INVALID_TRANSACTION_ID"));
                }

                if (request == null || string.IsNullOrEmpty(request.Reason))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Reversal reason is required", "MISSING_REVERSAL_REASON"));
                }

                var response = await _transactionService.ReverseTransactionAsync(transactionId, request.Reason);
                
                return Ok(ApiResponse<TransactionResponse>.CreateSuccess(response, "Transaction reversal processed"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("ReverseTransaction", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Validate a transaction request
        /// </summary>
        /// <param name="request">Transaction request to validate</param>
        /// <returns>Validation result</returns>
        [HttpPost]
        [Route("validate")]
        public async Task<IHttpActionResult> ValidateTransaction([FromBody] TransactionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var isValid = await _transactionService.ValidateTransactionAsync(request);
                
                var result = new { 
                    isValid, 
                    status = isValid ? "Valid" : "Invalid",
                    fromAccount = request.FromAccount,
                    toAccount = request.ToAccount,
                    amount = request.Amount
                };
                
                return Ok(ApiResponse<object>.CreateSuccess(result, "Transaction validation completed"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("ValidateTransaction", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - TRANSACTION_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
            System.Diagnostics.Trace.TraceError(logEntry);
            
            // Additional security logging with request context
            if (Request.Properties.ContainsKey("ApiKey"))
            {
                var apiKey = Request.Properties["ApiKey"].ToString();
                var maskedApiKey = SecurityHelper.MaskSensitiveData(apiKey);
                var requestId = Request.Properties.ContainsKey("RequestId") ? Request.Properties["RequestId"].ToString() : "Unknown";
                
                var securityLogEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Action: {action}, " +
                                      $"ApiKey: {maskedApiKey}, RequestId: {requestId}, Error: {ex.Message}";
                System.Diagnostics.Trace.TraceError(securityLogEntry);
            }
        }
    }
}
