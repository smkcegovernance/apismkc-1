# Google Drive API - Complete Reference Guide

**Project:** SMKCAPI - .NET Framework 4.6.2  
**Controller:** GoogleDriveConsentController  
**Service:** GoogleDriveStorageService  
**Base URL:** `/api/deposits/consent/googledrive`  
**Authentication:** None Required (All endpoints are `[AllowAnonymous]`)  
**Date:** February 2025

---

## ?? Table of Contents

1. [Quick Reference](#quick-reference)
2. [Upload Document API](#upload-document-api)
3. [Download Document API](#download-document-api)
4. [Get Document Info API](#get-document-info-api)
5. [Health Check API](#health-check-api)
6. [Error Codes](#error-codes)
7. [Code Examples](#code-examples)
8. [Testing Guide](#testing-guide)

---

## Quick Reference

| Endpoint | Method | Auth Required | Purpose |
|----------|--------|---------------|---------|
| `/api/deposits/consent/googledrive/health` | GET | ? No | Health check |
| `/api/deposits/consent/googledrive/upload` | POST | ? No | Upload document |
| `/api/deposits/consent/googledrive/download` | GET | ? No | Download document |
| `/api/deposits/consent/googledrive/info` | GET | ? No | Get document metadata |

**Storage Structure:**
```
Google Drive/
??? DepositManager/
    ??? BankConsent/
        ??? {requirementId}/
            ??? {bankId}/
                ??? {fileName}
```

---

# Upload Document API

## Endpoint Details

**URL:** `POST /api/deposits/consent/googledrive/upload`  
**Content-Type:** `application/json`  
**Authentication:** None (AllowAnonymous)  
**Controller Method:** `UploadConsentDocument([FromBody] UploadConsentDocumentRequest request)`

---

## Request Payload

### JSON Structure

```json
{
  "requirementId": "string",
  "bankId": "string",
  "quoteId": "string",
  "fileName": "string",
  "fileData": "string (base64)",
  "fileSize": number,
  "contentType": "string",
  "uploadedBy": "string",
  "remarks": "string"
}
```

### Field Specifications

| Field | Type | Required | Max Length | Validation | Description |
|-------|------|----------|------------|------------|-------------|
| `requirementId` | string | ? **Yes** | 50 | Not null/empty | Unique requirement identifier (e.g., REQ0000000001) |
| `bankId` | string | ? **Yes** | 50 | Not null/empty | Bank identifier (e.g., BANK001) |
| `quoteId` | string | ? No | 50 | - | Optional quote reference |
| `fileName` | string | ? **Yes** | 255 | Not null/empty, valid filename | File name with extension (e.g., consent_document.pdf) |
| `fileData` | string | ? **Yes** | - | Valid Base64 | Base64 encoded file content |
| `fileSize` | number | ? No | - | Positive integer | File size in bytes (for logging) |
| `contentType` | string | ? No | 100 | - | MIME type (default: "application/pdf") |
| `uploadedBy` | string | ? No | 100 | - | User email or identifier |
| `remarks` | string | ? No | 500 | - | Additional notes or comments |

---

## Request Examples

### Minimal Request (Required Fields Only)

```json
{
  "requirementId": "REQ0000000001",
  "bankId": "BANK001",
  "fileName": "consent.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKL01lZGlhQm94IFswIDAgNTk1IDg0Ml0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovQ29udGVudHMgNCAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwKL0xlbmd0aCA0NAo+PgpzdHJlYW0KQlQKL0YxIDI0IFRmCjEwMCA3MDAgVGQKKFRlc3QgUERGKSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCnhyZWYKMCA1CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxOCAwMDAwMCBuIAowMDAwMDAwMDc3IDAwMDAwIG4gCjAwMDAwMDAxNzggMDAwMDAgbiAKMDAwMDAwMDI1NyAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDUKL1Jvb3QgMSAwIFIKPj4Kc3RhcnR4cmVmCjM0MQolJUVPRgo="
}
```

### Complete Request (All Fields)

```json
{
  "requirementId": "REQ0000000001",
  "bankId": "BANK001",
  "quoteId": "QT001",
  "fileName": "consent_document.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKL01lZGlhQm94IFswIDAgNTk1IDg0Ml0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovQ29udGVudHMgNCAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwKL0xlbmd0aCA0NAo+PgpzdHJlYW0KQlQKL0YxIDI0IFRmCjEwMCA3MDAgVGQKKFRlc3QgUERGKSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCnhyZWYKMCA1CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxOCAwMDAwMCBuIAowMDAwMDAwMDc3IDAwMDAwIG4gCjAwMDAwMDAxNzggMDAwMDAgbiAKMDAwMDAwMDI1NyAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDUKL1Jvb3QgMSAwIFIKPj4Kc3RhcnR4cmVmCjM0MQolJUVPRgo=",
  "fileSize": 245678,
  "contentType": "application/pdf",
  "uploadedBy": "user@example.com",
  "remarks": "Bank consent document for deposit requirement"
}
```

---

## Response

### Success Response (201 Created)

**HTTP Status:** `201 Created`  
**Content-Type:** `application/json`

```json
{
  "success": true,
  "message": "Consent document uploaded successfully to Google Drive",
  "data": {
    "fileName": "consent_document.pdf",
    "originalFileName": "consent_document.pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "quoteId": "QT001",
    "fileSize": 245678,
    "contentType": "application/pdf",
    "uploadedAt": "2025-02-03T10:30:00Z",
    "uploadedBy": "user@example.com",
    "storagePath": "DepositManager/BankConsent/REQ0000000001/BANK001/consent_document.pdf",
    "downloadUrl": "/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf"
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Always `true` for successful uploads |
| `message` | string | Success message |
| `data.fileName` | string | Actual file name stored in Google Drive |
| `data.originalFileName` | string | Original file name from request |
| `data.requirementId` | string | Requirement ID used for storage path |
| `data.bankId` | string | Bank ID used for storage path |
| `data.quoteId` | string | Quote ID (if provided) |
| `data.fileSize` | number | File size in bytes |
| `data.contentType` | string | MIME type of the file |
| `data.uploadedAt` | string (ISO 8601) | UTC timestamp of upload |
| `data.uploadedBy` | string | User who uploaded (if provided) |
| `data.storagePath` | string | Full path in Google Drive |
| `data.downloadUrl` | string | Relative URL to download the file |

---

### Error Responses

#### 400 Bad Request - Null Request Body

```json
{
  "success": false,
  "message": "Request body is required",
  "error": "INVALID_REQUEST",
  "errorCode": "NULL_REQUEST"
}
```

#### 400 Bad Request - Missing Required Field

```json
{
  "success": false,
  "message": "requirementId is required",
  "error": "INVALID_PARAMETER",
  "errorCode": "NULL_PARAMETER"
}
```

#### 400 Bad Request - Invalid Base64

```json
{
  "success": false,
  "message": "Invalid base64 data for consent document",
  "error": "INVALID_OPERATION",
  "errorCode": "OPERATION_FAILED"
}
```

#### 500 Internal Server Error

```json
{
  "success": false,
  "message": "An error occurred while uploading the consent document to Google Drive",
  "error": "Detailed error message",
  "errorCode": "SERVER_ERROR",
  "data": {
    "hint": "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "consent_document.pdf",
    "requestId": "guid-value"
  }
}
```

---

## Validation Rules

### UploadConsentDocumentRequest.Validate()

The request is validated before processing. The following rules apply:

1. **requirementId**: Must not be null, empty, or whitespace
2. **bankId**: Must not be null, empty, or whitespace
3. **fileName**: Must not be null, empty, or whitespace
4. **fileData**: Must not be null, empty, or whitespace
5. **fileData**: Must be valid Base64 string

**Validation Error Example:**

```json
{
  "success": false,
  "message": "Validation failed: requirementId is required",
  "error": "VALIDATION_ERROR"
}
```

---

## File Processing

### Upload Flow

1. **Request Received** ? Log request with unique RequestId
2. **Validate Request** ? Check all required fields
3. **Decode Base64** ? Convert fileData to byte array
4. **Create Folder Structure** ? `DepositManager/BankConsent/{requirementId}/{bankId}/`
5. **Check Existing File** ? If file exists, delete old version
6. **Upload to Google Drive** ? Store new file
7. **Return Success** ? Return file metadata and download URL

### Storage Path Structure

```
Google Drive Root
??? DepositManager/
    ??? BankConsent/
        ??? {requirementId}/           ? Created automatically
            ??? {bankId}/              ? Created automatically
                ??? {fileName}         ? Your file
```

**Example:**
```
My Drive/
??? DepositManager/
    ??? BankConsent/
        ??? REQ0000000001/
            ??? BANK001/
                ??? consent_document.pdf
```

---

## Logging

Every upload request is logged with:

- Request ID (unique GUID)
- Client IP address
- Timestamp (UTC)
- Request parameters (requirementId, bankId, fileName, fileSize)
- Upload progress (step-by-step)
- Success/failure status
- Error details (if failed)

**Log Location:** `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

**Log Entry Example:**

```
================================================================================
=== GOOGLE DRIVE UPLOAD REQUEST START - RequestId: abc-123-def-456 ===
================================================================================
Client IP: 192.168.40.27
RequestId: abc-123-def-456
Timestamp: 2025-02-03 10:30:00.123 UTC
Authentication: NONE (Plain Access)
--- Request Parameters ---
  RequirementId: REQ0000000001
  BankId: BANK001
  QuoteId: QT001
  FileName: consent_document.pdf
  FileSize: 245678
  ContentType: application/pdf
  UploadedBy: user@example.com
[Step 1] Validating request parameters - SUCCESS - All parameters valid
[Step 2] Preparing to upload file to Google Drive - SUCCESS
[Step 3] Calling Google Drive Storage Service - SUCCESS - File uploaded successfully: consent_document.pdf
=== GOOGLE DRIVE UPLOAD REQUEST COMPLETED SUCCESSFULLY ===
================================================================================
```

---

# Download Document API

## Endpoint Details

**URL:** `GET /api/deposits/consent/googledrive/download`  
**Authentication:** None (AllowAnonymous)  
**Controller Method:** `DownloadConsentDocument(string requirementId, string bankId, string fileName)`

---

## Request Parameters

### Query String Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `requirementId` | string | ? **Yes** | Requirement identifier | REQ0000000001 |
| `bankId` | string | ? **Yes** | Bank identifier | BANK001 |
| `fileName` | string | ? **Yes** | File name with extension | consent_document.pdf |

---

## Request Examples

### Complete URL (Development)

```
http://localhost/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf
```

### Complete URL (Production)

```
https://yourdomain.com/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf
```

### URL with Special Characters

If fileName contains spaces or special characters, URL encode them:

```
http://localhost/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent%20document%20(final).pdf
```

---

## Response Formats

The API supports **two response formats** based on the `Accept` header:

### Format 1: Binary Download (Default)

**Request Headers:**
```
Accept: application/pdf
```
or
```
Accept: application/octet-stream
```
or
```
Accept: */*
```

**Response:**

**HTTP Status:** `200 OK`  
**Response Headers:**
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="consent_document.pdf"
Content-Length: 245678
```

**Response Body:** Binary PDF data (file download)

**Usage:** Direct file download in browser or save to disk

---

### Format 2: JSON Response with Base64

**Request Headers:**
```
Accept: application/json
```

**Response:**

**HTTP Status:** `200 OK`  
**Content-Type:** `application/json`

```json
{
  "success": true,
  "message": "Consent document retrieved successfully from Google Drive",
  "data": {
    "fileName": "consent_document.pdf",
    "fileData": "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKL01lZGlhQm94IFswIDAgNTk1IDg0Ml0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovQ29udGVudHMgNCAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwKL0xlbmd0aCA0NAo+PgpzdHJlYW0KQlQKL0YxIDI0IFRmCjEwMCA3MDAgVGQKKFRlc3QgUERGKSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCnhyZWYKMCA1CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxOCAwMDAwMCBuIAowMDAwMDAwMDc3IDAwMDAwIG4gCjAwMDAwMDAxNzggMDAwMDAgbiAKMDAwMDAwMDI1NyAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDUKL1Jvb3QgMSAwIFIKPj4Kc3RhcnR4cmVmCjM0MQolJUVPRgo=",
    "contentType": "application/pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "downloadedAt": "2025-02-03T10:35:00Z",
    "storageLocation": "Google Drive"
  }
}
```

**Usage:** When you need file data in JSON format (e.g., for API-to-API communication)

---

## Error Responses

### 400 Bad Request - Missing Parameter

**Missing requirementId:**
```json
{
  "success": false,
  "message": "requirementId is required",
  "error": "INVALID_PARAMETER"
}
```

**Missing bankId:**
```json
{
  "success": false,
  "message": "bankId is required",
  "error": "INVALID_PARAMETER"
}
```

**Missing fileName:**
```json
{
  "success": false,
  "message": "fileName is required",
  "error": "INVALID_PARAMETER"
}
```

---

### 404 Not Found - File Not Found

```json
{
  "success": false,
  "message": "Consent document not found on Google Drive",
  "error": "FILE_NOT_FOUND",
  "data": {
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "consent_document.pdf",
    "hint": "Check logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt for detailed path information"
  }
}
```

---

### 500 Internal Server Error

```json
{
  "success": false,
  "message": "An error occurred while downloading the consent document from Google Drive",
  "error": "Detailed error message",
  "data": {
    "hint": "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "consent_document.pdf",
    "requestId": "guid-value"
  }
}
```

---

## Download Flow

1. **Request Received** ? Log request with unique RequestId
2. **Validate Parameters** ? Check requirementId, bankId, fileName
3. **Find Folder Hierarchy** ? Locate DepositManager/BankConsent/{requirementId}/{bankId}/
4. **Find File** ? Search for fileName in bank folder
5. **Download from Google Drive** ? Get file bytes
6. **Determine Response Format** ? Check Accept header
7. **Return Response** ? Binary or JSON based on Accept header

---

## Logging

**Log Location:** `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

**Success Log Example:**

```
================================================================================
=== GOOGLE DRIVE DOWNLOAD REQUEST START - RequestId: xyz-789-abc-123 ===
================================================================================
Client IP: 192.168.40.27
RequestId: xyz-789-abc-123
Timestamp: 2025-02-03 10:35:00.456 UTC
Authentication: NONE (Plain Access)
--- Request Parameters ---
  RequirementId: REQ0000000001
  BankId: BANK001
  FileName: consent_document.pdf
[Step 1] Validating request parameters - SUCCESS - All required parameters present
[Step 2] Preparing to download file from Google Drive - SUCCESS
[Step 3] Calling Google Drive Storage Service - SUCCESS - Downloading: consent_document.pdf for REQ0000000001/BANK001
[Step 3] Google Drive Storage Service completed - SUCCESS - File retrieved, Base64 size: 327570 chars
[Step 4] Determining response format - SUCCESS
  Accept Header: application/pdf
  Response Format: Binary PDF
[Step 5] Preparing binary response - SUCCESS - File size: 245678 bytes (239.9 KB)
=== GOOGLE DRIVE DOWNLOAD REQUEST COMPLETED SUCCESSFULLY (BINARY) ===
================================================================================
```

---

# Get Document Info API

## Endpoint Details

**URL:** `GET /api/deposits/consent/googledrive/info`  
**Authentication:** None (AllowAnonymous)  
**Controller Method:** `GetConsentDocumentInfo(string requirementId, string bankId, string fileName)`

---

## Request Parameters

### Query String Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `requirementId` | string | ? **Yes** | Requirement identifier | REQ0000000001 |
| `bankId` | string | ? **Yes** | Bank identifier | BANK001 |
| `fileName` | string | ? **Yes** | File name with extension | consent_document.pdf |

---

## Request Example

### Complete URL

```
http://localhost/api/deposits/consent/googledrive/info?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf
```

---

## Response

### Success Response (200 OK)

**HTTP Status:** `200 OK`  
**Content-Type:** `application/json`

```json
{
  "success": true,
  "message": "Consent document information retrieved successfully from Google Drive",
  "data": {
    "fileName": "consent_document.pdf",
    "fileSize": 245678,
    "contentType": "application/pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "exists": true,
    "storageLocation": "Google Drive",
    "storagePath": "DepositManager/BankConsent/REQ0000000001/BANK001/consent_document.pdf",
    "downloadUrl": "/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf"
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Always `true` for successful info retrieval |
| `message` | string | Success message |
| `data.fileName` | string | File name |
| `data.fileSize` | number | File size in bytes |
| `data.contentType` | string | MIME type (always "application/pdf") |
| `data.requirementId` | string | Requirement identifier |
| `data.bankId` | string | Bank identifier |
| `data.exists` | boolean | Always `true` if file found |
| `data.storageLocation` | string | Storage type ("Google Drive") |
| `data.storagePath` | string | Full path in Google Drive |
| `data.downloadUrl` | string | Relative URL to download the file |

---

### Error Responses

#### 400 Bad Request - Missing Parameters

```json
{
  "success": false,
  "message": "requirementId, bankId, and fileName are required",
  "error": "INVALID_PARAMETER"
}
```

#### 404 Not Found - File Not Found

```json
{
  "success": false,
  "message": "Consent document not found on Google Drive",
  "error": "FILE_NOT_FOUND",
  "data": {
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "consent_document.pdf",
    "hint": "Check logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt for detailed path information"
  }
}
```

#### 500 Internal Server Error

```json
{
  "success": false,
  "message": "An error occurred while retrieving consent document information from Google Drive",
  "error": "Detailed error message",
  "data": {
    "hint": "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "consent_document.pdf",
    "requestId": "guid-value"
  }
}
```

---

## Use Case

This endpoint is useful when you need to:
- ? Check if a file exists before downloading
- ? Get file size before downloading
- ? Verify file metadata
- ? Build file listings without downloading actual files

---

# Health Check API

## Endpoint Details

**URL:** `GET /api/deposits/consent/googledrive/health`  
**Authentication:** None (AllowAnonymous)  
**Controller Method:** `HealthCheck()`

---

## Request

No parameters required.

### Complete URL

```
http://localhost/api/deposits/consent/googledrive/health
```

---

## Response

### Success Response (200 OK)

**HTTP Status:** `200 OK`  
**Content-Type:** `application/json`

```json
{
  "success": true,
  "message": "GoogleDriveConsentController is accessible",
  "timestamp": "2025-02-03T10:30:00Z",
  "storageType": "Google Drive",
  "storageLocation": "DepositManager/BankConsent",
  "logFilePath": "C:\\smkcapi_published\\Logs\\FtpLog_20250203.txt",
  "authenticationRequired": false
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Always `true` if controller is accessible |
| `message` | string | Controller status message |
| `timestamp` | string (ISO 8601) | Current UTC timestamp |
| `storageType` | string | Always "Google Drive" |
| `storageLocation` | string | Base storage path |
| `logFilePath` | string | Current log file path |
| `authenticationRequired` | boolean | Always `false` (no auth required) |

---

## Use Case

This endpoint is useful for:
- ? Verifying controller is deployed and accessible
- ? Checking service initialization status
- ? Monitoring endpoint (health checks)
- ? Getting log file location for debugging

---

# Error Codes

## Error Code Reference

| Error Code | HTTP Status | Meaning | Common Causes |
|------------|-------------|---------|---------------|
| `NULL_REQUEST` | 400 | Request body is null | Empty POST body |
| `NULL_PARAMETER` | 400 | Required parameter is missing | Missing requirementId, bankId, or fileName |
| `INVALID_PARAMETER` | 400 | Parameter validation failed | Empty or whitespace values |
| `INVALID_ARGUMENT` | 400 | Invalid argument value | Invalid characters, format issues |
| `INVALID_OPERATION` | 400 | Operation cannot be performed | Invalid Base64, file format issues |
| `FILE_NOT_FOUND` | 404 | Document not found | File doesn't exist at specified path |
| `STORAGE_ERROR` | 500 | Storage operation failed | Google Drive API error |
| `SERVER_ERROR` | 500 | Internal server error | Unexpected exception |

---

## Standard Error Response Format

All errors follow this structure:

```json
{
  "success": false,
  "message": "Human-readable error message",
  "error": "ERROR_CODE",
  "errorCode": "SPECIFIC_ERROR_CODE",
  "data": {
    // Additional context (optional)
  }
}
```

---

# Code Examples

## C# Examples

### Upload Document

```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

public async Task<string> UploadConsentDocument(
    string requirementId,
    string bankId,
    string filePath,
    string uploadedBy)
{
    // Read file and convert to Base64
    byte[] fileBytes = File.ReadAllBytes(filePath);
    string base64Content = Convert.ToBase64String(fileBytes);
    string fileName = Path.GetFileName(filePath);

    // Create request payload
    var uploadRequest = new
    {
        requirementId = requirementId,
        bankId = bankId,
        fileName = fileName,
        fileData = base64Content,
        fileSize = fileBytes.Length,
        contentType = "application/pdf",
        uploadedBy = uploadedBy
    };

    // Serialize to JSON
    string json = JsonConvert.SerializeObject(uploadRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Send request
    using (var client = new HttpClient())
    {
        var response = await client.PostAsync(
            "http://localhost/api/deposits/consent/googledrive/upload",
            content
        );

        string result = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Upload successful!");
            Console.WriteLine(result);
        }
        else
        {
            Console.WriteLine("Upload failed:");
            Console.WriteLine(result);
        }

        return result;
    }
}

// Usage
await UploadConsentDocument(
    "REQ0000000001",
    "BANK001",
    @"C:\documents\consent.pdf",
    "admin@example.com"
);
```

---

### Download Document (Binary)

```csharp
using System;
using System.IO;
using System.Net.Http;

public async Task DownloadConsentDocument(
    string requirementId,
    string bankId,
    string fileName,
    string savePath)
{
    var url = $"http://localhost/api/deposits/consent/googledrive/download" +
              $"?requirementId={requirementId}" +
              $"&bankId={bankId}" +
              $"&fileName={Uri.EscapeDataString(fileName)}";

    using (var client = new HttpClient())
    {
        // Set Accept header for binary download
        client.DefaultRequestHeaders.Add("Accept", "application/pdf");

        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(savePath, fileBytes);
            
            Console.WriteLine($"File downloaded successfully to: {savePath}");
            Console.WriteLine($"Size: {fileBytes.Length} bytes");
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Download failed:");
            Console.WriteLine(error);
        }
    }
}

// Usage
await DownloadConsentDocument(
    "REQ0000000001",
    "BANK001",
    "consent_document.pdf",
    @"C:\downloads\consent.pdf"
);
```

---

### Download Document (JSON)

```csharp
using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

public async Task<string> DownloadConsentDocumentAsBase64(
    string requirementId,
    string bankId,
    string fileName)
{
    var url = $"http://localhost/api/deposits/consent/googledrive/download" +
              $"?requirementId={requirementId}" +
              $"&bankId={bankId}" +
              $"&fileName={Uri.EscapeDataString(fileName)}";

    using (var client = new HttpClient())
    {
        // Set Accept header for JSON response
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        var response = await client.GetAsync(url);
        string result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var json = JObject.Parse(result);
            string base64Data = json["data"]["fileData"].ToString();
            
            Console.WriteLine("File retrieved successfully");
            Console.WriteLine($"Base64 length: {base64Data.Length}");
            
            return base64Data;
        }
        else
        {
            Console.WriteLine("Download failed:");
            Console.WriteLine(result);
            return null;
        }
    }
}

// Usage
string base64 = await DownloadConsentDocumentAsBase64(
    "REQ0000000001",
    "BANK001",
    "consent_document.pdf"
);

// Convert Base64 to file
if (base64 != null)
{
    byte[] fileBytes = Convert.FromBase64String(base64);
    File.WriteAllBytes(@"C:\downloads\consent.pdf", fileBytes);
}
```

---

### Get Document Info

```csharp
using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

public async Task<object> GetDocumentInfo(
    string requirementId,
    string bankId,
    string fileName)
{
    var url = $"http://localhost/api/deposits/consent/googledrive/info" +
              $"?requirementId={requirementId}" +
              $"&bankId={bankId}" +
              $"&fileName={Uri.EscapeDataString(fileName)}";

    using (var client = new HttpClient())
    {
        var response = await client.GetAsync(url);
        string result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var json = JObject.Parse(result);
            var data = json["data"];
            
            Console.WriteLine($"File: {data["fileName"]}");
            Console.WriteLine($"Size: {data["fileSize"]} bytes");
            Console.WriteLine($"Exists: {data["exists"]}");
            Console.WriteLine($"Path: {data["storagePath"]}");
            
            return data;
        }
        else
        {
            Console.WriteLine("Get info failed:");
            Console.WriteLine(result);
            return null;
        }
    }
}

// Usage
await GetDocumentInfo(
    "REQ0000000001",
    "BANK001",
    "consent_document.pdf"
);
```

---

### Health Check

```csharp
using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

public async Task<bool> CheckHealth()
{
    using (var client = new HttpClient())
    {
        var response = await client.GetAsync(
            "http://localhost/api/deposits/consent/googledrive/health"
        );
        
        string result = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            var json = JObject.Parse(result);
            bool success = (bool)json["success"];
            
            Console.WriteLine($"Health Check: {(success ? "PASS" : "FAIL")}");
            Console.WriteLine($"Message: {json["message"]}");
            Console.WriteLine($"Auth Required: {json["authenticationRequired"]}");
            
            return success;
        }
        else
        {
            Console.WriteLine("Health check failed");
            return false;
        }
    }
}

// Usage
bool isHealthy = await CheckHealth();
```

---

## JavaScript/jQuery Examples

### Upload Document

```javascript
// Upload file using jQuery
function uploadConsentDocument(requirementId, bankId, fileInput, uploadedBy) {
    const file = fileInput.files[0];
    
    // Read file and convert to Base64
    const reader = new FileReader();
    reader.onload = function(e) {
        const base64Data = e.target.result.split(',')[1]; // Remove data:application/pdf;base64, prefix
        
        const payload = {
            requirementId: requirementId,
            bankId: bankId,
            fileName: file.name,
            fileData: base64Data,
            fileSize: file.size,
            contentType: file.type,
            uploadedBy: uploadedBy
        };
        
        $.ajax({
            url: '/api/deposits/consent/googledrive/upload',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function(response) {
                console.log('Upload successful:', response);
                alert('File uploaded successfully!');
                console.log('Download URL:', response.data.downloadUrl);
            },
            error: function(xhr, status, error) {
                console.error('Upload failed:', xhr.responseText);
                alert('Upload failed: ' + xhr.responseJSON.message);
            }
        });
    };
    
    reader.readAsDataURL(file);
}

// Usage in HTML
// <input type="file" id="fileUpload" accept="application/pdf">
// <button onclick="uploadFile()">Upload</button>

function uploadFile() {
    const fileInput = document.getElementById('fileUpload');
    uploadConsentDocument(
        'REQ0000000001',
        'BANK001',
        fileInput,
        'user@example.com'
    );
}
```

---

### Download Document (Binary)

```javascript
// Download file and trigger browser download
function downloadConsentDocument(requirementId, bankId, fileName) {
    const params = new URLSearchParams({
        requirementId: requirementId,
        bankId: bankId,
        fileName: fileName
    });
    
    const url = `/api/deposits/consent/googledrive/download?${params}`;
    
    // Create invisible anchor to trigger download
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    
    console.log('Download started for:', fileName);
}

// Usage
downloadConsentDocument('REQ0000000001', 'BANK001', 'consent_document.pdf');
```

---

### Download Document (JSON with Fetch)

```javascript
// Download file data as JSON using Fetch API
async function getConsentDocumentData(requirementId, bankId, fileName) {
    const params = new URLSearchParams({
        requirementId: requirementId,
        bankId: bankId,
        fileName: fileName
    });
    
    const url = `/api/deposits/consent/googledrive/download?${params}`;
    
    try {
        const response = await fetch(url, {
            headers: {
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message);
        }
        
        const data = await response.json();
        console.log('File retrieved:', data.data.fileName);
        console.log('Base64 length:', data.data.fileData.length);
        
        // Convert Base64 to Blob for download
        const byteCharacters = atob(data.data.fileData);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/pdf' });
        
        // Trigger download
        const blobUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = fileName;
        a.click();
        window.URL.revokeObjectURL(blobUrl);
        
    } catch (error) {
        console.error('Download failed:', error);
        alert('Download failed: ' + error.message);
    }
}

// Usage
getConsentDocumentData('REQ0000000001', 'BANK001', 'consent_document.pdf');
```

---

### Get Document Info

```javascript
// Get document info using jQuery
function getDocumentInfo(requirementId, bankId, fileName) {
    const params = $.param({
        requirementId: requirementId,
        bankId: bankId,
        fileName: fileName
    });
    
    $.ajax({
        url: `/api/deposits/consent/googledrive/info?${params}`,
        type: 'GET',
        success: function(response) {
            console.log('Document Info:', response.data);
            
            const info = response.data;
            const infoHtml = `
                <h3>Document Information</h3>
                <p><strong>File Name:</strong> ${info.fileName}</p>
                <p><strong>File Size:</strong> ${(info.fileSize / 1024).toFixed(2)} KB</p>
                <p><strong>Exists:</strong> ${info.exists ? 'Yes' : 'No'}</p>
                <p><strong>Storage Path:</strong> ${info.storagePath}</p>
                <p><strong>Download URL:</strong> <a href="${info.downloadUrl}">Download</a></p>
            `;
            
            $('#documentInfo').html(infoHtml);
        },
        error: function(xhr, status, error) {
            console.error('Get info failed:', xhr.responseText);
            alert('Failed to get document info: ' + xhr.responseJSON.message);
        }
    });
}

// Usage
getDocumentInfo('REQ0000000001', 'BANK001', 'consent_document.pdf');
```

---

### Health Check

```javascript
// Health check using Fetch API
async function checkHealth() {
    try {
        const response = await fetch('/api/deposits/consent/googledrive/health');
        const data = await response.json();
        
        if (data.success) {
            console.log('? Health Check: PASS');
            console.log('Message:', data.message);
            console.log('Storage Type:', data.storageType);
            console.log('Auth Required:', data.authenticationRequired);
            return true;
        } else {
            console.log('? Health Check: FAIL');
            return false;
        }
    } catch (error) {
        console.error('Health check error:', error);
        return false;
    }
}

// Usage
checkHealth();
```

---

## cURL Examples

### Upload Document

```bash
curl -X POST "http://localhost/api/deposits/consent/googledrive/upload" \
  -H "Content-Type: application/json" \
  -d '{
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "fileName": "consent.pdf",
    "fileData": "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKL01lZGlhQm94IFswIDAgNTk1IDg0Ml0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovQ29udGVudHMgNCAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwKL0xlbmd0aCA0NAo+PgpzdHJlYW0KQlQKL0YxIDI0IFRmCjEwMCA3MDAgVGQKKFRlc3QgUERGKSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCnhyZWYKMCA1CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxOCAwMDAwMCBuIAowMDAwMDAwMDc3IDAwMDAwIG4gCjAwMDAwMDAxNzggMDAwMDAgbiAKMDAwMDAwMDI1NyAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDUKL1Jvb3QgMSAwIFIKPj4Kc3RhcnR4cmVmCjM0MQolJUVPRgo=",
    "fileSize": 1024,
    "contentType": "application/pdf",
    "uploadedBy": "system@example.com"
  }'
```

---

### Upload from File (using PowerShell)

```powershell
# Read file and convert to Base64
$fileBytes = [System.IO.File]::ReadAllBytes("C:\documents\consent.pdf")
$base64 = [System.Convert]::ToBase64String($fileBytes)

# Create JSON payload
$payload = @{
    requirementId = "REQ0000000001"
    bankId = "BANK001"
    fileName = "consent.pdf"
    fileData = $base64
    fileSize = $fileBytes.Length
    contentType = "application/pdf"
    uploadedBy = "system@example.com"
} | ConvertTo-Json

# Send request
Invoke-RestMethod -Uri "http://localhost/api/deposits/consent/googledrive/upload" `
    -Method Post `
    -ContentType "application/json" `
    -Body $payload
```

---

### Download Document (Binary)

```bash
curl -X GET "http://localhost/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf" \
  -H "Accept: application/pdf" \
  -o downloaded_consent.pdf
```

---

### Download Document (JSON)

```bash
curl -X GET "http://localhost/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf" \
  -H "Accept: application/json"
```

---

### Get Document Info

```bash
curl -X GET "http://localhost/api/deposits/consent/googledrive/info?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf"
```

---

### Health Check

```bash
curl -X GET "http://localhost/api/deposits/consent/googledrive/health"
```

---

# Testing Guide

## Postman Collection

### Environment Variables

Create a Postman environment with these variables:

```json
{
  "baseUrl": "http://localhost",
  "requirementId": "REQ0000000001",
  "bankId": "BANK001",
  "fileName": "consent_document.pdf"
}
```

---

### Test 1: Health Check

**Request:**
```
GET {{baseUrl}}/api/deposits/consent/googledrive/health
```

**Expected Response:** `200 OK` with success message

---

### Test 2: Upload Document

**Request:**
```
POST {{baseUrl}}/api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "{{requirementId}}",
  "bankId": "{{bankId}}",
  "fileName": "{{fileName}}",
  "fileData": "JVBERi0xLjQKJeLjz9MK...",
  "fileSize": 1024,
  "contentType": "application/pdf",
  "uploadedBy": "test@example.com"
}
```

**Expected Response:** `201 Created` with file metadata

---

### Test 3: Get Document Info

**Request:**
```
GET {{baseUrl}}/api/deposits/consent/googledrive/info?requirementId={{requirementId}}&bankId={{bankId}}&fileName={{fileName}}
```

**Expected Response:** `200 OK` with file info

---

### Test 4: Download Document (Binary)

**Request:**
```
GET {{baseUrl}}/api/deposits/consent/googledrive/download?requirementId={{requirementId}}&bankId={{bankId}}&fileName={{fileName}}
Headers:
  Accept: application/pdf
```

**Expected Response:** `200 OK` with binary PDF content

---

### Test 5: Download Document (JSON)

**Request:**
```
GET {{baseUrl}}/api/deposits/consent/googledrive/download?requirementId={{requirementId}}&bankId={{bankId}}&fileName={{fileName}}
Headers:
  Accept: application/json
```

**Expected Response:** `200 OK` with JSON containing Base64 data

---

## Manual Testing Steps

### Step 1: Verify Service is Running

```powershell
# Health check
curl http://localhost/api/deposits/consent/googledrive/health
```

**Expected:** Success response with `authenticationRequired: false`

---

### Step 2: Upload a Test File

1. Create a small PDF file or use the Base64 test content
2. Send POST request to `/upload` endpoint
3. Verify response has `201 Created` status
4. Note the `downloadUrl` in response

---

### Step 3: Verify File in Google Drive

1. Open Google Drive in browser
2. Navigate to: `DepositManager/BankConsent/{requirementId}/{bankId}/`
3. Verify file exists with correct name

---

### Step 4: Download the File

1. Use the `downloadUrl` from upload response
2. Verify file downloads correctly
3. Open downloaded PDF and verify content

---

### Step 5: Get File Info

1. Call `/info` endpoint with same parameters
2. Verify `fileSize` matches uploaded file
3. Verify `exists: true`

---

### Step 6: Check Logs

```powershell
# View latest logs
Get-Content "C:\smkcapi_published\Logs\FtpLog_*.txt" -Tail 50
```

**Look for:**
- Upload success messages
- Download success messages
- No error messages

---

## Common Testing Scenarios

### Scenario 1: Upload New File

```json
POST /api/deposits/consent/googledrive/upload
{
  "requirementId": "REQ001",
  "bankId": "BANK001",
  "fileName": "new_consent.pdf",
  "fileData": "base64..."
}
```

**Expected:** `201 Created`, file created in Drive

---

### Scenario 2: Upload Duplicate (Replace Existing)

```json
POST /api/deposits/consent/googledrive/upload
{
  "requirementId": "REQ001",
  "bankId": "BANK001",
  "fileName": "new_consent.pdf",  // Same as before
  "fileData": "different_base64..."
}
```

**Expected:** `201 Created`, old file deleted, new file uploaded

---

### Scenario 3: Download Non-Existent File

```
GET /api/deposits/consent/googledrive/download?requirementId=INVALID&bankId=INVALID&fileName=notfound.pdf
```

**Expected:** `404 Not Found` with error message

---

### Scenario 4: Missing Required Parameters

```
GET /api/deposits/consent/googledrive/download?requirementId=REQ001
```

**Expected:** `400 Bad Request` - "bankId is required"

---

### Scenario 5: Invalid Base64 Upload

```json
POST /api/deposits/consent/googledrive/upload
{
  "requirementId": "REQ001",
  "bankId": "BANK001",
  "fileName": "test.pdf",
  "fileData": "not-valid-base64!!!"
}
```

**Expected:** `400 Bad Request` - "Invalid base64 data"

---

## Performance Testing

### Expected Response Times

| Operation | Expected Time | Notes |
|-----------|---------------|-------|
| Health Check | < 100ms | Simple endpoint |
| Upload (1 MB) | 2-5 seconds | Depends on network |
| Download (1 MB) | 1-3 seconds | Cached by Google |
| Get Info | < 1 second | Metadata only |

---

## Security Testing

### Test: No Authentication Required

All endpoints should be accessible without any authentication:

```bash
# All these should work without auth headers
curl http://localhost/api/deposits/consent/googledrive/health
curl http://localhost/api/deposits/consent/googledrive/info?...
curl -X POST http://localhost/api/deposits/consent/googledrive/upload -d {...}
```

**Expected:** All succeed without authentication

---

## Troubleshooting Test Failures

### Upload Fails with 500 Error

1. Check Web.config has Google Drive settings
2. Verify service account credentials file exists
3. Check logs for authentication errors
4. Verify Drive folder is shared with service account

---

### Download Returns 404

1. Verify file was actually uploaded (check Drive)
2. Verify requirementId, bankId, fileName match exactly
3. Check case sensitivity
4. Check logs for detailed path information

---

### Base64 Decoding Errors

1. Ensure Base64 string doesn't have data URI prefix (`data:application/pdf;base64,`)
2. Verify no line breaks or whitespace in Base64 string
3. Test with known-good Base64 content

---

## Test Data

### Sample Base64 PDF (Small Test File)

```
JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKL01lZGlhQm94IFswIDAgNTk1IDg0Ml0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovQ29udGVudHMgNCAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwKL0xlbmd0aCA0NAo+PgpzdHJlYW0KQlQKL0YxIDI0IFRmCjEwMCA3MDAgVGQKKFRlc3QgUERGKSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCnhyZWYKMCA1CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxOCAwMDAwMCBuIAowMDAwMDAwMDc3IDAwMDAwIG4gCjAwMDAwMDAxNzggMDAwMDAgbiAKMDAwMDAwMDI1NyAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDUKL1Jvb3QgMSAwIFIKPj4Kc3RhcnR4cmVmCjM0MQolJUVPRgo=
```

This is a minimal valid PDF containing the text "Test PDF".

---

## Appendix

### Important Files

| File | Purpose | Location |
|------|---------|----------|
| Web.config | Google Drive configuration | Project root |
| service-account-credentials.json | Authentication credentials | GoogleDrive\ folder |
| GoogleDriveStorageService.cs | Storage service implementation | Services\DepositManager\ |
| GoogleDriveConsentController.cs | API controller | Controllers\DepositManager\ |
| FtpLog_YYYYMMDD.txt | Application logs | C:\smkcapi_published\Logs\ |

---

### Configuration Settings (Web.config)

```xml
<add key="GoogleDrive_UseServiceAccount" value="true" />
<add key="GoogleDrive_CredentialsPath" value="GoogleDrive\service-account-credentials.json" />
<add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
```

---

### Service Account Details

**Project ID:** smkc-website  
**Service Account Email:** smkc-analytics@smkc-website.iam.gserviceaccount.com  
**Client ID:** 116547222600715929223

**?? CRITICAL:** Share your Google Drive `DepositManager` folder with the service account email for the API to work!

---

### Support

**Logs:** `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`  
**Documentation:** `DOCS\GOOGLE_DRIVE_*.md`  
**GitHub:** https://github.com/atulbee/apismkc

---

**Document Version:** 1.0  
**Last Updated:** February 2025  
**Framework:** .NET Framework 4.6.2  
**Google Drive API:** v3

---

**? END OF DOCUMENT**
