# Troubleshooting: Download Endpoint Returning 404

## Problem
Upload is working correctly, but download endpoint `/api/deposits/consent/download` is returning HTTP 404.

## Root Cause Analysis

Since **upload is working**, we know:
- ? The controller IS registered in DI (`SimpleDependencyResolver`)
- ? The authentication is working (`ShaAuthentication`)
- ? The network/FTP storage service is configured correctly
- ? Files are being uploaded successfully

The 404 error for download suggests one of these issues:

### Issue #1: Route Not Being Matched
The route `/api/deposits/consent/download` might not be matching due to:
- IIS URL rewrite rules interfering
- Route order conflicts
- Missing route registration

### Issue #2: Authentication Failing Before Controller
The `ShaAuthentication` filter might be rejecting the request before it reaches the controller, causing Web API to return 404 instead of 401/403.

### Issue #3: HTTP Method Mismatch
The endpoint is marked as `[HttpGet]` but the request might be using a different method.

### Issue #4: IIS Application Not Restarted
After code changes, IIS might still be serving old DLLs.

## Solution Steps

### Step 1: Test the Health Check Endpoint (No Authentication)

First, verify the controller is accessible by testing the health check endpoint that doesn't require authentication:

```http
GET http://your-server/api/deposits/consent/health
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "ConsentDocumentController is accessible",
  "timestamp": "2025-02-03T10:30:00Z",
  "storageType": "network"
}
```

**If this returns 404:** The controller routing is broken. Skip to Step 5.

**If this returns 200 OK:** The routing works, authentication might be the issue. Continue to Step 2.

---

### Step 2: Check IIS Application Pool

Restart the IIS application pool to ensure new DLLs are loaded:

```powershell
# Open IIS Manager or use PowerShell
Restart-WebAppPool -Name "YourAppPoolName"

# Or restart IIS completely
iisreset
```

---

### Step 3: Verify Authentication Headers

The download endpoint requires SHA authentication. Verify you're sending all required headers:

```http
GET /api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=test.pdf
Headers:
  X-Api-Key: your-api-key
  X-Timestamp: 1675430000
  X-Signature: calculated-sha256-signature
  Accept: application/pdf
```

**Missing any of these headers will cause authentication to fail.**

---

### Step 4: Check Logs

Check the application logs to see if the request is even reaching the controller:

**Location:** `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

Look for entries like:
```
CONSENT_CONTROLLER_DOWNLOADCONSENTDOCUMENT - Starting request
```

**If you DON'T see these logs:**
- The request is being blocked before reaching the controller
- Authentication filter is rejecting it
- Route is not matching

**If you DO see these logs:**
- The controller is being hit
- Check the log details for the actual error

---

### Step 5: Verify Route Registration

Check that attribute routing is enabled in `WebApiConfig.cs`:

```csharp
public static void Register(HttpConfiguration config)
{
    // This MUST be present
    config.MapHttpAttributeRoutes();
    
    // ...rest of config
}
```

---

### Step 6: Test Without Authentication (Temporary)

Temporarily remove the `[ShaAuthentication]` attribute from the download method to test if authentication is the problem:

```csharp
[HttpGet]
[Route("download")]
// [ShaAuthentication]  <-- Comment this out temporarily
// [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
public HttpResponseMessage DownloadConsentDocument(...)
{
    // ...
}
```

**Rebuild and test again.**

**If it works now:** Authentication is the issue. Verify your headers.
**If it still returns 404:** Routing is the issue.

---

### Step 7: Check Web.config for URL Rewrite Rules

Check if there are any URL rewrite rules in `Web.config` that might be interfering:

```xml
<system.webServer>
  <rewrite>
    <rules>
      <!-- Check for any rules that might block /api/deposits/consent/download -->
    </rules>
  </rewrite>
</system.webServer>
```

---

### Step 8: Verify File Structure

Ensure the DLL files are in the correct location:

```
C:\smkcapi_published\bin\
??? SmkcApi.dll         <-- Must contain ConsentDocumentController
??? SmkcApi.pdb
??? ... other DLLs
```

**Check DLL timestamp to ensure it's the latest version:**

```powershell
Get-ChildItem "C:\smkcapi_published\bin\SmkcApi.dll" | Select-Object Name, LastWriteTime
```

---

### Step 9: Use IIS Failed Request Tracing

Enable Failed Request Tracing in IIS to see exactly why the request is failing:

1. Open IIS Manager
2. Select your website
3. Double-click "Failed Request Tracing Rules"
4. Add a rule for status code 404
5. Reproduce the issue
6. Check the trace log files

---

### Step 10: Test with Postman/PowerShell

Test the endpoint with a simple PowerShell script to eliminate client-side issues:

```powershell
# Test health check (no auth)
Invoke-WebRequest -Uri "http://your-server/api/deposits/consent/health" -Method GET

