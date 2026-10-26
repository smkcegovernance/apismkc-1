using System;
using System.Globalization;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface IWaterService
    {
        Task<BillFetchResponse> FetchBillAsync(BillFetchRequest req);
        Task<CollectionPostResult> PostCollectionAsync(CollectionPostRequest req, string idempotencyKey);
        Task<WaterCollectionSuccessResponse> AuthorizeCollectionAsync(WaterCollectionSuccessRequest req);
    }

    public class WaterService : IWaterService
    {
        private readonly IWaterRepository _repo;
        public WaterService(IWaterRepository repo) { _repo = repo; }

        // UPDATED: Use stored procedure GET_CUSTOMER_API via repository
        public async Task<BillFetchResponse> FetchBillAsync(BillFetchRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConsumerNo))
                return null;

            // ✅ await the repo call
            var proc = await _repo.GetBillViaProcAsync(req.ConsumerNo);
            if (proc == null || "NOT_FOUND".Equals(proc.ApiStatus, StringComparison.OrdinalIgnoreCase))
                return null;

            // Map DB-shaped dto to API contract
            var arrears = proc.ArrearsBalance;
            if (arrears <= 0m)
            {
                var derivedArrears = proc.TotalBalance - proc.CurrentBillAmount - proc.CurrentInterestBalance;
                if (derivedArrears > 0m)
                    arrears = derivedArrears;
            }

            var currentBill = proc.CurrentBillAmount > 0m ? proc.CurrentBillAmount : proc.CurrentBalance;
            var rawLateFee = proc.CurrentInterestBalance > 0m ? proc.CurrentInterestBalance : proc.InterestBalance;
            var effectiveLateFee = rawLateFee;

            // Business rule: do not include late fee if due date has not been exceeded.
            DateTime dueDate;
            if (!string.IsNullOrWhiteSpace(proc.LastBillDueDate) &&
                DateTime.TryParseExact(
                    proc.LastBillDueDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out dueDate))
            {
                if (DateTime.Today <= dueDate.Date)
                {
                    effectiveLateFee = 0m;
                }
            }

            var totalDue = arrears + currentBill + effectiveLateFee;
            if (totalDue <= 0m)
            {
                totalDue = proc.TotalBalance;
            }

            var resp = new BillFetchResponse
            {
                // IDs & names
                ConsumerNo = proc.ConnectionNumber,
                Name = proc.CustomerName,
                Address = proc.CustomerAddress,

                // Status
                ConnectionStatus = proc.PaymentStatus,  // CURRENT/PAID/OVERDUE/...
                Mobile = null,                          // not provided by proc currently

                // Amounts
                Arrears = arrears,
                Current = currentBill,
                LateFee = effectiveLateFee,
                TotalDue = totalDue,

                // Dates
                AsOfDate = !string.IsNullOrWhiteSpace(proc.QueryDateTime)
                    ? proc.QueryDateTime.Substring(0, 10)   // yyyy-MM-dd from yyyy-MM-ddTHH:mm:ssZ
                    : DateTime.UtcNow.ToString("yyyy-MM-dd"),
                LastPaymentDate = proc.LastPaymentDate,
                LastPaymentAmount = proc.LastPaymentAmount
            };

            return resp;
        }

        public async Task<CollectionPostResult> PostCollectionAsync(CollectionPostRequest req, string idempotencyKey)
        {
            var existing = await _repo.GetReceiptByBankTxnAsync(req.BankTxnId);
            if (existing != null)
                return new CollectionPostResult { Status = CollectionPostStatus.Duplicate, Payload = existing };

            var customer = await _repo.GetCustomerAsync(req.ConsumerNo);
            if (customer == null)
                return new CollectionPostResult { Status = CollectionPostStatus.NotFound };

            if (customer.Status == "BLOCKED")
                return new CollectionPostResult { Status = CollectionPostStatus.BusinessRuleFailed, ErrorMessage = "Connection blocked" };

            var receipt = await _repo.PostCollectionAsync(req, idempotencyKey);
            return new CollectionPostResult { Status = CollectionPostStatus.Success, Payload = receipt };
        }

        public Task<WaterCollectionSuccessResponse> AuthorizeCollectionAsync(WaterCollectionSuccessRequest req)
        {
            return _repo.AuthorizeCollectionAsync(req);
        }
    }
}
