# ? Route Changed to `/downloadconsent`

## Summary
The consent document download route has been successfully changed from `/download` to `/downloadconsent` as requested.

---

## ?? Changes Made

### 1. Controller Route Updated
**File:** `Controllers\DepositManager\ConsentDocumentController.cs`

**Changed from:**
```csharp
[Route("download")]
```

**Changed to:**
```csharp
[Route("downloadconsent")]
```

### 2. Public Endpoints Updated
**File:** `Security\ApiKeyAuthenticationHandler.cs`

**Updated array:**
```csharp
private static readonly string[] PublicEndpoints = new[]
{
    "/api/auth/login",
    "/api/auth/bank/login",
    "/api/auth/account/login",
    "/api/auth/commissioner/login",
    "/api/booth/login",
    "/api/ftp-diagnostic/network-info",
    "/api/ftp-diagnostic/config",
    "/api/ftp-diagnostic/test",
    "/api/deposits/consent/health",
    "/api/deposits/consent/downloadconsent",  // ? UPDATED
    "/api/deposits/consent/info"
};
```

### 3. Documentation Updated
**File:** `Docs\ConsentDocumentController_Logging.md`

Updated route references to use `downloadconsent`

### 4. Download URL Updated
The `info` endpoint now returns the correct download URL with `downloadconsent` route.

---

## ? New Endpoints

### 1. Health Check (unchanged)
```
GET http://localhost:57031/api/deposits/consent/health
```

### 2. Download Consent Document (NEW ROUTE)
```
GET http://localhost:57031/api/deposits/consent/downloadconsent?requirementId=REQ0000000024&bankId=BNK00011&fileName=Ehrms_portal_info.pdf
```

### 3. Document Info (unchanged)
```
GET http://localhost:57031/api/deposits/consent/info?requirementId=REQ0000000024&bankId=BNK00011&fileName=Ehrms_portal_info.pdf
```

---

## ?? Next Steps - RESTART REQUIRED

### Important: You MUST restart the application for changes to take effect!

1. **In Visual Studio: Press `Shift + F5`** to stop debugging
2. **Wait 5 seconds**
3. **Press `F5`** to start debugging again
4. **Test the new route in Postman**

---

## ?? Test in Postman

### Correct URL:
```
GET http://localhost:57031/api/deposits/consent/downloadconsent?requirementId=REQ0000000024&bankId=BNK00011&fileName=Ehrms_portal_info.pdf
```

**Headers:** None (no authentication required)

**Expected Response:**
- If file exists: PDF downloads
- If file doesn't exist: JSON error with `"error": "FILE_NOT_FOUND"`

---

## ? Build Status
? **Build Successful** - All changes compile correctly

---

## ?? All Available Routes

| Endpoint | Route | Auth Required |
|----------|-------|---------------|
| Health Check | `/api/deposits/consent/health` | No |
| Download | `/api/deposits/consent/downloadconsent` | No |
| Info | `/api/deposits/consent/info` | No |

---

**Remember: Restart the application and use the NEW route `/downloadconsent`!** ??
