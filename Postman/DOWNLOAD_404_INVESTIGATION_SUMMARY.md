# Download 404 Issue - Investigation Summary

## Problem Statement
- ? Upload consent documents: **WORKING**
- ? Download consent documents: **Returns 404**

## Investigation Findings

### What's Working
1. ? Controller is properly registered in `SimpleDependencyResolver`
2. ? Network storage service is configured correctly (admin share path)
3. ? Upload functionality proves authentication is working
4. ? Files are successfully saved to `\\192.168.40.47\c$\inetpub\ftproot\BankConsents`
5. ? Project builds successfully without errors

### What's Configured
1. ? Route prefix: `[RoutePrefix("api/deposits/consent")]`
2. ? Download route: `[Route("download")]`
3. ? HTTP method: `[HttpGet]`
4. ? Authentication: `[ShaAuthentication]`
5. ? Rate limiting: `[RateLimit(maxRequests: 100, timeWindowMinutes: 1)]`

### Possible Causes of 404

#### 1. **IIS Not Restarted** (Most Likely)
After code changes, IIS or the application pool needs to be restarted to load new DLLs.

**Solution:**
```powershell
# Option 1: Restart IIS
iisreset

# Option 2: Restart specific app pool
Restart-WebAppPool -Name "YourAppPoolName"
```

#### 2. **Authentication Failing Silently**
The `[ShaAuthentication]` filter might be rejecting requests before they reach the controller. Web API sometimes returns 404 instead of 401 for routing issues.

**Solution:**
- Test the health check endpoint first: `GET /api/deposits/consent/health` (no auth required)
- Verify all authentication headers are present and correct
- Check logs for authentication errors

#### 3. **Route Order Conflict**
Another route might be matching before the download route.

**Solution:**
- Check for conflicting routes in other controllers
- Ensure `config.MapHttpAttributeRoutes()` is called first in `WebApiConfig.cs`

#### 4. **URL Rewrite Rules**
IIS URL rewrite rules in `Web.config` might be interfering.

**Solution:**
- Check `<system.webServer><rewrite><rules>` section
- Temporarily disable rewrite rules to test

#### 5. **DLL Deployment Issue**
The updated DLL might not be deployed to the production server.

**Solution:**
- Verify `SmkcApi.dll` timestamp matches build time
- Check file location: `C:\smkcapi_published\bin\SmkcApi.dll`

## Immediate Action Items

### Priority 1: Deploy and Restart
```powershell
# 1. Copy latest DLLs to production
Copy-Item "bin\*.dll" -Destination "C:\smkcapi_published\bin\" -Force

# 2. Restart IIS
iisreset

# 3. Test health check
Invoke-WebRequest -Uri "http://your-server/api/deposits/consent/health"
```

### Priority 2: Test with Correct Authentication
```http
GET /api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=test.pdf
Headers:
  X-Api-Key: your-api-key
  X-Timestamp: unix-timestamp
  X-Signature: sha256-hmac-signature
  Accept: application/pdf
```

### Priority 3: Check Logs
```
Location: C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt

Look for:
- "CONSENT_CONTROLLER_DOWNLOADCONSENTDOCUMENT - Starting request"
- "File not found" or "Access denied" errors
- Authentication failures
```

## Files Modified

### 1. `Web.config`
```xml
<!-- Updated to use admin share -->
<add key="Network_Share" value="c$\inetpub\ftproot\BankConsents" />
<add key="Ftp_User" value="administrator" />
<add key="Ftp_Password" value="smkc@1234" />
```

### 2. `Controllers\DepositManager\ConsentDocumentController.cs`
- ? Added health check endpoint (no authentication)
- ? Download endpoint properly configured
- ? Info endpoint properly configured

### 3. `App_Start\SimpleDependencyResolver.cs`
- ? ConsentDocumentController already registered
- ? NetworkStorageService configured as IFtpStorageService

## Testing Strategy

