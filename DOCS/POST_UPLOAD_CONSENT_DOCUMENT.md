# Upload Consent Document API - POST Endpoint

## ? Summary

A new **POST endpoint** has been created for uploading consent documents with a structured request object containing all necessary parameters.

---

## ?? Endpoint Details

### Route
```
POST /api/deposits/consent/documentconsent
```

### Authentication
? **No authentication required** - Plain access (same as download endpoints)

### Content-Type
```
Content-Type: application/json
```

---

## ?? Request Model

### UploadConsentDocumentRequest

| Property | Type | Required | Description | Example |
|----------|------|----------|-------------|---------|
| `requirementId` | string | ? Yes | Requirement identifier | `"REQ0000000001"` |
| `bankId` | string | ? Yes | Bank identifier | `"BNK00011"` |
| `quoteId` | string | ?? Optional | Quote identifier (for reference) | `"QT001"` |
| `fileName` | string | ? Yes | File name (must be .pdf) | `"consent_document.pdf"` |
| `fileData` | string | ? Yes | Base64 encoded file content | `"JVBERi0xLjQ..."` |
| `fileSize` | long? | ?? Optional | File size in bytes | `245678` |
| `contentType` | string | ?? Optional | Content type (default: application/pdf) | `"application/pdf"` |
| `uploadedBy` | string | ?? Optional | User identifier | `"user123"` |
| `remarks` | string | ?? Optional | Additional notes | `"Initial submission"` |

---

## ?? Request Example

### JSON Body
```json
{
  "requirementId": "REQ0000000024",
  "bankId": "BNK00011",
  "quoteId": "QT12345",
  "fileName": "bank_consent.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MKMSAwIG9iaiA8PAovVHlwZSAvQ2F0YW...",
  "fileSize": 245678,
  "contentType": "application/pdf",
  "uploadedBy": "user@bank.com",
  "remarks": "Consent document for deposit requirement"
}
```

### With Data URI Format
```json
{
  "requirementId": "REQ0000000024",
  "bankId": "BNK00011",
  "fileName": "consent.pdf",
  "fileData": "data:application/pdf;base64,JVBERi0xLjQKJeLjz9MK..."
}
```

---

## ? Success Response (201 Created)

```json
{
  "success": true,
  "message": "Consent document uploaded successfully",
  "data": {
    "fileName": "abc123_bank_consent.pdf",
    "originalFileName": "bank_consent.pdf",
    "requirementId": "REQ0000000024",
    "bankId": "BNK00011",
    "quoteId": "QT12345",
    "fileSize": 245678,
    "contentType": "application/pdf",
    "uploadedAt": "2024-02-04T12:30:00Z",
    "uploadedBy": "user@bank.com",
    "downloadUrl": "/api/deposits/consent/downloadconsent?requirementId=REQ0000000024&bankId=BNK00011&fileName=abc123_bank_consent.pdf"
  }
}
```

---

## ? Error Responses

### 400 Bad Request - Missing Required Field
```json
{
  "success": false,
  "message": "requirementId is required",
  "error": "INVALID_PARAMETER",
  "errorCode": "REQ_ID_REQUIRED"
}
```

### 400 Bad Request - Invalid File Type
```json
{
  "success": false,
  "message": "Only PDF files are allowed",
  "error": "INVALID_FILE_TYPE",
  "errorCode": "PDF_ONLY"
}
```

### 400 Bad Request - File Too Large
```json
{
  "success": false,
  "message": "fileSize must not exceed 5 MB",
  "error": "FILE_TOO_LARGE",
  "errorCode": "MAX_SIZE_EXCEEDED"
}
```

### 400 Bad Request - Invalid Base64
```json
{
  "success": false,
  "message": "fileData must be valid base64 encoded string",
  "error": "INVALID_BASE64",
  "errorCode": "INVALID_ENCODING"
}
```

