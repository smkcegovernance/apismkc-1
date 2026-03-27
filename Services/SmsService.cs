using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmkcApi.Infrastructure;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface ISmsService
    {
        Task<List<SmsSendResult>> SendBulkSmsAsync(SmsSendRequest request);
        Task<List<SmsSendResult>> SendWaterBillSmsAsync(WaterBillSmsSendRequest request);
    }

    public class SmsService : ISmsService
    {
        private readonly IWaterRepository _repo;
        private readonly ISmsSender _smsSender;
        private readonly int _batchSize;

        // your Marathi template:
        // "सा. मि. कु. मनापा. आपले पाणी जोडणी क्रमांक {#var#} थकबाकी सहित रक्कम रुपये {#var#}. चे बिल तयार झाले आहे. 31 ऑक्टोबर 2025 पर्यंत एकूण रूपये {#var#} संपूर्ण भरल्यास अभय योजनेअंतर्गत रुपये {#var#} ची सवलत मिळेल"
        private const string Template = "सा. मि. कु. मनापा. आपले पाणी जोडणी क्रमांक {CONN} थकबाकी सहित रक्कम रुपये {AMT}. चे बिल तयार झाले आहे. 30 मार्च 2025 पर्यंत एकूण रूपये {AFTER} संपूर्ण भरल्यास अभय योजनेअंतर्गत रुपये {DISC} ची सवलत मिळेल";

        // New template for water connection bill SMS
        private const string WaterBillTemplate = "सांगली मिरज व कुपवाड महानगरपालिका : प्रिय ग्राहक {NAME} , आपले पाणी कनेक्शन नं. {CONN} चे बिल ₹. {AMT} तयार झाले आहे. कृपया अंतिम दिनांक {DUE} पूर्वी भरणा करावा. धन्यवाद. ऑनलाईन पेमेंट लिंक : {URL}";
        public SmsService(IWaterRepository repo, ISmsSender smsSender)
        {
            _repo = repo;
            _smsSender = smsSender;
            _batchSize = int.TryParse(ConfigurationManager.AppSettings["Sms_MaxNumbersPerRequest"], out var n) ? n : 100;
        }

        public async Task<List<SmsSendResult>> SendBulkSmsAsync(SmsSendRequest request)
        {
            long connNo = 0;
            if (!string.IsNullOrWhiteSpace(request.ConnectionNumber))
                long.TryParse(request.ConnectionNumber, out connNo);

            string ward = string.IsNullOrWhiteSpace(request.WardCode) ? "0" : request.WardCode;
            string div = string.IsNullOrWhiteSpace(request.DivCode) ? "0" : request.DivCode;

            var rows = await _repo.GetConnectionBalanceWithMobileAsync(connNo, ward, div);

            var results = new List<SmsSendResult>();

            // Filter only eligible rows (status ELIGIBLE_* etc.) and that have mobile
            var sendRows = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.MobileNumber) &&
                            (r.Status?.StartsWith("ELIGIBLE") == true || r.Status == "ELIGIBLE_NO_DISCOUNT" || r.Status == "ELIGIBLE_WITH_DISCOUNT"))
                .ToList();

            // Build messages per number
            var numberToMessage = new List<SmsNumberMessage>();

            foreach (var r in sendRows)
            {
                // Format numbers: ensure country code 91; provider expects full number (e.g., 9198xxxxxxxx)
                var msisdn = NormalizeMobile(r.MobileNumber);
                if (string.IsNullOrWhiteSpace(msisdn)) continue;

                // Replace placeholders
                var msg = Template
                    .Replace("{CONN}", r.ConnectionNumber ?? "")
                    .Replace("{AMT}", r.TotalBalance.ToString("0"))
                    .Replace("{AFTER}", r.AfterDiscountBalance.ToString("0"))
                    .Replace("{DISC}", r.DiscountAmount.ToString("0"));

                numberToMessage.Add(new SmsNumberMessage
                {
                    Number = msisdn,
                    Message = msg,
                    Connection = r.ConnectionNumber
                });
            }

            // If preview only, return first few with Sent=false and ProviderResponse indicate preview
            if (request.PreviewOnly)
            {
                return numberToMessage
                    .Take(10)
                    .Select(x => new SmsSendResult
                    {
                        ConnectionNumber = x.Connection,
                        MobileNumber = x.Number,
                        Sent = false,
                        ProviderResponse = "PREVIEW",
                        Error = null
                    }).ToList();
            }

            // We will batch by number, but template is per number. Since provider supports multi-number but single message text,
            // we must send per-message per-number OR group numbers that share the same message. Here messages vary per user (balance),
            // so simplest is to send per-number. If performance matters, group identical messages (rare).
            // To respect provider: max 100 numbers per request; but messages vary -> do single recipient requests (or group identical messages).
            // We'll send per-number in small parallel batches.

            var tasks = new List<Task<SmsSendResult>>();

            foreach (var group in numberToMessage.GroupBy(x => x.Message))
            {
                // If group has many numbers and same message, we can batch them in up to _batchSize chunks
                var numbers = group.Select(g => g.Number).ToList();
                var connectionMap = group.ToDictionary(g => g.Number, g => g.Connection);

                for (int i = 0; i < numbers.Count; i += _batchSize)
                {
                    var batch = numbers.Skip(i).Take(_batchSize).ToList();
                    var batchMsg = group.Key;

                    // call provider
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var providerResponse = await _smsSender.SendSmsRawAsync(batch, batchMsg, unicode: true);
                            // providerResponse can be parsed to map individual messageIds
                            var firstNum = batch.First();
                            return new SmsSendResult
                            {
                                ConnectionNumber = connectionMap.ContainsKey(firstNum) ? connectionMap[firstNum] : null,
                                MobileNumber = string.Join(",", batch),
                                Sent = true,
                                ProviderResponse = providerResponse,
                                Error = null
                            };
                        }
                        catch (Exception ex)
                        {
                            return new SmsSendResult
                            {
                                ConnectionNumber = null,
                                MobileNumber = string.Join(",", batch),
                                Sent = false,
                                ProviderResponse = null,
                                Error = ex.Message
                            };
                        }
                    }));
                }
            }

            var completed = await Task.WhenAll(tasks);
            results.AddRange(completed);

            return results;
        }

        private string NormalizeMobile(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = new string(raw.Where(char.IsDigit).ToArray());
            if (s.Length == 10) return s;
            if (s.Length == 12 && s.StartsWith("91")) return s.Substring(2);
            if (s.Length == 13 && s.StartsWith("091")) return s.Substring(3);
            // else return as-is
            return s;
        }

        // New method to send water bill SMS with customer name and due date
        public async Task<List<SmsSendResult>> SendWaterBillSmsAsync(WaterBillSmsSendRequest request)
        {
            long connNo = 0;
            if (!string.IsNullOrWhiteSpace(request.ConnectionNumber))
                long.TryParse(request.ConnectionNumber, out connNo);

            string ward = string.IsNullOrWhiteSpace(request.WardCode) ? "0" : request.WardCode;
            string div = string.IsNullOrWhiteSpace(request.DivCode) ? "0" : request.DivCode;

            var rows = await _repo.GetWaterBillSmsDataAsync(connNo, ward, div);

            var results = new List<SmsSendResult>();

            // Filter only rows with mobile
            var sendRows = rows.Where(r => !string.IsNullOrWhiteSpace(r.MobileNumber)).ToList();

            // Build messages per number
            var numberToMessage = new List<SmsNumberMessage>();

            foreach (var r in sendRows)
            {
                var msisdn = NormalizeMobile(r.MobileNumber);
                if (string.IsNullOrWhiteSpace(msisdn)) continue;

                // Replace placeholders
                var msg = WaterBillTemplate
                    .Replace("{NAME}", r.CustomerName ?? "")
                    .Replace("{CONN}", r.ConnectionNumber ?? "")
                    .Replace("{AMT}", r.TotalAmount.ToString("0"))
                    .Replace("{DUE}", r.DueDate ?? "")
                    .Replace("{URL}", r.PaymentUrl ?? "");

                numberToMessage.Add(new SmsNumberMessage
                {
                    Number = msisdn,
                    Message = msg,
                    Connection = r.ConnectionNumber
                });
            }

            // If preview only, return first few with Sent=false
            if (request.PreviewOnly)
            {
                return numberToMessage
                    .Take(10)
                    .Select(x => new SmsSendResult
                    {
                        ConnectionNumber = x.Connection,
                        MobileNumber = x.Number,
                        Sent = false,
                        ProviderResponse = $"PREVIEW: {x.Message}",
                        Error = null
                    }).ToList();
            }

            // Send SMS in batches
            var tasks = new List<Task<SmsSendResult>>();

            foreach (var group in numberToMessage.GroupBy(x => x.Message))
            {
                var numbers = group.Select(g => g.Number).ToList();
                var connectionMap = group.ToDictionary(g => g.Number, g => g.Connection);

                for (int i = 0; i < numbers.Count; i += _batchSize)
                {
                    var batch = numbers.Skip(i).Take(_batchSize).ToList();
                    var batchMsg = group.Key;
                    var firstConn = connectionMap[batch.First()];

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var providerResponse = await _smsSender.SendSmsRawAsync(
                                batch, batchMsg, unicode: true, connectionNumber: firstConn);
                            
                            var firstNum = batch.First();
                            return new SmsSendResult
                            {
                                ConnectionNumber = connectionMap.ContainsKey(firstNum) ? connectionMap[firstNum] : null,
                                MobileNumber = string.Join(",", batch),
                                Sent = true,
                                ProviderResponse = providerResponse,
                                Error = null
                            };
                        }
                        catch (Exception ex)
                        {
                            return new SmsSendResult
                            {
                                ConnectionNumber = connectionMap.ContainsKey(batch.First()) ? connectionMap[batch.First()] : null,
                                MobileNumber = string.Join(",", batch),
                                Sent = false,
                                ProviderResponse = null,
                                Error = ex.Message
                            };
                        }
                    }));
                }
            }

            var completed = await Task.WhenAll(tasks);
            results.AddRange(completed);

            return results;
        }
    }
}
