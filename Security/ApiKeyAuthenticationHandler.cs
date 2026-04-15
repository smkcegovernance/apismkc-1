using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Configuration;
using System.Collections.Generic;

namespace SmkcApi.Security
{
    public class ApiKeyAuthenticationHandler : DelegatingHandler
    {
        // Development test keys
        private static readonly HashSet<string> TestApiKeys = new HashSet<string>
        {
            "TEST_API_KEY_12345678901234567890123456789012",
            "DEV_API_KEY_ABCDE67890FGHIJ12345KLMNO67890",
            "ADMIN_API_KEY_XYZ12345678901234567890ABC456"
        };

        private static readonly Dictionary<string, string> TestApiKeySecrets = new Dictionary<string, string>
        {
            ["TEST_API_KEY_12345678901234567890123456789012"] = "TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890",
            ["DEV_API_KEY_ABCDE67890FGHIJ12345KLMNO67890"] = "DEV_SECRET_KEY_FGHIJ67890KLMNO12345PQRST",
            ["ADMIN_API_KEY_XYZ12345678901234567890ABC456"] = "ADMIN_SECRET_KEY_ABC45678901234567890DEF",
            ["BOOTH_API_KEY_12345678901234567890123456789012"] = "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890"
        };

        // Public endpoints that don't require authentication
        private static readonly string[] PublicEndpoints = new[]
        {
            "/api/auth/login",              // Unified login endpoint
            "/api/auth/bank/login",
            "/api/auth/account/login",
            "/api/auth/commissioner/login",
            "/api/booth/login",             // Booth Mapping login endpoint
            "/api/ftp-diagnostic/network-info",  // FTP diagnostic endpoints
            "/api/ftp-diagnostic/config",
            "/api/ftp-diagnostic/test",
            "/api/deposits/consent/health",      // Consent document endpoints (plain access)
            "/api/deposits/consent/downloadconsent",
            "/api/deposits/consent/documentconsent",  // POST upload endpoint
            "/api/deposits/consent/info",
            "/api/deposits/consent/googledrive/health",      // Google Drive consent endpoints (plain access)
            "/api/deposits/consent/googledrive/upload",
            "/api/deposits/consent/googledrive/download",
            "/api/deposits/consent/googledrive/info",
            "/api/women-child-welfare"
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Skip authentication for OPTIONS requests (CORS preflight)
                if (request.Method == HttpMethod.Options)
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                // Skip authentication for public endpoints (login routes)
                var requestPath = request.RequestUri.AbsolutePath.ToLowerInvariant();
                if (IsPublicEndpoint(requestPath))
                {
                    LogSecurityEvent($"Public endpoint accessed: {requestPath}");
                    return await base.SendAsync(request, cancellationToken);
                }

                // Extract authentication headers
                if (!request.Headers.Contains("X-API-Key"))
                {
                    return CreateUnauthorizedResponse("Missing API Key");
                }

                if (!request.Headers.Contains("X-Timestamp"))
                {
                    return CreateUnauthorizedResponse("Missing Timestamp");
                }

                if (!request.Headers.Contains("X-Signature"))
                {
                    return CreateUnauthorizedResponse("Missing Signature");
                }

                var apiKey = request.Headers.GetValues("X-API-Key").FirstOrDefault();
                var timestamp = request.Headers.GetValues("X-Timestamp").FirstOrDefault();
                var signature = request.Headers.GetValues("X-Signature").FirstOrDefault();

                // Validate API key format
                if (!SecurityHelper.IsValidApiKeyFormat(apiKey))
                {
                    return CreateUnauthorizedResponse("Invalid API Key format");
                }

                // Get secret key - check Web.config first, then test keys
                string secretKey = GetSecretKeyForApiKey(apiKey);
                if (secretKey == null)
                {
                    LogSecurityEvent($"Invalid API Key attempted: {SecurityHelper.MaskSensitiveData(apiKey)}");
                    return CreateUnauthorizedResponse("Invalid API Key");
                }

                // Validate timestamp
                if (!SecurityHelper.IsValidTimestamp(timestamp))
                {
                    LogSecurityEvent($"Invalid timestamp from API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");
                    return CreateUnauthorizedResponse("Invalid or expired timestamp");
                }

                // Get request body for signature validation
                string requestBody = "";
                if (request.Content != null)
                {
                    requestBody = await request.Content.ReadAsStringAsync();
                }

                // SERVER-SIDE SIGNATURE CALCULATION
                var httpMethod = request.Method.Method;
                var requestUri = request.RequestUri.PathAndQuery;
                
                // DETAILED DEBUG LOGGING
                var debugInfo = new System.Text.StringBuilder();
                debugInfo.AppendLine("=== SERVER SIGNATURE CALCULATION ===");
                debugInfo.AppendLine($"HTTP Method: {httpMethod}");
                debugInfo.AppendLine($"Request URI: {requestUri}");
                debugInfo.AppendLine($"Request Body: {(string.IsNullOrEmpty(requestBody) ? "(empty)" : requestBody.Substring(0, Math.Min(100, requestBody.Length)))}");
                debugInfo.AppendLine($"Timestamp: {timestamp}");
                debugInfo.AppendLine($"API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");
                
                // Create string to sign (EXACTLY as documented)
                var stringToSign = $"{httpMethod.ToUpper()}{requestUri}{requestBody}{timestamp}{apiKey}";
                debugInfo.AppendLine($"String to Sign: {stringToSign.Substring(0, Math.Min(200, stringToSign.Length))}...");
                
                // Calculate expected signature
                var expectedSignature = SecurityHelper.CreateRequestSignature(
                    httpMethod,
                    requestUri,
                    requestBody,
                    timestamp,
                    apiKey,
                    secretKey
                );
                
                debugInfo.AppendLine($"Expected Signature: {expectedSignature}");
                debugInfo.AppendLine($"Received Signature: {signature}");
                debugInfo.AppendLine($"Signatures Match: {signature == expectedSignature}");
                debugInfo.AppendLine("=====================================");
                
                // Log the debug info
                System.Diagnostics.Trace.TraceInformation(debugInfo.ToString());

                if (signature != expectedSignature)
                {
                    LogSecurityEvent($"Invalid signature from API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");
                    LogSecurityEvent($"SIGNATURE MISMATCH DETAILS:\n{debugInfo}");
                    return CreateUnauthorizedResponse("Invalid signature");
                }

                // Add authenticated user information to request properties
                request.Properties["ApiKey"] = apiKey;
                request.Properties["AuthenticatedAt"] = DateTime.UtcNow;

                LogSecurityEvent($"Successful authentication for API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Authentication error: {ex.Message}\n{ex.StackTrace}");
                return CreateUnauthorizedResponse("Authentication failed");
            }
        }

