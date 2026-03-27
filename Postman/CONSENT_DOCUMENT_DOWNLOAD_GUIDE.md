# Bank Consent Document Download API Guide

## ?? IMPORTANT - RECENT FIX APPLIED

**Issue:** The API was returning HTTP 500 errors when downloading consent documents.

**Root Causes:**
1. ? `ConsentDocumentController` was not registered in dependency injection
2. ? Network path construction was incorrect for admin shares with subdirectories

**Status:** ? **FIXED** - See `FIX_SUMMARY.md` for details

**What was fixed:**
- ? Controller properly registered in `SimpleDependencyResolver`
- ? Network path construction improved in `NetworkStorageService`
- ? Build successful - all changes compile

**Next Steps:**
1. Deploy updated DLLs to production
2. Restart IIS application pool
3. Test endpoints (should now return 200/404 instead of 500)
4. Optional: Update Web.config to use regular share (see `CONFIG_FIX_INSTRUCTIONS.md`)

---

## Overview
This guide explains how to download bank consent documents from the Deposit Manager system using the newly created APIs.

## File Server Configuration
- **Server**: 192.168.40.47
- **Storage Type**: Network Share (recommended) or FTP
- **Base Path**: `\\192.168.40.47\BankConsents` or `/BankConsents`
- **File Structure**: `/{requirementId}/{bankId}/{fileName}`

---

## API Endpoints

### 1. Common Consent Download API (Recommended)
**For all roles - Account, Bank, Commissioner**

#### Download Consent Document (Binary)
```http
GET /api/deposits/consent/download?requirementId={req}&bankId={bank}&fileName={file}
Headers:
  - X-Api-Key: your-api-key
  - X-Timestamp: current-timestamp
  - X-Signature: sha256-signature
  - Accept: application/pdf
```

**Parameters:**
- `requirementId` (required): Requirement identifier (e.g., REQ0000000001)
- `bankId` (required): Bank identifier
- `fileName` (required): Consent file name from quote data
- `quoteId` (optional): Quote identifier (for future enhancement)

**Response:**
- **Success (200)**: Binary PDF file download
- **Not Found (404)**: File doesn't exist
- **Bad Request (400)**: Missing parameters

**Example:**
```bash
GET /api/deposits/consent/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_123.pdf
```

---

#### Download Consent Document (JSON with Base64)
```http
GET /api/deposits/consent/download?requirementId={req}&bankId={bank}&fileName={file}
Headers:
  - X-Api-Key: your-api-key
  - X-Timestamp: current-timestamp
  - X-Signature: sha256-signature
  - Accept: application/json
```

**Response:**
```json
{
  "success": true,
  "message": "Consent document retrieved successfully",
  "data": {
    "fileName": "consent_123.pdf",
    "fileData": "base64_encoded_content...",
    "contentType": "application/pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "downloadedAt": "2025-02-03T10:30:00Z"
  }
}
```

---

#### Get Consent Document Info
```http
GET /api/deposits/consent/info?requirementId={req}&bankId={bank}&fileName={file}
Headers:
  - X-Api-Key: your-api-key
  - X-Timestamp: current-timestamp
  - X-Signature: sha256-signature
```

**Response:**
```json
{
  "success": true,
  "message": "Consent document information retrieved successfully",
  "data": {
    "fileName": "consent_123.pdf",
    "fileSize": 245678,
    "contentType": "application/pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "exists": true,
    "downloadUrl": "/api/deposits/consent/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_123.pdf"
  }
}
```

---

### 2. Role-Specific Endpoints

#### Bank Controller - Download Own Quote's Consent
```http
GET /api/deposits/bank/quotes/{quoteId}/consent?requirementId={req}&bankId={bank}
Headers:
  - X-Api-Key: your-api-key
  - X-Timestamp: current-timestamp
  - X-Signature: sha256-signature
```

**Parameters:**
- `quoteId` (path): Quote identifier
- `requirementId` (query): Requirement identifier
- `bankId` (query): Bank identifier
- `inline` (optional): true = view in browser, false = download (default)

**Example:**
```bash
GET /api/deposits/bank/quotes/QT001/consent?requirementId=REQ0000000001&bankId=BANK001
```

---

#### Account Controller - Download Any Quote's Consent
```http
GET /api/deposits/account/quotes/{quoteId}/consent?requirementId={req}&bankId={bank}
Headers:
  - X-Api-Key: your-api-key
  - X-Timestamp: current-timestamp
  - X-Signature: sha256-signature
```

