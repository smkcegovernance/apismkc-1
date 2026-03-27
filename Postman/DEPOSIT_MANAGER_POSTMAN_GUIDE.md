# Deposit Manager API - Postman Testing Guide

## ?? Files Included

1. **SMKC_Deposit_Manager.postman_collection.json** - Complete API collection with all endpoints
2. **SMKC_Deposit_Manager.postman_environment.json** - Environment variables configuration
3. **DEPOSIT_MANAGER_POSTMAN_GUIDE.md** - This comprehensive testing guide

---

## ?? Quick Start

### Step 1: Import Collection
1. Open Postman
2. Click **Import** button (top left)
3. Drag and drop `SMKC_Deposit_Manager.postman_collection.json`
4. Collection will appear in left sidebar under "SMKC Deposit Management System"

### Step 2: Import Environment
1. Click **Environments** icon (left sidebar)
2. Click **Import** button
3. Select `SMKC_Deposit_Manager.postman_environment.json`
4. Select "SMKC Deposit Manager - Development" from dropdown (top right)

### Step 3: Verify Configuration
Click on the environment and verify these values:
- `baseUrl`: `http://localhost:57031` (or your API URL)
- `apiKey`: `TEST_API_KEY_12345678901234567890123456789012`
- `secretKey`: `TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890`
- `requirementId`: `REQ-001` (update with actual IDs as needed)
- `bankId`: `BANK-001` (update with actual IDs as needed)

---

## ?? Authentication

All Deposit Manager APIs now require **HMAC-SHA256 authentication** with the following headers:

### Required Headers for GET Requests (3 Headers)
```http
X-API-Key: {{apiKey}}
X-Timestamp: {{timestamp}}
X-Signature: {{signature}}
```

### Required Headers for POST Requests (4 Headers)
```http
Content-Type: application/json
X-API-Key: {{apiKey}}
X-Timestamp: {{timestamp}}
X-Signature: {{signature}}
```

**Note:** GET requests do NOT include Content-Type header (no request body). This matches the proven pattern from the Voter API.

### How It Works
The collection includes a **pre-request script** that automatically:
1. Generates Unix timestamp (seconds)
2. Creates signature string: `METHOD + URI + BODY + TIMESTAMP + APIKEY`
3. Calculates HMAC-SHA256 signature using secret key
4. Base64 encodes the signature
5. Sets `timestamp` and `signature` variables for use in headers

**You don't need to do anything manually!** Just send the request and authentication happens automatically.

---

## ?? API Endpoints Overview

### Bank APIs (4 Endpoints)
**Base Path:** `/api/deposits/bank`

| # | Name | Method | Endpoint | Headers | Description |
|---|------|--------|----------|---------|-------------|
| 1 | Get Published Requirements | GET | `/requirements` | 3 | View available requirements |
| 2 | Get Requirement Details | GET | `/requirements/{id}` | 3 | View specific requirement |
| 3 | Get Bank Quotes | GET | `/quotes` | 3 | View your submitted quotes |
| 4 | Submit Quote | POST | `/quotes/submit` | 4 | Submit/update a quote |

### Account Department APIs (5 Endpoints)
**Base Path:** `/api/deposits/account`

| # | Name | Method | Endpoint | Headers | Description |
|---|------|--------|----------|---------|-------------|
| 1 | Get All Requirements | GET | `/requirements` | 3 | View all requirements |
| 2 | Create Requirement | POST | `/requirements/create` | 4 | Create new requirement |
| 3 | Get All Banks | GET | `/banks` | 3 | View registered banks |
| 4 | Register Bank | POST | `/banks/create` | 4 | Register new bank |
| 5 | Get All Quotes with Rankings | GET | `/quotes` | 3 | View all quotes |

### Commissioner APIs (5 Endpoints)
**Base Path:** `/api/deposits/commissioner`