### 400 Bad Request - Null Request Body
```json
{
  "success": false,
  "message": "Request body is required",
  "error": "INVALID_REQUEST",
  "errorCode": "NULL_REQUEST"
}
```

### 500 Internal Server Error - Upload Failed
```json
{
  "success": false,
  "message": "Failed to upload consent document to storage server",
  "error": "UPLOAD_FAILED",
  "errorCode": "STORAGE_ERROR"
}
```

### 500 Internal Server Error - Server Error
```json
{
  "success": false,
  "message": "An error occurred while uploading the consent document",
  "error": "Detailed error message...",
  "errorCode": "SERVER_ERROR",
  "data": {
    "hint": "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
    "requirementId": "REQ0000000024",
    "bankId": "BNK00011",
    "fileName": "consent.pdf",
    "requestId": "abc-123-def-456"
  }
}
```

---

## ?? Postman Setup

### Request Configuration

**Method:** `POST`

**URL:**
```
http://localhost:57031/api/deposits/consent/documentconsent
```

**Headers:**
```
Content-Type: application/json
```

**Body (raw JSON):**
```json
{
  "requirementId": "REQ0000000024",
  "bankId": "BNK00011",
  "fileName": "test_consent.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MKMSAwIG9iaiA8PAovVHlwZSAvQ2F0YW..."
}
```

### How to Get Base64 File Data

#### Option 1: Using Online Tool
1. Go to https://base64.guru/converter/encode/pdf
2. Upload your PDF file
3. Copy the base64 string

#### Option 2: Using PowerShell
```powershell
$bytes = [System.IO.File]::ReadAllBytes("C:\path\to\consent.pdf")
$base64 = [System.Convert]::ToBase64String($bytes)
Write-Output $base64
```

#### Option 3: Using JavaScript/Node.js
```javascript
const fs = require('fs');
const fileBuffer = fs.readFileSync('consent.pdf');
const base64 = fileBuffer.toString('base64');
console.log(base64);
```

#### Option 4: Using Python
```python
import base64
with open('consent.pdf', 'rb') as f:
    base64_data = base64.b64encode(f.read()).decode('utf-8')
    print(base64_data)
```

---

## ?? Validation Rules

### Automatic Validation (Server-Side)

1. **Required Fields**
   - `requirementId` - Must not be null/empty
   - `bankId` - Must not be null/empty
   - `fileName` - Must not be null/empty
   - `fileData` - Must not be null/empty

2. **File Type**
   - Only `.pdf` files are allowed
   - Extension check is case-insensitive

3. **File Size**
   - Maximum: 5 MB (5,242,880 bytes)
   - Validated after base64 decoding

4. **Base64 Format**
   - Must be valid base64 string
   - Supports data URI format: `data:application/pdf;base64,...`
   - Automatically strips data URI prefix if present

5. **Empty File Check**
   - File must contain data after decoding
   - Zero-byte files are rejected

---

## ?? File Storage

### Storage Location
```
\\192.168.40.47\c$\inetpub\ftproot\BankConsents\{requirementId}\{bankId}\{fileName}
```

### Example
For `requirementId=REQ0000000024` and `bankId=BNK00011`:
```
\\192.168.40.47\c$\inetpub\ftproot\BankConsents\REQ0000000024\BNK00011\abc123_consent.pdf
```

### File Naming
- The storage service may generate a unique filename
- Original filename is preserved in the response
- Use the returned `fileName` for subsequent downloads

---

## ?? Complete Workflow Example

### Step 1: Upload Consent Document
```http
POST /api/deposits/consent/documentconsent
Content-Type: application/json

{
  "requirementId": "REQ0000000024",
  "bankId": "BNK00011",
  "fileName": "consent.pdf",
  "fileData": "JVBERi0xLjQKJe..."
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "fileName": "abc123_consent.pdf",
    "downloadUrl": "/api/deposits/consent/downloadconsent?requirementId=REQ0000000024&bankId=BNK00011&fileName=abc123_consent.pdf"
  }
}
```

