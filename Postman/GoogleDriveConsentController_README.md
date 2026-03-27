# Google Drive Consent Controller - Postman Collection

Complete API testing collection for the Google Drive Consent Controller endpoints.

## ?? Overview

This Postman collection provides ready-to-use API requests for testing all endpoints of the `GoogleDriveConsentController`, which manages bank consent documents using Google Drive storage.

## ?? Quick Start

### 1. Import Collection into Postman

**Option A: Import from File**
1. Open Postman
2. Click **Import** button (top left)
3. Select **File** tab
4. Browse to: `Postman\GoogleDriveConsentController.postman_collection.json`
5. Click **Import**

**Option B: Import from Raw JSON**
1. Open Postman
2. Click **Import** button
3. Select **Raw text** tab
4. Copy the entire content of `GoogleDriveConsentController.postman_collection.json`
5. Paste and click **Continue** ? **Import**

### 2. Configure Environment Variables

The collection uses a variable for the base URL:

1. In Postman, click the **Environment quick look** icon (eye icon, top right)
2. Click **Edit** next to **Globals** or create a new environment
3. Add/Update the variable:
   - **Variable**: `base_url`
   - **Initial Value**: `http://localhost:5000` (or your server URL)
   - **Current Value**: `http://localhost:5000` (or your server URL)

**Example Configurations:**
- Local Development: `http://localhost:5000`
- Local IIS: `http://localhost/apismkc`
- Staging Server: `http://your-server-ip:port`
- Production: `https://api.yourdomain.com`

## ?? Available Endpoints

### 1. Health Check
**Purpose**: Verify the controller is accessible and operational

- **Method**: `GET`
- **Endpoint**: `/api/deposits/consent/googledrive/health`
- **Authentication**: None
- **Use Case**: System health monitoring, connectivity testing

**Sample Response:**
```json
{
  "success": true,
  "message": "GoogleDriveConsentController is accessible",
  "timestamp": "2024-01-20T10:30:00.000Z",
  "storageType": "Google Drive",
  "storageLocation": "DepositManager/BankConsent",
  "authenticationRequired": false
}
```

### 2. Upload Consent Document
**Purpose**: Upload a new bank consent document to Google Drive

- **Method**: `POST`
- **Endpoint**: `/api/deposits/consent/googledrive/upload`
- **Authentication**: None
- **Content-Type**: `application/json`

**Request Body Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `requirementId` | string | ? Yes | Requirement identifier (e.g., REQ0000000001) |
| `bankId` | string | ? Yes | Bank identifier |
| `fileName` | string | ? Yes | File name with extension |
| `fileData` | string | ? Yes | Base64 encoded file content |
| `quoteId` | string | ? No | Quote identifier |
| `fileSize` | number | ? No | File size in bytes |
| `contentType` | string | ? No | MIME type (default: application/pdf) |
| `uploadedBy` | string | ? No | Uploader email/identifier |

**Sample Request:**
```json
{
  "requirementId": "REQ0000000001",
  "bankId": "BANK001",
  "quoteId": "QUOTE001",
  "fileName": "consent_document.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MK...",
  "fileSize": 1024,
  "contentType": "application/pdf",
  "uploadedBy": "admin@bank.com"
}
```

**Sample Response (201 Created):**
```json
{
  "success": true,
  "message": "Consent document uploaded successfully to Google Drive",
  "data": {
    "fileName": "consent_document_20240120103045.pdf",
    "originalFileName": "consent_document.pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "quoteId": "QUOTE001",
    "fileSize": 1024,
    "contentType": "application/pdf",
    "uploadedAt": "2024-01-20T10:30:45.123Z",
    "uploadedBy": "admin@bank.com",
    "storagePath": "DepositManager/BankConsent/REQ0000000001/BANK001/consent_document_20240120103045.pdf",
    "downloadUrl": "/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document_20240120103045.pdf"
  }
}
```

### 3. Download Consent Document (Binary)
**Purpose**: Download the consent document as a binary PDF file

- **Method**: `GET`
- **Endpoint**: `/api/deposits/consent/googledrive/download`
- **Authentication**: None
- **Accept Header**: `application/pdf`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `requirementId` | string | ? Yes | Requirement identifier |
| `bankId` | string | ? Yes | Bank identifier |
| `fileName` | string | ? Yes | File name to download |

**Sample Request:**
```
GET /api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document_20240120103045.pdf
Headers:
  Accept: application/pdf
```

**Response:**
- **Content-Type**: `application/pdf`
- **Content-Disposition**: `attachment; filename="consent_document_20240120103045.pdf"`
- **Body**: Binary PDF content