| # | Name | Method | Endpoint | Headers | Description |
|---|------|--------|----------|---------|-------------|
| 1 | Get Requirements for Review | GET | `/requirements` | 3 | View pending requirements |
| 2 | Get Requirement with Quotes | GET | `/requirements/{id}` | 3 | View requirement details |
| 3 | Authorize Requirement | POST | `/requirements/{id}/authorize` | 4 | Approve requirement |
| 4 | Finalize Deposit | POST | `/requirements/{id}/finalize` | 4 | Select winning bank |
| 5 | Get All Quotes | GET | `/quotes` | 3 | View all quotes |

**Headers:** 3 = Auth only, 4 = Content-Type + Auth

---

## ?? Sample Request & Response

### Example: Get Requirements (GET - 3 Headers)

**Request:**
```http
GET /api/deposits/bank/requirements?status=published
X-API-Key: TEST_API_KEY_12345678901234567890123456789012
X-Timestamp: 1705234800
X-Signature: abc123def456...
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Requirements retrieved successfully",
  "data": {
    "requirements": [...]
  },
  "timestamp": "2025-01-14T12:00:00Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Example: Submit Quote (POST - 4 Headers)

**Request:**
```http
POST /api/deposits/bank/quotes/submit
Content-Type: application/json
X-API-Key: TEST_API_KEY_12345678901234567890123456789012
X-Timestamp: 1705234800
X-Signature: abc123def456...

{
  "requirementId": "REQ-001",
  "bankId": "BANK-001",
  "interestRate": 7.5,
  "remarks": "Best competitive rate",
  "consentDocument": {
    "fileName": "consent.pdf",
    "fileData": "base64_encoded_data_here",
    "fileSize": 123456,
    "uploadedAt": "2025-01-14T12:00:00Z"
  }
}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Quote submitted successfully",
  "data": {
    "quoteId": "QUOTE-001",
    "status": "submitted",
    "submittedAt": "2025-01-14T12:00:00Z"
  },
  "timestamp": "2025-01-14T12:00:00Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

## ?? Testing Workflow

### Workflow 1: Account Department Creates Requirement

**Step 1:** Get existing banks (GET - 3 headers)
```
GET /api/deposits/account/banks
```
- View all registered banks
- Note bank IDs for reference

**Step 2:** Create a new requirement (POST - 4 headers)
```
POST /api/deposits/account/requirements/create
Body:
{
  "schemeName": "Fixed Deposit - High Value",
  "depositType": "callable",
  "amount": 10000000,
  "depositPeriod": 12,
  "validityPeriod": "2025-12-31T23:59:59Z",
  "description": "High value fixed deposit",
  "createdBy": "ACC-USER-001"
}
```
- **Response:** Note the `requirementId` from response
- Update environment variable: `requirementId` = returned ID

**Step 3:** Verify creation (GET - 3 headers)
```
GET /api/deposits/account/requirements
```
- Should see your newly created requirement

### Workflow 2: Commissioner Authorizes Requirement

**Step 1:** View pending requirements (GET - 3 headers)
```
GET /api/deposits/commissioner/requirements?status=pending
```
- View requirements awaiting authorization

**Step 2:** Review specific requirement (GET - 3 headers)
```
GET /api/deposits/commissioner/requirements/{{requirementId}}
```
- View detailed requirement information

**Step 3:** Authorize requirement (POST - 4 headers)
```
POST /api/deposits/commissioner/requirements/{{requirementId}}/authorize
Body:
{
  "commissionerId": "COM-001"
}
```
- Requirement status changes to "published"
- Banks can now submit quotes

### Workflow 3: Bank Submits Quote

**Step 1:** View published requirements (GET - 3 headers)
```
GET /api/deposits/bank/requirements?status=published
```
- See available requirements to quote

**Step 2:** View specific requirement details (GET - 3 headers)
```
GET /api/deposits/bank/requirements/{{requirementId}}
```
- Get detailed requirement information

**Step 3:** Submit quote (POST - 4 headers)
```
POST /api/deposits/bank/quotes/submit
Body:
{
  "requirementId": "REQ-001",
  "bankId": "BANK-001",
  "interestRate": 7.5,
  "remarks": "Competitive rate",
  "consentDocument": {
    "fileName": "consent.pdf",
    "fileData": "base64_data",
    "fileSize": 123456,
    "uploadedAt": "2025-01-14T12:00:00Z"
  }
}
```