**Parameters:**
- `quoteId` (path): Quote identifier
- `requirementId` (query): Requirement identifier
- `bankId` (query): Bank identifier
- `inline` (optional): true = view in browser, false = download (default)

**Example:**
```bash
GET /api/deposits/account/quotes/QT001/consent?requirementId=REQ0000000001&bankId=BANK001&inline=true
```

---

#### Commissioner Controller - Download Quote's Consent for Review
```http
GET /api/deposits/commissioner/quotes/{quoteId}/consent?requirementId={req}&bankId={bank}
Headers:
  - X-Api-Key: your-api-key
  - X-Timestamp: current-timestamp
  - X-Signature: sha256-signature
```

**Parameters:**
- `quoteId` (path): Quote identifier
- `requirementId` (query): Requirement identifier
- `bankId` (query): Bank identifier
- `inline` (optional): true = view in browser, false = download (default)

**Example:**
```bash
GET /api/deposits/commissioner/quotes/QT001/consent?requirementId=REQ0000000001&bankId=BANK001
```

---

## Complete Workflow Example

### Step 1: Bank Submits Quote with Consent Document
```http
POST /api/deposits/bank/quotes/submit
Body:
{
  "requirementId": "REQ0000000001",
  "bankId": "BANK001",
  "interestRate": 7.5,
  "remarks": "Competitive rate",
  "consentDocument": {
    "fileName": "bank_consent.pdf",
    "fileData": "base64_encoded_pdf_content...",
    "fileSize": 245678,
    "uploadedAt": "2025-02-03T10:00:00Z"
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Quote submitted successfully",
  "data": {
    "quoteId": "QT001",
    "consentFileName": "abc123_bank_consent.pdf"
  }
}
```

---

### Step 2: Account Department Reviews Quote
```http
GET /api/deposits/account/quotes?requirementId=REQ0000000001
```

**Response includes consent file name:**
```json
{
  "success": true,
  "data": {
    "quotes": [
      {
        "id": "QT001",
        "bankId": "BANK001",
        "interestRate": 7.5,
        "consentFileName": "abc123_bank_consent.pdf"
      }
    ]
  }
}
```

---

### Step 3: Download Consent Document (Multiple Options)

#### Option A: Using Common API
```http
GET /api/deposits/consent/download?requirementId=REQ0000000001&bankId=BANK001&fileName=abc123_bank_consent.pdf
Accept: application/pdf
```
? Downloads PDF file directly

---

#### Option B: Using Account-Specific API
```http
GET /api/deposits/account/quotes/QT001/consent?requirementId=REQ0000000001&bankId=BANK001&inline=true
```
? Opens PDF in browser for viewing

---

#### Option C: Get Base64 Content for Frontend Display
```http
GET /api/deposits/consent/download?requirementId=REQ0000000001&bankId=BANK001&fileName=abc123_bank_consent.pdf
Accept: application/json
```

**Response:**
```json
{
  "success": true,
  "data": {
    "fileName": "abc123_bank_consent.pdf",
    "fileData": "base64_content...",
    "contentType": "application/pdf"
  }
}
```

Then in frontend:
```javascript
// Display PDF in browser
const pdfData = response.data.fileData;
const pdfBlob = base64toBlob(pdfData, 'application/pdf');
const pdfUrl = URL.createObjectURL(pdfBlob);
window.open(pdfUrl, '_blank');
```

---

## Error Handling

### Missing Parameters
```json
{
  "success": false,
  "message": "requirementId, bankId, and fileName are required",
  "error": "INVALID_PARAMETER"
}
```

### File Not Found
```json
{
  "success": false,
  "message": "Consent document not found on storage server",
  "error": "FILE_NOT_FOUND",
  "data": {
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "abc123_bank_consent.pdf"
  }
}
```

### Storage Server Error
```json
{
  "success": false,
  "message": "An error occurred while downloading the consent document",
  "error": "Network path not accessible: \\\\192.168.40.47\\BankConsents"
}
```

---

## Security & Authentication

All endpoints require:
1. **SHA-256 HMAC Authentication** (4 headers)
   - X-Api-Key
   - X-Timestamp
   - X-Signature
   - Content-Type (for POST requests)

2. **Rate Limiting**
   - 100 requests per minute per API key
   - Exceeding limit returns HTTP 429