### Test 1: Health Check (No Auth)
```http
GET http://your-server/api/deposits/consent/health
Expected: 200 OK with JSON response
```

**If fails:** Routing is broken ? Check IIS restart and route registration

**If succeeds:** Continue to Test 2

### Test 2: Download with Auth
```http
GET http://your-server/api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=test.pdf
Headers: X-Api-Key, X-Timestamp, X-Signature, Accept
Expected: 200 OK with file content or 404 if file doesn't exist
```

**If fails with 404:** Check authentication headers and logs

**If succeeds:** ? Issue resolved!

### Test 3: Verify File Download
1. Upload a file using the working upload endpoint
2. Note the returned fileName
3. Download that specific file using the download endpoint
4. Verify the downloaded content matches the uploaded file

## Configuration Reference

### Current Storage Configuration
```xml
<add key="Storage_Type" value="network" />
<add key="Network_Server" value="192.168.40.47" />
<add key="Network_Share" value="c$\inetpub\ftproot\BankConsents" />
<add key="Ftp_User" value="administrator" />
<add key="Ftp_Password" value="smkc@1234" />
```

### Directory Structure
```
\\192.168.40.47\c$\inetpub\ftproot\BankConsents\
??? REQ0000000001\
?   ??? BANK001\
?   ?   ??? uploaded_file.pdf
?   ??? BANK002\
?       ??? another_file.pdf
??? REQ0000000002\
    ??? BANK001\
        ??? document.pdf
```

## Expected Behavior After Fix

### Successful Download (200 OK)
```json
{
  "success": true,
  "message": "Consent document retrieved successfully",
  "data": {
    "fileName": "test.pdf",
    "fileData": "JVBERi0xLjQKJeLjz9MK...",
    "contentType": "application/pdf",
    "requirementId": "REQ001",
    "bankId": "BANK001",
    "downloadedAt": "2025-02-03T10:30:00Z"
  }
}
```

### File Not Found (404)
```json
{
  "success": false,
  "message": "Consent document not found on storage server",
  "error": "FILE_NOT_FOUND",
  "data": {
    "requirementId": "REQ001",
    "bankId": "BANK001",
    "fileName": "test.pdf",
    "hint": "Check logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt"
  }
}
```

## Common Pitfalls to Avoid

? **Don't:** Forget to restart IIS after code changes
? **Do:** Always run `iisreset` after deploying new DLLs

? **Don't:** Test without proper authentication headers
? **Do:** Include X-Api-Key, X-Timestamp, and X-Signature

? **Don't:** Assume the route is broken
? **Do:** Test health check endpoint first to verify routing

? **Don't:** Mix up requirementId, bankId, and fileName parameters
? **Do:** Use exact values from upload response or database

? **Don't:** Use POST method for download
? **Do:** Use GET method as specified

## Next Steps

1. **Immediate:**
   - Deploy updated DLLs to production server
   - Restart IIS application pool
   - Test health check endpoint

2. **Short-term:**
   - Test download with valid authentication
   - Verify file retrieval works
   - Check logs for any errors

3. **Long-term:**
   - Set up automated deployment
   - Add monitoring for endpoint availability
   - Create integration tests

## Support Resources

- **Detailed Troubleshooting:** `Postman\TROUBLESHOOT_404_DOWNLOAD.md`
- **Configuration Guide:** `Postman\NETWORK_SHARE_CONFIGURATION.md`
- **Download API Guide:** `Postman\CONSENT_DOCUMENT_DOWNLOAD_GUIDE.md`
- **Logs Location:** `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

## Summary

? **Code is correct** - Controller, routing, and DI are properly configured
? **Build successful** - No compilation errors
? **Storage configured** - Admin share path updated correctly

?? **Most likely issue:** IIS needs to be restarted to load new DLLs
?? **Alternative issue:** Authentication headers not being sent correctly

**Recommended Action:** Deploy DLLs + Restart IIS + Test health check endpoint