**Step 4:** Verify submission (GET - 3 headers)
```
GET /api/deposits/bank/quotes?bankId={{bankId}}
```
- View your submitted quotes

### Workflow 4: Commissioner Finalizes Deposit

**Step 1:** View requirement with all quotes (GET - 3 headers)
```
GET /api/deposits/commissioner/requirements/{{requirementId}}
```
- See all bank quotes with rankings

**Step 2:** Review all quotes (GET - 3 headers)
```
GET /api/deposits/commissioner/quotes?requirementId={{requirementId}}
```
- Compare all submitted quotes

**Step 3:** Select winning bank and finalize (POST - 4 headers)
```
POST /api/deposits/commissioner/requirements/{{requirementId}}/finalize
Body:
{
  "bankId": "BANK-001"
}
```
- Deposit is finalized with selected bank

---

## ?? Automated Tests

The collection includes **automatic test scripts** that run after each request:

### Tests Run Automatically:
1. ? Status code should be 200 or 201
2. ? Response has required security headers (`X-Request-ID`, `X-Content-Type-Options`)
3. ? Response has rate limit headers (`X-RateLimit-Limit`, `X-RateLimit-Remaining`)
4. ?? Logs rate limit status to console

### View Test Results:
1. Send any request
2. Click on **Test Results** tab
3. See which tests passed/failed
4. Check console for rate limit information

### Run All Tests at Once:
1. Click on collection name "SMKC Deposit Management System"
2. Click **Run** button
3. Select all folders or specific endpoints
4. Click **Run SMKC Deposit Management System**
5. View comprehensive test results

---

## ?? Security Features

### 1. SHA-256 HMAC Authentication ?
- All requests require valid signature
- Signature expires after 5 minutes
- Prevents replay attacks

### 2. IP Whitelisting ?
- Currently disabled (allows all IPs)
- Can be enabled in production
- All IPs are logged for audit

### 3. Rate Limiting ?
- **Limit:** 100 requests per minute per API key
- **Response:** HTTP 429 when exceeded
- **Headers:** Track usage in response headers

### Response Headers You'll See:
```http
X-Request-ID: 550e8400-e29b-41d4-a716-446655440000
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 85
X-RateLimit-Reset: 1705234860
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Cache-Control: no-store, no-cache, must-revalidate
```

---

## ?? Common Errors & Solutions

### Error 401: Unauthorized - Missing API Key
```json
{
  "success": false,
  "message": "Missing API Key",
  "timestamp": "2025-01-14T12:00:00Z"
}
```
**Solution:**
- Verify environment is selected (top right dropdown)
- Check `apiKey` and `secretKey` are set in environment
- Ensure headers are using `{{apiKey}}` variable

### Error 401: Invalid Signature
```json
{
  "success": false,
  "message": "Invalid signature",
  "timestamp": "2025-01-14T12:00:00Z"
}
```
**Solution:**
- Check pre-request script is running (look in console for debug logs)
- Verify `secretKey` matches server configuration
- Ensure request body is valid JSON (for POST requests)
- Check timestamp is not expired
- **Verify correct headers:** GET = 3 headers, POST = 4 headers

### Error 401: Invalid or Expired Timestamp
```json
{
  "success": false,
  "message": "Invalid or expired timestamp",
  "timestamp": "2025-01-14T12:00:00Z"
}
```
**Solution:**
- Timestamp is automatically generated and valid for 5 minutes
- Check your system clock is accurate
- Re-send the request to generate new timestamp

### Error 429: Rate Limit Exceeded
```json
{
  "success": false,
  "message": "Rate limit exceeded",
  "error": "RATE_LIMIT_EXCEEDED",
  "retryAfter": 60,
  "limit": 100,
  "current": 101,
  "resetTime": 1705234860,
  "timestamp": "2025-01-14T12:00:00Z"
}
```
**Solution:**
- Wait for rate limit to reset (check `resetTime` or `retryAfter`)
- Check `X-RateLimit-Reset` header for exact reset time
- Reduce request frequency
- Use different API key if available

