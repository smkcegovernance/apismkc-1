using System;

namespace SmkcApi.Models
{
    public class SmsSendRequest
    {
        // Pass either ConnectionNumber or Ward/Div or both as needed per proc
        public string ConnectionNumber { get; set; } // optional
        public string WardCode { get; set; } // optional
        public string DivCode { get; set; } // optional
        public bool PreviewOnly { get; set; } = false; // if true, don't actually send
    }

    // New request model for water connection bill SMS
    public class WaterBillSmsSendRequest
    {
        public string ConnectionNumber { get; set; } // optional
        public string WardCode { get; set; } // optional
        public string DivCode { get; set; } // optional
        public bool PreviewOnly { get; set; } = false;
    }

    public class SmsSendResult
    {
        public string ConnectionNumber { get; set; }
        public string MobileNumber { get; set; }
        public bool Sent { get; set; }
        public string ProviderResponse { get; set; }
        public string Error { get; set; }
    }
    public class SmsNumberMessage
    {
        public string Number { get; set; }
        public string Message { get; set; }
        public string Connection { get; set; }
    }

    // DTO returned by the proc (map columns)
    public class ConnectionBalanceMobileDto
    {
        public string ConnectionNumber { get; set; }
        public string WardCode { get; set; }
        public string WardName { get; set; }
        public string DivCode { get; set; }
        public string DivName { get; set; }
        public string MobileNumber { get; set; }
        public string CustomerNumber { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal AfterDiscountBalance { get; set; }
        public string Status { get; set; } // ELIGIBLE_*, NO_BALANCE, EXPIRED
        public string QueryType { get; set; }
        public string SearchCriteria { get; set; }
        public DateTime QueryDate { get; set; }
    }

    // DTO for water bill SMS with customer details
    public class WaterBillSmsDto
    {
        public string ConnectionNumber { get; set; }
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string DueDate { get; set; }
        public string PaymentUrl { get; set; }
    }
}