# Test download with authentication
$apiKey = "your-api-key"
$apiSecret = "your-api-secret"
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

# Calculate signature (simplified - you need proper HMAC-SHA256)
$signature = "calculated-signature"

$headers = @{
    "X-Api-Key" = $apiKey
    "X-Timestamp" = $timestamp
    "X-Signature" = $signature
    "Accept" = "application/pdf"
}

$params = @{
    requirementId = "REQ001"
    bankId = "BANK001"
    fileName = "test.pdf"
}

$uri = "http://your-server/api/deposits/consent/download?" + ($params.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join "&"

Invoke-WebRequest -Uri $uri -Method GET -Headers $headers
```

---

## Quick Checklist

- [ ] Health check endpoint works (`/api/deposits/consent/health`)
- [ ] IIS application pool restarted
- [ ] All authentication headers are present and correct
- [ ] Logs show the request reaching the controller
- [ ] `config.MapHttpAttributeRoutes()` is present in WebApiConfig
- [ ] Latest DLLs are deployed to the server
- [ ] No URL rewrite rules blocking the endpoint
- [ ] HTTP method is GET (not POST)
- [ ] URL parameters are correct (requirementId, bankId, fileName)

---

## Common Mistakes

### ? Mistake #1: Missing Authentication Headers
```http
GET /api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=test.pdf
# Missing X-Api-Key, X-Timestamp, X-Signature headers
```

### ? Mistake #2: Wrong URL Format
```http
# Wrong - missing parameters
GET /api/deposits/consent/download

# Correct - all parameters included
GET /api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=test.pdf
```

### ? Mistake #3: Using POST Instead of GET
```http
POST /api/deposits/consent/download  # ? Wrong method
GET /api/deposits/consent/download   # ? Correct method
```

### ? Mistake #4: Old DLLs Still Loaded
After making code changes, you MUST restart IIS or the app pool.

---

## Expected Behavior

### Success Case (200 OK)
```json
{
  "success": true,
  "message": "Consent document retrieved successfully",
  "data": {
    "fileName": "test.pdf",
    "fileData": "base64_encoded_content...",
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
    "fileName": "test.pdf"
  }
}
```

### Authentication Failed (401)
```json
{
  "error": "Invalid signature or missing authentication headers"
}
```

---

## Still Getting 404?

If you've tried all the steps above and still getting 404:

1. **Check the exact URL you're calling:**
   ```
   http://your-server/api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=test.pdf
   ```

2. **Verify the controller is compiled into the DLL:**
   ```powershell
   # Use a decompiler or check the DLL
   ildasm "C:\smkcapi_published\bin\SmkcApi.dll"
   ```

3. **Check IIS application logs:**
   - Event Viewer ? Windows Logs ? Application
   - Look for ASP.NET errors

4. **Enable detailed errors in Web.config:**
   ```xml
   <system.web>
     <customErrors mode="Off" />
   </system.web>
   ```

5. **Contact support with:**
   - Exact URL being called
   - All request headers
   - Response status code and body
   - Logs from `FtpLog_YYYYMMDD.txt`
   - IIS application pool name
   - Server name and IIS version

---

## Next Steps After Fix

Once the endpoint is working:

1. ? Test with real uploaded files
2. ? Verify authentication is working
3. ? Test binary download (Accept: application/pdf)
4. ? Test JSON response (Accept: application/json)
5. ? Test error cases (missing parameters, file not found)
6. ? Monitor logs for any issues
7. ? Update frontend to use the endpoint

---

## Related Files

- Controller: `Controllers\DepositManager\ConsentDocumentController.cs`
- DI Registration: `App_Start\SimpleDependencyResolver.cs`
- Routing Config: `App_Start\WebApiConfig.cs`
- Storage Service: `Services\DepositManager\NetworkStorageService.cs`
- Logs: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`