### Error 400: Bad Request - Validation Failed
```json
{
  "success": false,
  "message": "Validation error details",
  "timestamp": "2025-01-14T12:00:00Z"
}
```
**Solution:**
- Check request body matches expected format
- Verify all required fields are present
- Ensure data types are correct (numbers, strings, dates)
- Review response message for specific validation errors

### Error 404: Not Found
```json
{
  "success": false,
  "message": "Resource not found",
  "timestamp": "2025-01-14T12:00:00Z"
}
```
**Solution:**
- Verify resource ID exists (requirementId, bankId, etc.)
- Check URL path is correct
- Update environment variables with valid IDs

---

## ?? Debugging

### Enable Debug Logging
The pre-request script includes comprehensive debug logging:

1. Open Postman Console (View ? Show Postman Console or Ctrl+Alt+C)
2. Send a request
3. View debug output:
```
=== Authentication Debug ===
HTTP Method: POST
Request URI: /api/deposits/bank/quotes/submit
Request Body: {"requirementId":"REQ-001",...}
Timestamp: 1705234800
API Key: TEST_API_KEY_12345...
String to Sign: POST/api/deposits/bank/quotes/submit{...}1705234800TEST_API_KEY_12345...
Signature: abc123def456...
========================
```

### Check Authentication Components
Verify in console:
- ? HTTP Method is uppercase (GET, POST, etc.)
- ? Request URI includes full path with query params
- ? Request Body is complete JSON string (or empty for GET)
- ? Timestamp is Unix timestamp (10 digits)
- ? API Key is complete and correct
- ? Signature is base64 encoded

### Header Pattern Verification
```
GET requests should show:
- X-API-Key ?
- X-Timestamp ?
- X-Signature ?
- Total: 3 headers

POST requests should show:
- Content-Type ?
- X-API-Key ?
- X-Timestamp ?
- X-Signature ?
- Total: 4 headers
```

---

## ?? Test API Keys

The following test keys are configured in both collection and environment:

### Test Key 1 (Default)
```
API Key: TEST_API_KEY_12345678901234567890123456789012
Secret:  TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890
```

### Test Key 2
```
API Key: DEV_API_KEY_ABCDE67890FGHIJ12345KLMNO67890
Secret:  DEV_SECRET_KEY_FGHIJ67890KLMNO12345PQRST
```

### Test Key 3
```
API Key: ADMIN_API_KEY_XYZ12345678901234567890ABC456
Secret:  ADMIN_SECRET_KEY_ABC45678901234567890DEF
```

**To switch keys:**
1. Click on environment
2. Update `apiKey` and `secretKey` values
3. Save environment

?? **Remove test keys before production!**

---

## ?? Best Practices

### 1. Use Environment Variables
- Don't hardcode values in requests
- Use `{{variableName}}` for dynamic values
- Update environment when IDs change

### 2. Test in Sequence
- Follow workflow order (Create ? Authorize ? Submit ? Finalize)
- Save IDs from responses to environment variables
- Verify each step before proceeding

### 3. Monitor Rate Limits
- Check `X-RateLimit-Remaining` header
- Slow down when approaching limit
- Plan batch operations accordingly

### 4. Verify Header Patterns
- GET requests = 3 headers (no Content-Type)
- POST requests = 4 headers (with Content-Type)
- This matches the proven Voter API pattern

### 5. Save Important IDs
When creating resources, save their IDs:
1. View response in **Body** tab
2. Copy ID from response
3. Update environment variable
4. Use `{{variableName}}` in subsequent requests

Example:
```javascript
// Add to Tests tab
if (pm.response.code === 200) {
    const responseJson = pm.response.json();
    if (responseJson.data && responseJson.data.requirementId) {
        pm.environment.set("requirementId", responseJson.data.requirementId);
        console.log("Saved requirementId:", responseJson.data.requirementId);
    }
}
```

