# Consent Document Download API - Summary

## What Was Created

### 1. New Common Controller
**File:** `Controllers\DepositManager\ConsentDocumentController.cs`

This controller provides centralized consent document download functionality accessible to all roles.

**Endpoints:**
- `GET /api/deposits/consent/download` - Download consent document (binary or JSON)
- `GET /api/deposits/consent/info` - Get document metadata without downloading

**Features:**
- ? Download as binary PDF file (for direct download)
- ? Download as JSON with base64 content (for frontend integration)
- ? Document info endpoint for checking file existence and metadata
- ? Comprehensive error handling
- ? Security logging
- ? SHA-256 authentication
- ? Rate limiting

---

### 2. Enhanced Existing Controllers
Added consent download endpoints to all three role-specific controllers:

**BankController** (`Controllers\DepositManager\BankController.cs`)
- `GET /api/deposits/bank/quotes/{quoteId}/consent`

**AccountController** (`Controllers\DepositManager\AccountController.cs`)
- `GET /api/deposits/account/quotes/{quoteId}/consent`

**CommissionerController** (`Controllers\DepositManager\CommissionerController.cs`)
- `GET /api/deposits/commissioner/quotes/{quoteId}/consent`

**Features:**
- Binary PDF download
- Inline viewing option (inline=true)
- Integrates with existing service layer

---

### 3. Documentation
**File:** `Postman\CONSENT_DOCUMENT_DOWNLOAD_GUIDE.md`

Comprehensive guide covering:
- All API endpoints with examples
- Request/response formats
- Complete workflow examples
- Error handling
- Security & authentication
- Configuration settings
- Postman examples
- Testing checklist

---

## How It Works

### File Storage
Documents are stored on **192.168.40.47** in the following structure:
```
\\192.168.40.47\BankConsents\
??? {requirementId}\
    ??? {bankId}\
        ??? {fileName}
```

### Download Flow

1. **Bank submits quote** with consent document
   - Document uploaded to `\\192.168.40.47\BankConsents\{req}\{bank}\`
   - Unique filename generated and stored in database

2. **Get quote details** to retrieve consent file name
   - Any role can query quotes to get `consentFileName`

3. **Download consent document** using one of:
   - Common API: `/api/deposits/consent/download`
   - Role-specific: `/api/deposits/{role}/quotes/{id}/consent`

---

## Quick Start

### Download Consent Document (Binary PDF)
```http
GET /api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf
Headers:
  Accept: application/pdf
  X-Api-Key: your-key
  X-Timestamp: timestamp
  X-Signature: signature
```

### Download Consent Document (JSON with Base64)
```http
GET /api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf
Headers:
  Accept: application/json
  X-Api-Key: your-key
  X-Timestamp: timestamp
  X-Signature: signature
```

### Get Document Info
```http
GET /api/deposits/consent/info?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf
Headers:
  X-Api-Key: your-key
  X-Timestamp: timestamp
  X-Signature: signature
```

### Role-Specific Download
```http
GET /api/deposits/account/quotes/QT001/consent?requirementId=REQ001&bankId=BANK001
Headers:
  X-Api-Key: your-key
  X-Timestamp: timestamp
  X-Signature: signature
```

---

## Response Formats

### Binary Download (Accept: application/pdf)
- HTTP 200 OK
- Content-Type: application/pdf
- Content-Disposition: attachment; filename="consent.pdf"
- Body: Binary PDF data

### JSON Response (Accept: application/json)
```json
{
  "success": true,
  "message": "Consent document retrieved successfully",
  "data": {
    "fileName": "consent.pdf",
    "fileData": "base64_encoded_content...",
    "contentType": "application/pdf",
    "requirementId": "REQ001",
    "bankId": "BANK001",
    "downloadedAt": "2025-02-03T10:30:00Z"
  }
}
```

### Document Info Response
```json
{
  "success": true,
  "message": "Consent document information retrieved successfully",
  "data": {
    "fileName": "consent.pdf",
    "fileSize": 245678,
    "contentType": "application/pdf",
    "requirementId": "REQ001",
    "bankId": "BANK001",
    "exists": true,
    "downloadUrl": "/api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf"
  }
}
```

---

## Error Responses

### Missing Parameters (400)
```json
{
  "success": false,
  "message": "requirementId, bankId, and fileName are required",
  "error": "INVALID_PARAMETER"
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
    "fileName": "consent.pdf"
  }
}
```

---

## File Server Access

The system uses `NetworkStorageService` which accesses the file server at **192.168.40.47**.

**Network Share Path:** `\\192.168.40.47\BankConsents`

**Configuration (Web.config):**
```xml
<add key="Network_Server" value="192.168.40.47" />
<add key="Network_Share" value="BankConsents" />
<add key="Ftp_User" value="username" />
<add key="Ftp_Password" value="password" />
```

**See `ADMIN_SHARE_FIX.md` for detailed setup instructions.**

---

## Security

All endpoints include:
- ? SHA-256 HMAC authentication
- ? Rate limiting (100 req/min)
- ? Request ID tracking
- ? Comprehensive logging
- ? Secure error messages

---

## Testing

### Build Status
? **Build Successful** - All controllers compile without errors

### Test Endpoints
1. Upload consent via quote submission
2. Get quote details to retrieve fileName
3. Test common download API (binary)
4. Test common download API (JSON)
5. Test document info endpoint
6. Test role-specific endpoints
7. Verify error handling

---

## Next Steps

1. **Configure File Server**
   - Follow `ADMIN_SHARE_FIX.md` to set up network share
   - Ensure credentials in Web.config are correct

2. **Test Endpoints**
   - Use Postman collection
   - Test all download variations
   - Verify file access and security

3. **Monitor Logs**
   - Check `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`
   - Monitor application logs for errors

4. **Update Frontend**
   - Integrate download endpoints
   - Display PDF files in browser
   - Handle errors gracefully

---

## Files Modified/Created

### Created
- ? `Controllers\DepositManager\ConsentDocumentController.cs`
- ? `Postman\CONSENT_DOCUMENT_DOWNLOAD_GUIDE.md`
- ? `Postman\CONSENT_DOCUMENT_DOWNLOAD_SUMMARY.md` (this file)

### Modified
- ? `Controllers\DepositManager\BankController.cs`
- ? `Controllers\DepositManager\AccountController.cs`
- ? `Controllers\DepositManager\CommissionerController.cs`

---

## Support

**Documentation:**
- Main Guide: `CONSENT_DOCUMENT_DOWNLOAD_GUIDE.md`
- File Server Setup: `ADMIN_SHARE_FIX.md`
- API Guide: `DEPOSIT_MANAGER_POSTMAN_GUIDE.md`

**Logs:**
- Application: `C:\smkcapi_published\Logs\`
- FTP/Network: `FtpLog_YYYYMMDD.txt`

**Common Issues:**
- Network access: See `ADMIN_SHARE_FIX.md`
- Authentication: Check SHA-256 signature
- Rate limits: Monitor X-RateLimit-* headers

---

## Summary

? **Common API created** for downloading bank consent documents  
? **Role-specific endpoints** added to all controllers  
? **Multiple download formats** supported (binary PDF and JSON base64)  
? **Comprehensive documentation** provided  
? **Build successful** - All code compiles without errors  
? **File server integration** using existing NetworkStorageService  
? **Security implemented** with SHA-256 authentication and rate limiting  

**The API is ready to use!** ??