3. **IP Whitelisting** (if enabled)
   - Currently disabled in development
   - Enable in production via Web.config

---

## File Storage Paths

### Network Share Structure
```
\\192.168.40.47\BankConsents\
??? REQ0000000001\
?   ??? BANK001\
?   ?   ??? abc123_bank_consent.pdf
?   ??? BANK002\
?       ??? def456_bank_consent.pdf
??? REQ0000000002\
    ??? BANK001\
        ??? ghi789_bank_consent.pdf
```

### FTP Structure
```
/BankConsents/
??? REQ0000000001/
?   ??? BANK001/
?   ?   ??? abc123_bank_consent.pdf
?   ??? BANK002/
?       ??? def456_bank_consent.pdf
??? REQ0000000002/
    ??? BANK001/
        ??? ghi789_bank_consent.pdf
```

---

## Configuration (Web.config)

### For Network Share (Recommended)
```xml
<appSettings>
  <!-- Use local path if API runs on same server -->
  <add key="Network_LocalPath" value="C:\inetpub\ftproot\BankConsents" />
  
  <!-- OR use network share if API is on different server -->
  <add key="Network_Server" value="192.168.40.47" />
  <add key="Network_Share" value="BankConsents" />
  <add key="Network_BasePath" value="BankConsents" />
  
  <!-- Credentials (reused from FTP settings) -->
  <add key="Ftp_User" value="your_username" />
  <add key="Ftp_Password" value="your_password" />
</appSettings>
```

### For FTP
```xml
<appSettings>
  <add key="Ftp_Host" value="192.168.40.47" />
  <add key="Ftp_Port" value="21" />
  <add key="Ftp_User" value="your_username" />
  <add key="Ftp_Password" value="your_password" />
  <add key="Ftp_BasePath" value="/BankConsents" />
</appSettings>
```

---

## Postman Examples

### Collection Variables
```javascript
{
  "requirementId": "REQ0000000001",
  "quoteId": "QT001",
  "bankId": "BANK001",
  "consentFileName": "abc123_bank_consent.pdf"
}
```

### Request Example - Download Binary
```
GET {{baseUrl}}/api/deposits/consent/download
  ?requirementId={{requirementId}}
  &bankId={{bankId}}
  &fileName={{consentFileName}}

Headers:
  X-Api-Key: {{apiKey}}
  X-Timestamp: {{$timestamp}}
  X-Signature: {{signature}}
  Accept: application/pdf
```

### Request Example - Get JSON with Base64
```
GET {{baseUrl}}/api/deposits/consent/download
  ?requirementId={{requirementId}}
  &bankId={{bankId}}
  &fileName={{consentFileName}}

Headers:
  X-Api-Key: {{apiKey}}
  X-Timestamp: {{$timestamp}}
  X-Signature: {{signature}}
  Accept: application/json
```

### Request Example - Role-Specific Download
```
GET {{baseUrl}}/api/deposits/account/quotes/{{quoteId}}/consent
  ?requirementId={{requirementId}}
  &bankId={{bankId}}
  &inline=true

Headers:
  X-Api-Key: {{apiKey}}
  X-Timestamp: {{$timestamp}}
  X-Signature: {{signature}}
```

---

## Testing Checklist

- [ ] Upload consent document via quote submission
- [ ] Verify file exists on storage server (192.168.40.47)
- [ ] Download using common API with binary response
- [ ] Download using common API with JSON response
- [ ] Get document info endpoint
- [ ] Download using bank-specific endpoint
- [ ] Download using account-specific endpoint
- [ ] Download using commissioner-specific endpoint
- [ ] Test with inline=true parameter
- [ ] Test error handling (missing parameters)
- [ ] Test error handling (file not found)
- [ ] Verify rate limiting headers
- [ ] Verify security authentication

---

## Support

**Logs Location:**
- Application logs: `C:\smkcapi_published\Logs\`
- FTP/Network logs: `FtpLog_YYYYMMDD.txt`

**Common Issues:**
1. **"Network path not accessible"** ? Check ADMIN_SHARE_FIX.md
2. **"File not found"** ? Verify fileName from quote data
3. **"Access denied"** ? Check credentials in Web.config
4. **Rate limit exceeded** ? Wait 1 minute or increase limit

**Need Help?**
Check the comprehensive guide: `ADMIN_SHARE_FIX.md`
