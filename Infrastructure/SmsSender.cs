using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SmkcApi.Repositories; // <--- to access WaterRepository
using Oracle.ManagedDataAccess.Client;

namespace SmkcApi.Infrastructure
{
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
    }

    public class SmsSender : ISmsSender, IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _user;
        private readonly string _password;
        private readonly string _senderId;
        private readonly string _channel;
        private readonly string _route;
        private readonly string _defaultPeid;
        private readonly string _dcsUnicode;
        private readonly int _maxNumbersPerRequest;

        private readonly IWaterRepository _repo; // ✅ repository for Oracle logging

        public SmsSender(IWaterRepository repo)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            _user = ConfigurationManager.AppSettings["Sms_User"] ?? "";
            _password = ConfigurationManager.AppSettings["Sms_Password"] ?? "";
            _senderId = ConfigurationManager.AppSettings["Sms_SenderId"] ?? "";
            _channel = ConfigurationManager.AppSettings["Sms_Channel"] ?? "Trans";
            _route = ConfigurationManager.AppSettings["Sms_DefaultRoute"] ?? "";
            _defaultPeid = ConfigurationManager.AppSettings["Sms_DefaultPeId"] ?? "";
            _dcsUnicode = ConfigurationManager.AppSettings["Sms_DCS_Unicode"] ?? "8";
            _maxNumbersPerRequest = int.TryParse(ConfigurationManager.AppSettings["Sms_MaxNumbersPerRequest"], out var n) ? n : 100;
        }

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

            // SMS Provider endpoint
            const string baseUrl = "http://sms.auurumdigital.com/api/mt/SendSMS";

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
            var finalPeid = string.IsNullOrWhiteSpace(peid) ? _defaultPeid : peid;

            // Build query URL
            var sb = new StringBuilder();
            sb.Append("user=").Append(UrlEnc(_user));
            sb.Append("&password=").Append(UrlEnc(_password));
            sb.Append("&senderid=").Append(UrlEnc(_senderId));
            sb.Append("&channel=").Append(UrlEnc(_channel));
            sb.Append("&DCS=").Append(UrlEnc(dcs));
            sb.Append("&flashsms=0");
            sb.Append("&number=").Append(UrlEnc(numberCsv));
            sb.Append("&text=").Append(UrlEnc(message));
            if (!string.IsNullOrWhiteSpace(_route)) sb.Append("&route=").Append(UrlEnc(_route));
            if (!string.IsNullOrWhiteSpace(finalPeid)) sb.Append("&peid=").Append(UrlEnc(finalPeid));
            if (!string.IsNullOrWhiteSpace(dlttemplateid)) sb.Append("&dlttemplateid=").Append(UrlEnc(dlttemplateid));
            if (!string.IsNullOrWhiteSpace(telemarketerid)) sb.Append("&telemarketerid=").Append(UrlEnc(telemarketerid));

            var requestUrl = baseUrl + "?" + sb.ToString();

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

        public void Dispose() => _http?.Dispose();
    }
}
