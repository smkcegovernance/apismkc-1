using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net;
using SmkcApi.Models;
using SmkcApi.Security;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    // NOTE: This controller was conflicting by name with
    // Controllers/DepositManager/AccountController.cs and causing
    // routing issues for /api/deposits/account/... endpoints.
    //
    // It has been renamed to avoid duplicate Web API controller
    // type names. All routes keep the same URL surface.

    [RoutePrefix("api/accounts")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class CoreAccountController : ApiController
    {
        private readonly IAccountService _accountService;

        public CoreAccountController(IAccountService accountService)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        /// <summary>
        /// Get account details by account number
        /// </summary>
        /// <param name="accountNumber">The account number</param>
        /// <returns>Account details</returns>
        [HttpGet]
        [Route("{accountNumber}")]
        public async Task<IHttpActionResult> GetAccount(string accountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(accountNumber))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Account number is required", "INVALID_ACCOUNT_NUMBER"));
                }

                var account = await _accountService.GetAccountAsync(accountNumber);
                
                if (account == null)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<Account>.CreateSuccess(account, "Account retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("GetAccount", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get account balance by account number
        /// </summary>
        /// <param name="accountNumber">The account number</param>
        /// <returns>Account balance information</returns>
        [HttpGet]
        [Route("{accountNumber}/balance")]
        public async Task<IHttpActionResult> GetAccountBalance(string accountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(accountNumber))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Account number is required", "INVALID_ACCOUNT_NUMBER"));
                }

                var balance = await _accountService.GetAccountBalanceAsync(accountNumber);
                
                if (balance == null)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<AccountBalanceResponse>.CreateSuccess(balance, "Account balance retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("GetAccountBalance", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get all accounts for a customer
        /// </summary>
        /// <param name="customerReference">The customer reference</param>
        /// <returns>List of customer accounts</returns>
        [HttpGet]
        [Route("customer/{customerReference}")]
        public async Task<IHttpActionResult> GetCustomerAccounts(string customerReference)
        {
            try
            {
                if (string.IsNullOrEmpty(customerReference))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Customer reference is required", "INVALID_CUSTOMER_REFERENCE"));
                }

                var accounts = await _accountService.GetAccountsByCustomerAsync(customerReference);
                
                return Ok(ApiResponse<System.Collections.Generic.List<Account>>.CreateSuccess(accounts, "Customer accounts retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("GetCustomerAccounts", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Create a new account
        /// </summary>
        /// <param name="request">Account creation request</param>
        /// <returns>Created account details</returns>
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateAccount([FromBody] AccountRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var account = await _accountService.CreateAccountAsync(request);
                
                return Ok(ApiResponse<Account>.CreateSuccess(account, "Account created successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("CreateAccount", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Update account status
        /// </summary>
        /// <param name="accountNumber">The account number</param>
        /// <param name="request">Status update request</param>
        /// <returns>Update result</returns>
        [HttpPut]
        [Route("{accountNumber}/status")]
        public async Task<IHttpActionResult> UpdateAccountStatus(string accountNumber, [FromBody] AccountStatusUpdateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(accountNumber))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Account number is required", "INVALID_ACCOUNT_NUMBER"));
                }

                if (request == null || string.IsNullOrEmpty(request.Status))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Status is required", "MISSING_STATUS"));
                }

                var result = await _accountService.UpdateAccountStatusAsync(accountNumber, request.Status);
                
                if (!result)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<object>.CreateSuccess(new { accountNumber, status = request.Status }, "Account status updated successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("UpdateAccountStatus", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Validate account exists and is active
        /// </summary>
        /// <param name="accountNumber">The account number</param>
        /// <returns>Validation result</returns>
        [HttpGet]
        [Route("{accountNumber}/validate")]
        public async Task<IHttpActionResult> ValidateAccount(string accountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(accountNumber))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Account number is required", "INVALID_ACCOUNT_NUMBER"));
                }

                var isValid = await _accountService.ValidateAccountAsync(accountNumber);
                
                var result = new { accountNumber, isValid, status = isValid ? "Valid" : "Invalid" };
                return Ok(ApiResponse<object>.CreateSuccess(result, "Account validation completed"));
            }
            catch (Exception ex)
            {
                LogError("ValidateAccount", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - CORE_ACCOUNT_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
            System.Diagnostics.Trace.TraceError(logEntry);
            
            // Additional security logging with request context
            if (Request.Properties.ContainsKey("ApiKey"))
            {
                var apiKey = Request.Properties["ApiKey"].ToString();
                var maskedApiKey = SecurityHelper.MaskSensitiveData(apiKey);
                var requestId = Request.Properties.ContainsKey("RequestId") ? Request.Properties["RequestId"].ToString() : "Unknown";
                
                var securityLogEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Controller: CoreAccountController, " +
                                      $"Action: {action}, ApiKey: {maskedApiKey}, RequestId: {requestId}, Error: {ex.Message}";
                System.Diagnostics.Trace.TraceError(securityLogEntry);
            }
        }
    }
}
