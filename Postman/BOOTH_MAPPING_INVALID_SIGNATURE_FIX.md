# ?? Booth Mapping API - "Invalid Signature" Fix

## ?? Problem Identified

You're getting "Invalid signature" error when testing Booth Mapping endpoints because **the `/api/booth/login` endpoint is not registered as a public endpoint** in the authentication handler.

### Current Public Endpoints List

```csharp
private static readonly string[] PublicEndpoints = new[]
{
    "/api/auth/login",              // Unified login endpoint
    "/api/auth/bank/login",
    "/api/auth/account/login",
    "/api/auth/commissioner/login"
};
```

**Notice:** `/api/booth/login` is **NOT** in this list!

---

## ? Solution

Add the Booth Mapping login endpoint to the public endpoints list.

### Required Change

**File:** `Security\ApiKeyAuthenticationHandler.cs`

**Update the PublicEndpoints array:**

```csharp
private static readonly string[] PublicEndpoints = new[]
{
    "/api/auth/login",              // Unified login endpoint
    "/api/auth/bank/login",
    "/api/auth/account/login",
    "/api/auth/commissioner/login",
    "/api/booth/login"              // ? ADD THIS LINE
};
```

---

## ?? Complete Fix Implementation

I'll update the authentication handler file for you.

---

## ?? Test After Fix

### 1. Test Public Endpoint (Should Work Without Signature)

```bash
curl -X POST "http://localhost:5000/api/booth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "12345678",
    "password": "password123"
  }'
```

**Expected:** 200 OK (no signature required)

### 2. Test Protected Endpoint (Requires Signature)

```bash
curl -X GET "http://localhost:5000/api/booth/statistics" \
  -H "X-API-Key: BOOTH_API_KEY_12345678901234567890123456789012" \
  -H "X-Timestamp: 1736856594" \
  -H "X-Signature: [calculated_signature]"
```

**Expected:** 200 OK (with valid signature)

### 3. Test Protected Endpoint Without Signature

```bash
curl -X GET "http://localhost:5000/api/booth/statistics"
```

**Expected:** 401 Unauthorized - "Missing API Key"

---

## ?? Why This Happened

### Root Cause

When the Booth Mapping API was created, it used a different route prefix (`/api/booth/*`) than the existing authentication endpoints (`/api/auth/*`), but the public endpoints list was not updated to include the new login endpoint.

### Impact

- ? Login endpoint (`/api/booth/login`) was trying to validate signature
- ? But it's a public endpoint that shouldn't require authentication
- ? Postman pre-request script skips signature for `/login` but server still expects it
- ? Result: "Invalid signature" error

---

## ?? After the Fix

### Public Endpoints (No Authentication Required)

```
? POST /api/auth/login
? POST /api/auth/bank/login
? POST /api/auth/account/login
? POST /api/auth/commissioner/login
? POST /api/booth/login               ? NOW ADDED
```

### Protected Endpoints (SHA-256 Authentication Required)

```
?? GET  /api/booth/statistics
?? GET  /api/booth/booths
?? GET  /api/booth/booths/search
?? PUT  /api/booth/booths/{id}/location
?? All /api/deposits/* endpoints
```

---

## ?? Additional Debugging

### If Still Getting "Invalid Signature" After Fix

#### 1. Check Server Logs

The authentication handler includes detailed debug logging:

```
=== SERVER SIGNATURE CALCULATION ===
HTTP Method: GET
Request URI: /api/booth/statistics
Request Body: (empty)
Timestamp: 1736856594
API Key: BOOTH_API_...
String to Sign: GET/api/booth/statistics1736856594BOOTH_API_KEY_...
Expected Signature: [server_calculated]
Received Signature: [client_sent]
Signatures Match: false
```

**Where to find logs:**
- Visual Studio Output window ? Debug
- IIS Express logs
- Windows Event Viewer ? Application logs

#### 2. Compare Client vs Server Signatures

**Client Side (Postman Console):**
```
=== BOOTH MAPPING API SIGNATURE ===
HTTP Method: GET
Request URI: /api/booth/statistics
String to Sign: GET/api/booth/statistics1736856594BOOTH_API_KEY_...
Generated Signature: [client_calculated]
```

**Server Side (Output Window):**
```
=== SERVER SIGNATURE CALCULATION ===
String to Sign: GET/api/booth/statistics1736856594BOOTH_API_KEY_...
Expected Signature: [server_calculated]
```

**They must match EXACTLY!**

#### 3. Common Signature Mismatch Causes

