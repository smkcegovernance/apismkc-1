namespace SmkcApi.Models.DepositManager
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string Error { get; set; }
        public string ErrorCode { get; set; }
    }
}