### 4. Download Consent Document (JSON)
**Purpose**: Get the consent document as base64 encoded data in JSON format

- **Method**: `GET`
- **Endpoint**: `/api/deposits/consent/googledrive/download`
- **Authentication**: None
- **Accept Header**: `application/json`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `requirementId` | string | ? Yes | Requirement identifier |
| `bankId` | string | ? Yes | Bank identifier |
| `fileName` | string | ? Yes | File name to download |

**Sample Response:**
```json
{
  "success": true,
  "message": "Consent document retrieved successfully from Google Drive",
  "data": {
    "fileName": "consent_document_20240120103045.pdf",
    "fileData": "JVBERi0xLjQKJeLjz9MK...",
    "contentType": "application/pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "downloadedAt": "2024-01-20T10:35:00.000Z",
    "storageLocation": "Google Drive"
  }
}
```

### 5. Get Document Info
**Purpose**: Get metadata about a document without downloading it

- **Method**: `GET`
- **Endpoint**: `/api/deposits/consent/googledrive/info`
- **Authentication**: None

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `requirementId` | string | ? Yes | Requirement identifier |
| `bankId` | string | ? Yes | Bank identifier |
| `fileName` | string | ? Yes | File name |

**Sample Response:**
```json
{
  "success": true,
  "message": "Consent document information retrieved successfully from Google Drive",
  "data": {
    "fileName": "consent_document_20240120103045.pdf",
    "fileSize": 1024,
    "contentType": "application/pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "exists": true,
    "storageLocation": "Google Drive",
    "storagePath": "DepositManager/BankConsent/REQ0000000001/BANK001/consent_document_20240120103045.pdf",
    "downloadUrl": "/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document_20240120103045.pdf"
  }
}
```

## ?? Testing Workflow

### Complete Test Scenario

Follow this workflow to test all functionalities:

#### Step 1: Health Check
1. Select **"Health Check"** request
2. Click **Send**
3. Verify response status is `200 OK`
4. Confirm controller is accessible

#### Step 2: Upload a Document
1. Select **"Upload Consent Document"** request
2. Update the request body with your test data:
   - `requirementId`: Use a test requirement ID
   - `bankId`: Use a test bank ID
   - `fileName`: Your desired file name
   - `fileData`: Base64 encoded PDF content
3. Click **Send**
4. Verify response status is `201 Created`
5. **IMPORTANT**: Copy the `fileName` from the response (it will have a timestamp)

#### Step 3: Get Document Info
1. Select **"Get Document Info"** request
2. Update query parameters:
   - `requirementId`: Same as used in upload
   - `bankId`: Same as used in upload
   - `fileName`: Use the filename from upload response (with timestamp)
3. Click **Send**
4. Verify response shows correct file metadata

#### Step 4: Download as Binary
1. Select **"Download Consent Document (Binary)"** request
2. Update query parameters (same as Step 3)
3. Ensure header `Accept: application/pdf` is set
4. Click **Send**
5. In Postman, click **"Save Response"** ? **"Save to a file"**
6. Open the saved PDF to verify content

#### Step 5: Download as JSON
1. Select **"Download Consent Document (JSON)"** request
2. Update query parameters (same as Step 3)
3. Ensure header `Accept: application/json` is set
4. Click **Send**
5. Verify response contains base64 data in `fileData` field

## ??? Generating Base64 File Data

To test file uploads, you need to convert your PDF to base64. Here are several methods:

### Method 1: Using PowerShell (Windows)
```powershell
# Convert PDF to Base64
$fileContent = [System.IO.File]::ReadAllBytes("C:\path\to\your\document.pdf")
$base64String = [System.Convert]::ToBase64String($fileContent)
$base64String | Out-File "base64_output.txt"
```

### Method 2: Using Online Tools
1. Visit: https://base64.guru/converter/encode/pdf
2. Upload your PDF file
3. Copy the base64 output
4. Paste into the `fileData` field in Postman

### Method 3: Using Node.js
```javascript
const fs = require('fs');
const fileBuffer = fs.readFileSync('document.pdf');
const base64String = fileBuffer.toString('base64');
console.log(base64String);
```