### Step 2: Verify Upload
```http
GET /api/deposits/consent/info?requirementId=REQ0000000024&bankId=BNK00011&fileName=abc123_consent.pdf
```

**Response:**
```json
{
  "success": true,
  "data": {
    "fileName": "abc123_consent.pdf",
    "fileSize": 245678,
    "exists": true
  }
}
```

### Step 3: Download Document
```http
GET /api/deposits/consent/downloadconsent?requirementId=REQ0000000024&bankId=BNK00011&fileName=abc123_consent.pdf
```

**Response:** Binary PDF file downloads

---

## ?? Security Features

### No Authentication Required
- Same as GET endpoints - plain access enabled
- Accessible without API key or signatures

### Comprehensive Logging
Every upload request logs:
- ? Request ID (unique identifier)
- ? Client IP address
- ? All request parameters
- ? File size and metadata
- ? Validation results
- ? Upload success/failure
- ? Full exception details (if errors occur)

### Error Handling
- Specific error codes for different scenarios
- Detailed validation messages
- Safe error responses (no sensitive data exposed)
- Server errors include log file location

---

## ?? Testing Checklist

- [ ] Upload valid PDF file
- [ ] Upload with data URI format
- [ ] Upload without optional fields (quoteId, fileSize, etc.)
- [ ] Test file size validation (>5MB)
- [ ] Test invalid file type (.docx, .jpg, etc.)
- [ ] Test invalid base64 string
- [ ] Test missing required fields
- [ ] Test null request body
- [ ] Verify file exists on storage server
- [ ] Download uploaded file
- [ ] Check log file for detailed logs

---

## ?? Quick Start Commands

### curl Example
```bash
curl -X POST http://localhost:57031/api/deposits/consent/documentconsent \
  -H "Content-Type: application/json" \
  -d '{
    "requirementId": "REQ0000000024",
    "bankId": "BNK00011",
    "fileName": "consent.pdf",
    "fileData": "JVBERi0xLjQK..."
  }'
```

### PowerShell Example
```powershell
$body = @{
    requirementId = "REQ0000000024"
    bankId = "BNK00011"
    fileName = "consent.pdf"
    fileData = "JVBERi0xLjQK..."
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:57031/api/deposits/consent/documentconsent" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

### JavaScript/Fetch Example
```javascript
fetch('http://localhost:57031/api/deposits/consent/documentconsent', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    requirementId: 'REQ0000000024',
    bankId: 'BNK00011',
    fileName: 'consent.pdf',
    fileData: 'JVBERi0xLjQK...'
  })
})
.then(response => response.json())
.then(data => console.log(data));
```

---

## ?? All Available Endpoints

| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/api/deposits/consent/health` | Health check | No |
| GET | `/api/deposits/consent/downloadconsent` | Download document | No |
| GET | `/api/deposits/consent/info` | Document info | No |
| **POST** | `/api/deposits/consent/documentconsent` | **Upload document** | **No** |

---

## ?? Files Created/Modified

### New Files
- ? `Models\DepositManager\UploadConsentDocumentRequest.cs` - Request model
- ? `Docs\POST_UPLOAD_CONSENT_DOCUMENT.md` - This documentation

### Modified Files
- ? `Controllers\DepositManager\ConsentDocumentController.cs` - Added POST method
- ? `Models\DepositManager\Requests.cs` - Added ContentType to ConsentDocumentDto
- ? `Security\ApiKeyAuthenticationHandler.cs` - Added /documentconsent to public endpoints

---

## ?? Ready to Use!

The POST endpoint is now fully implemented with:
- ? Structured request object
- ? Comprehensive validation
- ? Detailed error messages
- ? File size limits (5 MB)
- ? Base64 format support (including data URI)
- ? Comprehensive logging
- ? No authentication required
- ? Build successful

**Restart the application and test in Postman!**

```
Press Shift+F5 to stop, then F5 to start debugging
```