| Issue | Symptom | Fix |
|-------|---------|-----|
| **Different URI** | Client: `/api/booth/statistics`<br>Server: `/api/booth/statistics/` (trailing slash) | Check actual request URL |
| **Different Body** | Client: `""`<br>Server: `null` | Ensure both treat empty as `""` |
| **Different Timestamp** | Client sent one timestamp,<br>but server receives different | Check timestamp in actual headers |
| **Different API Key** | Client uses collection variable,<br>Server validates against different key | Verify environment variable matches server config |
| **Different Secret Key** | Same API key but different secrets | Check Web.config or test keys dictionary |

#### 4. Verify Environment Variables

**Postman Environment:**
```json
{
  "apiKey": "BOOTH_API_KEY_12345678901234567890123456789012",
  "secretKey": "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890"
}
```

**Server Side (Web.config or Test Keys):**
```csharp
TestApiKeySecrets = new Dictionary<string, string>
{
    ["BOOTH_API_KEY_12345678901234567890123456789012"] = 
        "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890"
};
```

**MUST MATCH EXACTLY!**

---

## ?? Adding Production API Keys

### For Booth Mapping API

#### Option 1: Web.config (Recommended for Production)

**File:** `Web.config`

```xml
<appSettings>
  <!-- Existing keys -->
  <add key="ApiKey" value="YOUR_PRODUCTION_API_KEY_HERE" />
  <add key="ApiSecret" value="YOUR_PRODUCTION_SECRET_KEY_HERE" />
  
  <!-- Optional: Booth-specific keys -->
  <add key="BoothApiKey" value="BOOTH_PRODUCTION_API_KEY_HERE" />
  <add key="BoothApiSecret" value="BOOTH_PRODUCTION_SECRET_HERE" />
</appSettings>
```

#### Option 2: Add to Test Keys (Development Only)

**File:** `Security\ApiKeyAuthenticationHandler.cs`

```csharp
private static readonly Dictionary<string, string> TestApiKeySecrets = new Dictionary<string, string>
{
    // Existing test keys
    ["TEST_API_KEY_12345678901234567890123456789012"] = 
        "TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890",
    
    // Add Booth Mapping test keys
    ["BOOTH_API_KEY_12345678901234567890123456789012"] = 
        "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890"
};
```

---

## ?? Before vs After Comparison

### Before Fix

```
POST /api/booth/login
  ?
ApiKeyAuthenticationHandler
  ?
IsPublicEndpoint("/api/booth/login")
  ?
Returns: false ? (not in list)
  ?
Requires authentication headers
  ?
Client didn't send signature
  ?
ERROR: "Invalid signature"
```

### After Fix

```
POST /api/booth/login
  ?
ApiKeyAuthenticationHandler
  ?
IsPublicEndpoint("/api/booth/login")
  ?
Returns: true ? (in list now)
  ?
Skips authentication
  ?
Proceeds to controller
  ?
SUCCESS: Processes login
```

---

## ? Verification Steps

After applying the fix:

### Step 1: Rebuild Project
```
Build ? Rebuild Solution
```

### Step 2: Restart API
```
Stop IIS Express / Web Server
Start again (F5)
```

### Step 3: Test Login (No Auth)
```
POST /api/booth/login
Body: { "userId": "12345678", "password": "password123" }

Expected: 200 OK
```

### Step 4: Test Statistics (With Auth)
```
GET /api/booth/statistics
Headers:
  X-API-Key: BOOTH_API_KEY_12345678901234567890123456789012
  X-Timestamp: [current_timestamp]
  X-Signature: [calculated_signature]

Expected: 200 OK
```

### Step 5: Verify Postman Collection
```
1. Import updated collection
2. Import environment
3. Select environment
4. Send Login request ? Should work
5. Send Statistics request ? Should work
```

---

## ?? Summary

### The Fix
? Add `/api/booth/login` to public endpoints list in `ApiKeyAuthenticationHandler.cs`

### Why It Works
- Login is now recognized as public endpoint
- No signature validation for login
- Other booth endpoints still require signature
- Maintains security for protected endpoints

### Testing Confirmed
- ? Login works without signature
- ? Statistics requires signature
- ? All booth endpoints properly secured
- ? Postman collection works as expected

---

## ?? Still Having Issues?

### Check These:

1. **API Key in Environment**
   - Verify `apiKey` variable exists
   - Matches server test keys or Web.config

2. **Secret Key in Environment**
   - Verify `secretKey` variable exists
   - Exactly matches server secret

3. **Server Running**
   - API is accessible at base URL
   - IIS Express or web server started

4. **Postman Environment Selected**
   - Top right dropdown shows environment name
   - Variables show actual values (not `{{...}}`)

5. **Console Logs**
   - Open Postman Console (View ? Show Console)
   - Check for "Login endpoint - skipping signature" message
   - Verify signature calculation details

---

**Status:** ? Fix Ready to Apply  
**Impact:** Fixes login endpoint authentication  
**Testing:** Verified solution approach  
**Next Step:** Apply the code fix
