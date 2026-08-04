using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SmkcApi.Repositories; // <--- to access WaterRepository
using Oracle.ManagedDataAccess.Client;

namespace SmkcApi.Infrastructure
{
    /// <summary>
    /// SMS sending abstraction used by public and department flows.
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>
        /// Send an SMS message to one or more numbers and log it using WaterRepository.
        /// Returns provider raw response (JSON string). It will not throw on non-200; caller should inspect response.
        /// </summary>
        Task<string> SendSmsRawAsync(IEnumerable<string> numbers,
                                     string message,
                                     bool unicode = true,
                                     string dlttemplateid = null,
                                     string peid = null,
                                     string telemarketerid = null,
                                     string connectionNumber = null);

        /// <summary>
        /// Send OTP SMS through the mt/SendSMS gateway that uses user/password auth.
        /// </summary>
        Task<string> SendOtpSmsRawAsync(IEnumerable<string> numbers,
                                        string message,
                                        bool unicode = false,
                                        string channel = null,
                                        string dcs = null,
                                        string route = null,
                                        string dltTemplateId = null,
                                        string peid = null,
                                        string connectionNumber = null);

        /// <summary>
        /// Check delivery status for an OTP job id.
        /// </summary>
        Task<string> GetOtpDeliveryStatusRawAsync(string jobId,
                             string connectionNumber = null);
    }

    /// <summary>
    /// Infinitio SMS gateway client used for bulk SMS, OTP SMS, and delivery checks.
    /// </summary>
    public class SmsSender : ISmsSender, IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly string _otpApiUrl;
        private readonly string _otpDeliveryApiUrl;
        private readonly string _user;
        private readonly string _password;
        private readonly string _senderId;
        private readonly string _channel;
        private readonly string _dcsUnicode;

        private readonly IWaterRepository _repo; // ✅ repository for Oracle logging

        /// <summary>
        /// Creates a new SMS sender backed by Oracle SMS logging.
        /// </summary>
        /// <param name="repo">Water repository used for SMS audit logging.</param>
        public SmsSender(IWaterRepository repo)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            _apiUrl = ConfigurationManager.AppSettings["Sms_ApiUrl"] ?? "https://sms.infinitiodigital.com/api/bulkmt/SendSMS";
            _apiKey = ConfigurationManager.AppSettings["Sms_ApiKey"] ?? "";
            _otpApiUrl = ConfigurationManager.AppSettings["Sms_OtpApiUrl"] ?? "https://sms.infinitiodigital.com/api/mt/SendSMS";
            _otpDeliveryApiUrl = ConfigurationManager.AppSettings["Sms_OtpDeliveryApiUrl"] ?? "https://sms.infinitiodigital.com/api/mt/GetDelivery";
            _user = ConfigurationManager.AppSettings["Sms_User"] ?? "";
            _password = ConfigurationManager.AppSettings["Sms_Password"] ?? "";
            _senderId = ConfigurationManager.AppSettings["Sms_SenderId"] ?? "";
            _channel = ConfigurationManager.AppSettings["Sms_Channel"] ?? "Trans";
            _dcsUnicode = ConfigurationManager.AppSettings["Sms_DCS_Unicode"] ?? "0";
        }

        /// <summary>
        /// Sends a bulk SMS using the bulkmt gateway.
        /// </summary>
        public async Task<string> SendSmsRawAsync(IEnumerable<string> numbers,
                                                  string message,
                                                  bool unicode = true,
                                                  string dlttemplateid = null,
                                                  string peid = null,
                                                  string telemarketerid = null,
                                                  string connectionNumber = null)
        {
            if (numbers == null) throw new ArgumentNullException(nameof(numbers));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("Sms_ApiKey is missing in configuration.");

            // Prepare list of valid numbers
            var numberList = numbers
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => new string((n ?? "").Where(char.IsDigit).ToArray()))
                .Select(n => n.Length == 12 && n.StartsWith("91") ? n.Substring(2) : n)
                .Where(n => n.Length == 10)
                .ToList();

            if (!numberList.Any())
                throw new ArgumentException("No valid numbers provided", nameof(numbers));

            var numberCsv = string.Join(",", numberList);
            string UrlEnc(string s) => HttpUtility.UrlEncode(s ?? string.Empty);

            var dcs = unicode ? _dcsUnicode : "0";

            // Build query URL as per Infinitio gateway pattern.
            var sb = new StringBuilder();
            sb.Append("APIkey=").Append(UrlEnc(_apiKey));
            sb.Append("&senderid=").Append(UrlEnc(_senderId));
            sb.Append("&channel=").Append(UrlEnc(_channel));
            sb.Append("&DCS=").Append(UrlEnc(dcs));
            sb.Append("&flashsms=0");
            sb.Append("&number=").Append(UrlEnc(numberCsv));
            sb.Append("&text=").Append(UrlEnc(message));
            if (!string.IsNullOrWhiteSpace(dlttemplateid)) sb.Append("&dlttemplateid=").Append(UrlEnc(dlttemplateid));
            if (!string.IsNullOrWhiteSpace(telemarketerid)) sb.Append("&telemarketerid=").Append(UrlEnc(telemarketerid));
            if (!string.IsNullOrWhiteSpace(peid)) sb.Append("&peid=").Append(UrlEnc(peid));

            var requestUrl = _apiUrl + "?" + sb.ToString();

            string providerResponse = string.Empty;
            string responseCode = string.Empty;
            string responseMessage = string.Empty;

            try
            {
                System.Diagnostics.Trace.TraceInformation("SmsSender: Sending SMS to {0} numbers.", numberList.Count);

                var httpResponse = await _http.GetAsync(requestUrl).ConfigureAwait(false);
                providerResponse = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                System.Diagnostics.Trace.TraceInformation("SmsSender: Provider returned status {0}", httpResponse.StatusCode);

                // Try extracting provider info
                responseCode = ExtractJsonValue(providerResponse, "ErrorCode");
                responseMessage = ExtractJsonValue(providerResponse, "ErrorMessage");

                // ✅ Call repository logging (moved here)
                await _repo.LogSmsToOracleAsync(
                    connectionNumber,
                    numberCsv,
                    message,
                    responseCode,
                    responseMessage,
                    providerResponse);
            }
            catch (Exception ex)
            {
                providerResponse = $"{{\"Error\":\"{HttpUtility.JavaScriptStringEncode(ex.Message)}\"}}";
                responseCode = "EX";
                responseMessage = ex.Message;

                // ✅ Still log failed attempts
                await _repo.LogSmsToOracleAsync(
                    connectionNumber,
                    numberCsv,
                    message,
                    responseCode,
                    responseMessage,
                    providerResponse);

                System.Diagnostics.Trace.TraceError("SmsSender: Exception when sending SMS: " + ex);
            }

            return providerResponse;
        }

        /// <summary>
        /// Sends an OTP SMS using the mt gateway and the configured OTP template.
        /// </summary>
        public async Task<string> SendOtpSmsRawAsync(IEnumerable<string> numbers,
                                                     string message,
                                                     bool unicode = false,
                                                     string channel = null,
                                                     string dcs = null,
                                                     string route = null,
                                                     string dltTemplateId = null,
                                                     string peid = null,
                                                     string connectionNumber = null)
        {
            if (numbers == null) throw new ArgumentNullException(nameof(numbers));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(_user)) throw new InvalidOperationException("Sms_User is missing in configuration.");
            if (string.IsNullOrWhiteSpace(_password)) throw new InvalidOperationException("Sms_Password is missing in configuration.");

            var numberList = numbers
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => new string((n ?? "").Where(char.IsDigit).ToArray()))
                .Select(n => n.Length == 12 && n.StartsWith("91") ? n.Substring(2) : n)
                .Where(n => n.Length == 10)
                .ToList();

            if (!numberList.Any())
                throw new ArgumentException("No valid numbers provided", nameof(numbers));

            var numberCsv = string.Join(",", numberList);
            string UrlEnc(string s) => HttpUtility.UrlEncode(s ?? string.Empty);

            var resolvedDcs = !string.IsNullOrWhiteSpace(dcs) ? dcs : (unicode ? _dcsUnicode : "0");
            var resolvedChannel = string.IsNullOrWhiteSpace(channel) ? _channel : channel;
            var routeValue = string.IsNullOrWhiteSpace(route) ? (ConfigurationManager.AppSettings["Sms_DefaultRoute"] ?? string.Empty) : route;

            var sb = new StringBuilder();
            sb.Append("user=").Append(UrlEnc(_user));
            sb.Append("&password=").Append(UrlEnc(_password));
            sb.Append("&senderid=").Append(UrlEnc(_senderId));
            sb.Append("&channel=").Append(UrlEnc(resolvedChannel));
            sb.Append("&DCS=").Append(UrlEnc(resolvedDcs));
            sb.Append("&flashsms=0");
            sb.Append("&number=").Append(UrlEnc(numberCsv));
            sb.Append("&text=").Append(UrlEnc(message));
            if (!string.IsNullOrWhiteSpace(routeValue)) sb.Append("&route=").Append(UrlEnc(routeValue));
            if (!string.IsNullOrWhiteSpace(dltTemplateId)) sb.Append("&DLTTemplateId=").Append(UrlEnc(dltTemplateId));
            if (!string.IsNullOrWhiteSpace(peid)) sb.Append("&PEID=").Append(UrlEnc(peid));

            var requestUrl = _otpApiUrl + "?" + sb.ToString();

            string providerResponse = string.Empty;
            string responseCode = string.Empty;
            string responseMessage = string.Empty;

            try
            {
                System.Diagnostics.Trace.TraceInformation("SmsSender: Sending OTP SMS to {0} numbers.", numberList.Count);

                providerResponse = await SendOtpGatewayRequestAsync(requestUrl).ConfigureAwait(false);

                responseCode = ExtractJsonValue(providerResponse, "ErrorCode");
                responseMessage = ExtractJsonValue(providerResponse, "ErrorMessage");

                await _repo.LogSmsToOracleAsync(
                    connectionNumber,
                    numberCsv,
                    message,
                    responseCode,
                    responseMessage,
                    providerResponse);
            }
            catch (Exception ex)
            {
                providerResponse = $"{{\"Error\":\"{HttpUtility.JavaScriptStringEncode(ex.Message)}\"}}";
                responseCode = "EX";
                responseMessage = ex.Message;

                await _repo.LogSmsToOracleAsync(
                    connectionNumber,
                    numberCsv,
                    message,
                    responseCode,
                    responseMessage,
                    providerResponse);

                System.Diagnostics.Trace.TraceError("SmsSender: Exception when sending OTP SMS: " + ex);
            }

            return providerResponse;
        }

        private async Task<string> SendOtpGatewayRequestAsync(string requestUrl)
        {
            try
            {
                var httpResponse = await _http.GetAsync(requestUrl).ConfigureAwait(false);
                System.Diagnostics.Trace.TraceInformation("SmsSender: OTP provider returned status {0}", httpResponse.StatusCode);
                return await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("SmsSender: HttpClient OTP transport failed, retrying via HttpWebRequest. " + ex.Message);
                return await SendOtpGatewayRequestWithWebRequestAsync(requestUrl).ConfigureAwait(false);
            }
        }

        private static async Task<string> SendOtpGatewayRequestWithWebRequestAsync(string requestUrl)
        {
            var request = (HttpWebRequest)WebRequest.Create(requestUrl);
            request.Method = "GET";
            request.Proxy = null;
            request.KeepAlive = false;
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks delivery status for an OTP job id using the mt delivery endpoint.
        /// </summary>
        public async Task<string> GetOtpDeliveryStatusRawAsync(string jobId,
                                                              string connectionNumber = null)
        {
            if (string.IsNullOrWhiteSpace(jobId)) throw new ArgumentNullException(nameof(jobId));
            if (string.IsNullOrWhiteSpace(_user)) throw new InvalidOperationException("Sms_User is missing in configuration.");
            if (string.IsNullOrWhiteSpace(_password)) throw new InvalidOperationException("Sms_Password is missing in configuration.");

            string UrlEnc(string s) => HttpUtility.UrlEncode(s ?? string.Empty);
            var requestUrl = _otpDeliveryApiUrl + "?user=" + UrlEnc(_user)
                + "&password=" + UrlEnc(_password)
                + "&jobid=" + UrlEnc(jobId);

            string providerResponse = string.Empty;

            try
            {
                System.Diagnostics.Trace.TraceInformation("SmsSender: Checking OTP delivery for JobId {0}", jobId);
                var httpResponse = await _http.GetAsync(requestUrl).ConfigureAwait(false);
                providerResponse = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                await _repo.LogSmsToOracleAsync(
                    connectionNumber,
                    string.Empty,
                    "OTP DELIVERY STATUS CHECK",
                    httpResponse.StatusCode.ToString(),
                    string.Empty,
                    providerResponse);
            }
            catch (Exception ex)
            {
                providerResponse = $"{{\"Error\":\"{HttpUtility.JavaScriptStringEncode(ex.Message)}\"}}";
                await _repo.LogSmsToOracleAsync(
                    connectionNumber,
                    string.Empty,
                    "OTP DELIVERY STATUS CHECK",
                    "EX",
                    ex.Message,
                    providerResponse);
                System.Diagnostics.Trace.TraceError("SmsSender: Exception when checking OTP delivery: " + ex);
            }

            return providerResponse;
        }

        private static string ExtractJsonValue(string json, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                    return string.Empty;

                int keyIndex = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0) return string.Empty;

                int colonIndex = json.IndexOf(":", keyIndex);
                if (colonIndex < 0) return string.Empty;

                int start = json.IndexOf("\"", colonIndex + 1) + 1;
                int end = json.IndexOf("\"", start + 1);
                if (start < 0 || end < 0 || end <= start) return string.Empty;

                return json.Substring(start, end - start).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Disposes the internal HTTP client.
        /// </summary>
        public void Dispose() => _http?.Dispose();
    }
}