### 6. Review Logs
- Always check Postman Console for debug info
- Review Test Results after each request
- Look for patterns in failures

---

## ?? Additional Documentation

### Related Files:
1. **DEPOSIT_MANAGER_SECURITY.md** - Complete security documentation
2. **DEPOSIT_MANAGER_SECURITY_CHANGES.md** - Implementation details
3. **DEPOSIT_MANAGER_AUTH_QUICK_REFERENCE.md** - Quick authentication reference
4. **POSTMAN_HEADER_FIX.md** - Header pattern correction details

### API Documentation:
- All endpoints include XML documentation in code
- Descriptions available in Postman request descriptions
- Review controller files for detailed parameter info

---

## ?? Troubleshooting Checklist

Before asking for help, verify:

- [ ] Environment is selected in Postman (top right)
- [ ] `apiKey` and `secretKey` are set in environment
- [ ] Pre-request script is enabled (check collection settings)
- [ ] API server is running and accessible
- [ ] `baseUrl` matches your server URL
- [ ] System clock is accurate (for timestamp validation)
- [ ] Request body is valid JSON (for POST requests)
- [ ] All required fields are present in request
- [ ] Resource IDs exist (requirementId, bankId, etc.)
- [ ] Not exceeding rate limit (check headers)
- [ ] Correct header count: GET=3, POST=4
- [ ] Postman Console shows no errors
- [ ] Test Results show which tests failed

---

## ?? Updates & Maintenance

### When Server URL Changes:
1. Click on environment
2. Update `baseUrl` value
3. Save environment

### When API Keys Rotate:
1. Get new keys from administrator
2. Update `apiKey` and `secretKey` in environment
3. Test with simple GET request first

### When Adding New Requests:
1. Duplicate existing request
2. Update method, URL, and body
3. **Important:** Set correct headers (GET=3, POST=4)
4. Headers and authentication are inherited
5. Test immediately

---

## ?? Support

### For Authentication Issues:
1. Check Postman Console for debug output
2. Verify signature calculation components
3. **Confirm correct header count (GET=3, POST=4)**
4. Test with provided test API keys
5. Review `DEPOSIT_MANAGER_SECURITY.md`

### For API Issues:
1. Check response error message
2. Verify request body format
3. Review endpoint documentation
4. Check server logs

### For Rate Limit Issues:
1. Check `X-RateLimit-*` headers
2. Wait for reset time
3. Reduce request frequency
4. Request limit increase if needed

---

## ? Quick Test Checklist

### Initial Setup (One Time)
- [ ] Import collection
- [ ] Import environment
- [ ] Select environment
- [ ] Verify `baseUrl` is correct
- [ ] Test with "Get All Banks" request (3 headers)

### Before Each Testing Session
- [ ] Ensure API server is running
- [ ] Environment is selected
- [ ] Check rate limit status
- [ ] Clear Postman Console

### Verify Header Patterns
- [ ] GET requests show 3 headers
- [ ] POST requests show 4 headers
- [ ] No Content-Type on GET requests
- [ ] Content-Type present on POST requests

### After Creating Resources
- [ ] Save resource IDs to environment
- [ ] Verify creation with GET request
- [ ] Update dependent requests with new IDs

### Before Reporting Issues
- [ ] Review Postman Console logs
- [ ] Check Test Results tab
- [ ] Verify correct header patterns
- [ ] Try with fresh environment

---

**Collection Version:** 2.1.0  
**Last Updated:** January 2025  
**Authentication:** HMAC-SHA256  
**Framework:** .NET Framework 4.5  
**Header Pattern:** Matches Voter API ?  
**Status:** ? Ready for Testing

## ?? You're All Set!

Start testing by:
1. Select "SMKC Deposit Manager - Development" environment
2. Open "Account Department APIs" folder
3. Send "Get All Banks" request (3 headers - no Content-Type)
4. Review response and rate limit headers
5. Try a POST request (4 headers - with Content-Type)
6. Continue with other endpoints

**Important:** GET = 3 headers, POST = 4 headers (matches Voter API pattern)

Happy Testing! ??
