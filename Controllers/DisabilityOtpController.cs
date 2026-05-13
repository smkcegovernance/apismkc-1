using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using SmkcApi.Infrastructure;
using SmkcApi.Security;

namespace SmkcApi.Controllers
{
    /// <summary>
    /// Public OTP endpoints for SMKC Disability Registration form.
    /// No SHA authentication required — callers are public citizens.
    /// Rate-limited per IP to prevent abuse.
    /// </summary>
    [RoutePrefix("api/disability")]
    [RateLimit(maxRequests: 10, timeWindowMinutes: 1)]
    public class DisabilityOtpController : ApiController
    {
        private const int OtpTtlMinutes = 5;
        private const int MaxAttempts = 5;

        // Static in-process store: key = 10-digit mobile, value = OtpEntry.
        // Resets on app-pool recycle — acceptable for a short-lived OTP.
        private static readonly ConcurrentDictionary<string, OtpEntry> OtpStore
            = new ConcurrentDictionary<string, OtpEntry>();

        private readonly ISmsSender _smsSender;

        public DisabilityOtpController(ISmsSender smsSender)
        {
            _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
        }

        /// <summary>
        /// Generate a 6-digit OTP and send it to the supplied mobile number via SMS.
        /// POST /api/disability/send-otp
        /// Body: { "mobile": "9876543210" }
        /// </summary>
        [HttpPost]
        [Route("send-otp")]
        public async Task<IHttpActionResult> SendOtp([FromBody] DisabilityOtpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mobile))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "अवैध विनंती" });

            var mobile = NormalizeMobile(request.Mobile);
            if (mobile == null)
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "कृपया वैध 10 अंकी मोबाईल क्रमांक प्रविष्ट करा" });

            var otp = GenerateOtp();
            OtpStore[mobile] = new OtpEntry
            {
                Otp = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
                Attempts = 0
            };

            var message = string.Format(
                "Dear User, Your One Time Password for SMKC Municipal Corporation Disability Registration is {0}. Please Do Not Share this with Anybody.",
                otp);

            try
            {
                await _smsSender.SendSmsRawAsync(
                    new[] { mobile },
                    message,
                    unicode: false,
                    dlttemplateid: ConfigurationManager.AppSettings["Sms_DisabilityOtpTemplateId"]);
                return Ok(new { success = true, message = "OTP यशस्वीरित्या पाठवला गेला" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "DisabilityOtpController.SendOtp error: " + ex);
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = "OTP पाठवताना त्रुटी आली" });
            }
        }

        /// <summary>
        /// Verify an OTP for the given mobile number.
        /// POST /api/disability/verify-otp
        /// Body: { "mobile": "9876543210", "otp": "123456" }
        /// </summary>
        [HttpPost]
        [Route("verify-otp")]
        public IHttpActionResult VerifyOtp([FromBody] DisabilityVerifyOtpRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Mobile)
                || string.IsNullOrWhiteSpace(request.Otp))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "अवैध विनंती" });

            var mobile = NormalizeMobile(request.Mobile);
            if (mobile == null)
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "अवैध मोबाईल क्रमांक" });

            OtpEntry entry;
            if (!OtpStore.TryGetValue(mobile, out entry))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "OTP आढळला नाही. कृपया पुन्हा OTP मागवा." });

            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                OtpEntry removed;
                OtpStore.TryRemove(mobile, out removed);
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "OTP कालबाह्य झाला. कृपया नवीन OTP मागवा." });
            }

            entry.Attempts++;

            if (entry.Attempts > MaxAttempts)
            {
                OtpEntry removed;
                OtpStore.TryRemove(mobile, out removed);
                return Content((HttpStatusCode)429,
                    new { success = false, message = "जास्त अयशस्वी प्रयत्न. कृपया नवीन OTP मागवा." });
            }

            if (entry.Otp != request.Otp.Trim())
                return Content(HttpStatusCode.BadRequest,
                    new
                    {
                        success = false,
                        message = string.Format("चुकीचा OTP. {0} प्रयत्न शिल्लक.", MaxAttempts - entry.Attempts)
                    });

            OtpEntry verified;
            OtpStore.TryRemove(mobile, out verified);
            return Ok(new { success = true, message = "मोबाईल क्रमांक यशस्वीरित्या पडताळला" });
        }

        // ── helpers ─────────────────────────────────────────────────────────────

        private static string NormalizeMobile(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var digits = new System.Text.StringBuilder();
            foreach (var ch in raw)
                if (char.IsDigit(ch)) digits.Append(ch);
            var s = digits.ToString();
            // strip leading country code 91 if present
            if (s.Length == 12 && s.StartsWith("91")) s = s.Substring(2);
            return s.Length == 10 ? s : null;
        }

        private static string GenerateOtp()
        {
            // Cryptographically random 6-digit OTP
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var value = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000;
                return value.ToString();
            }
        }

        // ── nested types ─────────────────────────────────────────────────────────

        private sealed class OtpEntry
        {
            public string Otp { get; set; }
            public DateTime ExpiresAt { get; set; }
            public int Attempts { get; set; }
        }
    }

    public class DisabilityOtpRequest
    {
        public string Mobile { get; set; }
    }

    public class DisabilityVerifyOtpRequest
    {
        public string Mobile { get; set; }
        public string Otp { get; set; }
    }
}