### Method 4: Sample Minimal PDF (for testing)
Use this minimal valid PDF in base64 format:
```
JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PC9UeXBlL1BhZ2UvUGFyZW50IDIgMCBSL1Jlc291cmNlczw8L0ZvbnQ8PC9GMSA0IDAgUj4+Pj4vTWVkaWFCb3hbMCAwIDYxMiA3OTJdL0NvbnRlbnRzIDUgMCBSPj4KZW5kb2JqCjQgMCBvYmoKPDwvVHlwZS9Gb250L1N1YnR5cGUvVHlwZTEvQmFzZUZvbnQvVGltZXMtUm9tYW4+PgplbmRvYmoKNSAwIG9iago8PC9MZW5ndGggNDQ+PnN0cmVhbQpCVAovRjEgMTggVGYKMTAwIDcwMCBUZAooVGVzdCBDb25zZW50IERvY3VtZW50KSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCjIgMCBvYmoKPDwvVHlwZS9QYWdlcy9Db3VudCAxL0tpZHNbMyAwIFJdPj4KZW5kb2JqCjEgMCBvYmoKPDwvVHlwZS9DYXRhbG9nL1BhZ2VzIDIgMCBSPj4KZW5kb2JqCnhyZWYKMCA2CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDI4MSAwMDAwMCBuIAowMDAwMDAwMjMwIDAwMDAwIG4gCjAwMDAwMDAwMTUgMDAwMDAgbiAKMDAwMDAwMDEyNiAwMDAwMCBuIAowMDAwMDAwMTk1IDAwMDAwIG4gCnRyYWlsZXIKPDwvU2l6ZSA2L1Jvb3QgMSAwIFI+PgpzdGFydHhyZWYKMzMwCiUlRU9G
```
This creates a simple PDF with the text "Test Consent Document".

## ?? Sample Test Data

Use these sample values for testing:

```json
{
  "requirementId": "REQ0000000001",
  "bankId": "HDFC001",
  "quoteId": "QUOTE20240120001",
  "fileName": "bank_consent_hdfc.pdf",
  "fileData": "<your-base64-encoded-pdf>",
  "fileSize": 1024,
  "contentType": "application/pdf",
  "uploadedBy": "testuser@bank.com"
}
```

## ? Common Error Responses

### 400 Bad Request - Missing Parameters
```json
{
  "success": false,
  "message": "requirementId is required",
  "error": "INVALID_REQUEST",
  "errorCode": "MISSING_PARAMETER"
}
```

### 404 Not Found - File Not Found
```json
{
  "success": false,
  "message": "Consent document not found on Google Drive",
  "error": "FILE_NOT_FOUND",
  "data": {
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "nonexistent.pdf",
    "hint": "Check logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt"
  }
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred while uploading the consent document to Google Drive",
  "error": "Error message details",
  "errorCode": "SERVER_ERROR",
  "data": {
    "hint": "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
    "requestId": "..."
  }
}
```

## ?? Debugging Tips

### 1. Check Detailed Logs
All operations are logged in detail. Check the log file at:
```
C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt
```
(where YYYYMMDD is today's date, e.g., FtpLog_20240120.txt)

### 2. Postman Console
Enable Postman Console to view detailed request/response information:
1. View ? Show Postman Console (or Alt + Ctrl + C)
2. Monitor request headers, body, and response details

### 3. Network Issues
If requests fail to connect:
- Verify the `base_url` environment variable is correct
- Check if the API server is running
- Verify firewall settings allow the connection
- Test with the Health Check endpoint first

### 4. Authentication Errors
This API has **NO AUTHENTICATION REQUIRED**. If you get authentication errors:
- Remove any Authorization headers
- Remove any API keys from headers
- The endpoints are designed for plain access

## ?? Response Format

All endpoints follow a consistent response format:

### Success Response
```json
{
  "success": true,
  "message": "Operation description",
  "data": {
    // Response data here
  }
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "error": "ERROR_CODE",
  "errorCode": "SPECIFIC_ERROR",
  "data": {
    // Additional error context
  }
}
```

## ?? Security Notes

- **No Authentication Required**: These endpoints are accessible without authentication
- **Plain Access**: All roles (Account, Bank, Commissioner) can access
- **Production**: Consider adding authentication for production environments
- **Logging**: All requests are logged with client IP and request details
- **Data Validation**: Input validation is performed on all endpoints

## ?? Collection Features

- ? Complete endpoint coverage
- ? Pre-configured sample requests
- ? Example responses for all scenarios
- ? Environment variable support
- ? Detailed descriptions
- ? Error response examples
- ? Query parameter documentation

## ?? Support

For issues or questions:
1. Check the detailed logs at `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`
2. Review the API documentation in the controller comments
3. Verify Google Drive credentials and configuration
4. Check network connectivity to Google Drive API

## ?? License

Part of the SMKCAPI - .NET 4.6.2 Web API Project

---

**Last Updated**: January 2024  
**API Version**: 1.0  
**Framework**: .NET Framework 4.6.2
