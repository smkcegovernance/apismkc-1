using System;
using System.Collections.Generic;
using System.Text;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface ISWRService
    {
        SWRApiResponse Register(SWRRegistrationRequest request);
        SWRApiResponse Update(string identifier, SWRRegistrationRequest request);
        SWRApiResponse GetRegistration(string identifier);
        SWRApiResponse Search(string searchText, string status, string applicationMode);
        SWRApiResponse UpdateStatus(string identifier, SWRStatusUpdateRequest request);
    }

    public class SWRService : ISWRService
    {
        private readonly ISWRRepository _repository;

        public SWRService(ISWRRepository repository)
        {
            _repository = repository;
        }

        // ---------------------------------------------------------------
        //  Register
        // ---------------------------------------------------------------
        public SWRApiResponse Register(SWRRegistrationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return SWRApiResponse.CreateError("Full name is required", "VALIDATION_ERROR");
            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                return SWRApiResponse.CreateError("Mobile number is required", "VALIDATION_ERROR");

            var saveResult = _repository.SaveRegistration(request, null);
            if (!saveResult.Success)
                return SWRApiResponse.CreateError(saveResult.Message, saveResult.ErrorCode ?? "SAVE_ERROR");

            var regId = saveResult.Data.RegId;

            // Save schemes if provided
            if (request.Schemes != null && request.Schemes.Count > 0)
            {
                var schemesResult = _repository.ReplaceSchemes(regId, request.Schemes);
                if (!schemesResult.Success)
                    return SWRApiResponse.CreateError(schemesResult.Message, schemesResult.ErrorCode ?? "SCHEME_ERROR");
            }

            return SWRApiResponse.CreateSuccess(
                new { regId, registrationNumber = saveResult.Data.RegistrationNumber },
                "Registration completed successfully",
                saveResult.Data.RegistrationNumber
            );
        }

        // ---------------------------------------------------------------
        //  Update
        // ---------------------------------------------------------------
        public SWRApiResponse Update(string identifier, SWRRegistrationRequest request)
        {
            var resolveResult = _repository.ResolveRegistration(identifier);
            if (!resolveResult.Success)
                return SWRApiResponse.CreateError(resolveResult.Message, resolveResult.ErrorCode ?? "NOT_FOUND");

            dynamic existing = resolveResult.Data;
            int regId = Convert.ToInt32(existing["reg_id"]);

            var saveResult = _repository.SaveRegistration(request, regId);
            if (!saveResult.Success)
                return SWRApiResponse.CreateError(saveResult.Message, saveResult.ErrorCode ?? "SAVE_ERROR");

            if (request.Schemes != null)
            {
                var schemesResult = _repository.ReplaceSchemes(regId, request.Schemes);
                if (!schemesResult.Success)
                    return SWRApiResponse.CreateError(schemesResult.Message, schemesResult.ErrorCode ?? "SCHEME_ERROR");
            }

            return SWRApiResponse.CreateSuccess(
                new { regId, registrationNumber = saveResult.Data.RegistrationNumber },
                "Registration updated successfully",
                saveResult.Data.RegistrationNumber
            );
        }

        // ---------------------------------------------------------------
        //  Get
        // ---------------------------------------------------------------
        public SWRApiResponse GetRegistration(string identifier)
        {
            var result = _repository.ResolveRegistration(identifier);
            if (!result.Success)
                return SWRApiResponse.CreateError(result.Message, result.ErrorCode ?? "NOT_FOUND");

            return SWRApiResponse.CreateSuccess(result.Data, "OK");
        }

        // ---------------------------------------------------------------
        //  Search
        // ---------------------------------------------------------------
        public SWRApiResponse Search(string searchText, string status, string applicationMode)
        {
            var result = _repository.Search(searchText, status, applicationMode);
            if (!result.Success)
                return SWRApiResponse.CreateError(result.Message, result.ErrorCode ?? "SEARCH_ERROR");

            return SWRApiResponse.CreateSuccess(result.Data, "OK");
        }

        // ---------------------------------------------------------------
        //  Update Status
        // ---------------------------------------------------------------
        public SWRApiResponse UpdateStatus(string identifier, SWRStatusUpdateRequest request)
        {
            var resolveResult = _repository.ResolveRegistration(identifier);
            if (!resolveResult.Success)
                return SWRApiResponse.CreateError(resolveResult.Message, resolveResult.ErrorCode ?? "NOT_FOUND");

            dynamic existing = resolveResult.Data;
            int regId = Convert.ToInt32(existing["reg_id"]);

            var result = _repository.UpdateStatus(regId, request.Status, request.Remarks, request.OperatorUserId);
            if (!result.Success)
                return SWRApiResponse.CreateError(result.Message, result.ErrorCode ?? "UPDATE_ERROR");

            return SWRApiResponse.CreateSuccess(null, "Status updated successfully");
        }
    }
}
