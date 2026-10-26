using System;
using Newtonsoft.Json;

namespace SmkcApi.Models
{
    /// <summary>
    /// Bank → Our API: collection posting payload
    /// </summary>
    public class CollectionPostRequest
    {
        public string ConsumerNo { get; set; }          // Water connection no
        public decimal Amount { get; set; }          // Paid amount
        public string PaymentMode { get; set; }          // UPI|NEFT|RTGS|IMPS|CARD|CHEQUE|CASH
        public string BankTxnId { get; set; }          // Unique bank txn id (idempotency)
        public string UtrNo { get; set; }          // Optional UTR for NEFT/RTGS
        public string PaidDate { get; set; }          // ISO 8601 string, e.g. "2025-10-10T10:43:00Z"
        public string Narration { get; set; }          // Optional note
    }

    /// <summary>
    /// Our API → Bank: response after posting a collection
    /// </summary>
    public class CollectionPostResponse
    {
        public string ReceiptNo { get; set; }         // Generated receipt no
        public string ConsumerNo { get; set; }
        public decimal PostedAmount { get; set; }
        public string PostedAt { get; set; }         // "yyyy-MM-ddTHH:mm:ssZ"
        public string BankTxnId { get; set; }
        public decimal BalanceAfter { get; set; }         // Remaining balance after posting
    }

    public enum CollectionPostStatus
    {
        Success,
        Duplicate,
        NotFound,
        BusinessRuleFailed,
        Error
    }

    /// <summary>
    /// Internal service result used by controller to choose HTTP code.
    /// </summary>
    public class CollectionPostResult
    {
        public CollectionPostStatus Status { get; set; }
        public CollectionPostResponse Payload { get; set; }  // present for Success/Duplicate
        public string ErrorMessage { get; set; }  // for BusinessRuleFailed/Error
    }

    /// <summary>
    /// Payment success payload used to authorize water collection through Oracle procedure.
    /// </summary>
    public class WaterCollectionSuccessRequest
    {
        public long ConnectionNo { get; set; }      // xws_connno
        public long BookNo { get; set; }            // xws_bookno
        public long SrNo { get; set; }              // xws_srno
        public long ReceiptNo { get; set; }         // xws_receiptno
        public decimal TotalPaid { get; set; }      // xtotalpaid
        public long TransactionNo { get; set; }     // transaction_no

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("razorpay_payment_id")]
        public string RazorpayPaymentId { get; set; }

        [JsonProperty("payment_id")]
        public string PaymentId
        {
            get { return RazorpayPaymentId; }
            set { RazorpayPaymentId = value; }
        }

        [JsonProperty("razorpay_payment_link_id")]
        public string RazorpayPaymentLinkId { get; set; }

        [JsonProperty("payment_link_id")]
        public string PaymentLinkId
        {
            get { return RazorpayPaymentLinkId; }
            set { RazorpayPaymentLinkId = value; }
        }

        [JsonProperty("razorpay_payment_link_reference_id")]
        public string RazorpayPaymentLinkReferenceId { get; set; }

        [JsonProperty("payment_link_reference_id")]
        public string PaymentLinkReferenceId
        {
            get { return RazorpayPaymentLinkReferenceId; }
            set { RazorpayPaymentLinkReferenceId = value; }
        }

        [JsonProperty("razorpay_payment_link_status")]
        public string RazorpayPaymentLinkStatus { get; set; }

        [JsonProperty("payment_link_status")]
        public string PaymentLinkStatus
        {
            get { return RazorpayPaymentLinkStatus; }
            set { RazorpayPaymentLinkStatus = value; }
        }

        [JsonProperty("razorpay_signature")]
        public string RazorpaySignature { get; set; }

        [JsonProperty("signature")]
        public string Signature
        {
            get { return RazorpaySignature; }
            set { RazorpaySignature = value; }
        }

        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class WaterCollectionSuccessResponse
    {
        public bool Authorized { get; set; }
        public string Message { get; set; }
        public long ConnectionNo { get; set; }
        public long TransactionNo { get; set; }
    }

    public class RazorpayPaymentLinkRequest
    {
        public long ConnectionNo { get; set; }

        [JsonProperty("connection_number")]
        public string ConnectionNumber { get; set; }

        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public long AmountInPaise { get; set; }
        public string Currency { get; set; }

        [JsonProperty("accept_partial")]
        public bool AcceptPartial { get; set; }

        [JsonProperty("first_min_partial_amount")]
        public decimal FirstMinPartialAmount { get; set; }

        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }

        public string Description { get; set; }

        [JsonProperty("callback_url")]
        public string CallbackUrl { get; set; }
    }
}