        /// <summary>
        /// Check if the request path is a public endpoint that doesn't require authentication
        /// </summary>
        private bool IsPublicEndpoint(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Check if path matches any public endpoint
            return PublicEndpoints.Any(endpoint =>
                path.Equals(endpoint, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(endpoint + "/", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get secret key for API key from Web.config or test keys
        /// Checks Web.config first (production), then test keys (development)
        /// </summary>
        private string GetSecretKeyForApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            // Check Web.config for production keys
            var configApiKey = ConfigurationManager.AppSettings["ApiKey"];
            var configSecret = ConfigurationManager.AppSettings["ApiSecret"];
            
            if (!string.IsNullOrWhiteSpace(configApiKey) && 
                !string.IsNullOrWhiteSpace(configSecret) &&
                configApiKey == apiKey &&
                !configApiKey.StartsWith("CHANGE_ME"))
            {
                return configSecret;
            }

            // Check test keys for development
            if (TestApiKeySecrets.ContainsKey(apiKey))
            {
                return TestApiKeySecrets[apiKey];
            }

            return null;
        }

        private HttpResponseMessage CreateUnauthorizedResponse(string message)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            {
                Content = new StringContent($"{{\"success\":false,\"message\":\"{message}\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\"}}")
            };
            
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            // Add security headers
            response.Headers.Add("X-Content-Type-Options", "nosniff");
            response.Headers.Add("X-Frame-Options", "DENY");
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
            
            return response;
        }

        private void LogSecurityEvent(string message)
        {
            // In production, use a proper logging framework like NLog, Serilog, etc.
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY: {message}";
            System.Diagnostics.Trace.TraceWarning(logEntry);
            
            // Optional: Log to Windows Event Log for security monitoring
            try
            {
                System.Diagnostics.EventLog.WriteEntry("SMKC API", logEntry, System.Diagnostics.EventLogEntryType.Warning);
            }
            catch
            {
                // Ignore event log failures to prevent breaking the API
            }
        }
    }
}