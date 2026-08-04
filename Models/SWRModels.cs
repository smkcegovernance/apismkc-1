using System;
using System.Collections.Generic;

namespace SmkcApi.Models
{
    // ---------------------------------------------------------------
    //  Single Women Empowerment Registration — Model classes
    //  Ekal Mahila Sakshamikaran Karyakram
    // ---------------------------------------------------------------

    public class SWRApiResponse
    {
        public bool   Success            { get; set; }
        public string Message            { get; set; }
        public string RegistrationNumber { get; set; }
        public object Data               { get; set; }
        public string ErrorCode          { get; set; }
        public DateTime Timestamp        { get; set; }

        public SWRApiResponse() { Timestamp = DateTime.UtcNow; }

        public static SWRApiResponse CreateSuccess(object data, string message, string registrationNumber = null)
        {
            return new SWRApiResponse { Success = true, Message = message, Data = data, RegistrationNumber = registrationNumber };
        }

        public static SWRApiResponse CreateError(string message, string errorCode)
        {
            return new SWRApiResponse { Success = false, Message = message, ErrorCode = errorCode };
        }
    }

    public class SWROperationResult<T>
    {
        public bool   Success   { get; set; }
        public string Message   { get; set; }
        public T      Data      { get; set; }
        public string ErrorCode { get; set; }
    }

    public class SWRRegistrationRequest
    {
        // Application metadata
        public string ApplicationMode  { get; set; }   // ONLINE / OPERATOR

        // Personal Info
        public string FullName         { get; set; }
        public string Dob              { get; set; }   // yyyy-MM-dd
        public string MobileNumber     { get; set; }
        public string AadhaarNumber    { get; set; }

        // Location
        public string District         { get; set; }
        public string Taluka           { get; set; }
        public string Village          { get; set; }
        public string Ward             { get; set; }

        // Category
        public string WomenCategory    { get; set; }   // RURAL/URBAN/DOMESTIC/ADDITIONAL
        public string AgeGroup         { get; set; }   // 18-29/30-44/45-54/55-64+

        // Livelihood
        public string Occupation       { get; set; }
        public string AnnualIncome     { get; set; }
        public string MgnregaYears     { get; set; }

        // Children
        public string ChildrenBelow6   { get; set; }
        public string Children6To14    { get; set; }
        public string OtherInfo        { get; set; }
        public string PrimaryEarner    { get; set; }

        // Schemes — pipe-separated "CODE|Y" or "CODE|N"
        public List<string> Schemes    { get; set; }

        // Operator context
        public int?   OperatorUserId   { get; set; }
    }

    public class SWRStatusUpdateRequest
    {
        public string Status         { get; set; }
        public string Remarks        { get; set; }
        public int?   OperatorUserId { get; set; }
    }

    public class SWRSaveData
    {
        public int    RegId             { get; set; }
        public string RegistrationNumber { get; set; }
    }
}
