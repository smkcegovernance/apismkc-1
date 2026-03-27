# Google Drive API - Quick Reference Card

**SMKCAPI - .NET Framework 4.6.2**  
**Base URL:** `/api/deposits/consent/googledrive`  
**Authentication:** ? None Required (AllowAnonymous)

---

## ?? Endpoints Summary

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/health` | Health check |
| POST | `/upload` | Upload document |
| GET | `/download` | Download document |
| GET | `/info` | Get document metadata |

---

## ?? UPLOAD

**URL:** `POST /api/deposits/consent/googledrive/upload`

**Minimal Request:**
```json
{
  "requirementId": "REQ0000000001",
  "bankId": "BANK001",
  "fileName": "consent.pdf",
  "fileData": "JVBERi0xLjQKJe..."
}
```

**Complete Request:**
```json
{
  "requirementId": "REQ0000000001",
  "bankId": "BANK001",
  "quoteId": "QT001",
  "fileName": "consent_document.pdf",
  "fileData": "JVBERi0xLjQKJe...",
  "fileSize": 245678,
  "contentType": "application/pdf",
  "uploadedBy": "user@example.com",
  "remarks": "Bank consent document"
}
```

**Success Response (201):**
```json
{
  "success": true,
  "message": "Consent document uploaded successfully to Google Drive",
  "data": {
    "fileName": "consent_document.pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001",
    "storagePath": "DepositManager/BankConsent/REQ0000000001/BANK001/consent_document.pdf",
    "downloadUrl": "/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf"
  }
}
```

---

## ?? DOWNLOAD

**URL:** `GET /api/deposits/consent/googledrive/download`

**Parameters:**
```
?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf
```

**Binary Response (Accept: application/pdf):**
```
HTTP/1.1 200 OK
Content-Type: application/pdf
Content-Disposition: attachment; filename="consent_document.pdf"

[Binary PDF data]
```

**JSON Response (Accept: application/json):**
```json
{
  "success": true,
  "message": "Consent document retrieved successfully from Google Drive",
  "data": {
    "fileName": "consent_document.pdf",
    "fileData": "JVBERi0xLjQKJe...",
    "contentType": "application/pdf",
    "requirementId": "REQ0000000001",
    "bankId": "BANK001"
  }
}
```

---

## ?? INFO

**URL:** `GET /api/deposits/consent/googledrive/info`

**Parameters:**
```
?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf
```

**Response (200):**
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
    "storagePath": "DepositManager/BankConsent/REQ0000000001/BANK001/consent_document.pdf",
    "downloadUrl": "/api/deposits/consent/googledrive/download?requirementId=REQ0000000001&bankId=BANK001&fileName=consent_document.pdf"
  }
}
```

---

## ?? HEALTH

**URL:** `GET /api/deposits/consent/googledrive/health`

**No parameters required**

**Response (200):**
```json
{
  "success": true,
  "message": "GoogleDriveConsentController is accessible",
  "timestamp": "2025-02-03T10:30:00Z",
  "storageType": "Google Drive",
  "storageLocation": "DepositManager/BankConsent",
  "authenticationRequired": false
}
```

---

## ?? Required Fields

### Upload
- ? `requirementId`
- ? `bankId`
- ? `fileName`
- ? `fileData` (Base64)

### Download / Info
- ? `requirementId` (query param)
- ? `bankId` (query param)
- ? `fileName` (query param)

---

## ?? Error Codes

| Code | Status | Meaning |
|------|--------|---------|
| NULL_REQUEST | 400 | Request body is null |
| NULL_PARAMETER | 400 | Missing required parameter |
| INVALID_PARAMETER | 400 | Invalid parameter value |
| INVALID_OPERATION | 400 | Invalid Base64 or operation |
| FILE_NOT_FOUND | 404 | File not found |
| SERVER_ERROR | 500 | Internal server error |

---

## ?? C# Quick Examples

### Upload
```csharp
var request = new {
    requirementId = "REQ0000000001",
    bankId = "BANK001",
    fileName = "consent.pdf",
    fileData = Convert.ToBase64String(fileBytes)
};

var json = JsonConvert.SerializeObject(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync(url, content);
```

### Download Binary
```csharp
client.DefaultRequestHeaders.Add("Accept", "application/pdf");
var bytes = await client.GetByteArrayAsync(url);
File.WriteAllBytes("consent.pdf", bytes);
```

### Download JSON
```csharp
client.DefaultRequestHeaders.Add("Accept", "application/json");
var json = await client.GetStringAsync(url);
var data = JObject.Parse(json);
string base64 = data["data"]["fileData"].ToString();
```

---

## ?? JavaScript Quick Examples

### Upload
```javascript
const payload = {
    requirementId: 'REQ0000000001',
    bankId: 'BANK001',
    fileName: 'consent.pdf',
    fileData: base64String
};

const response = await fetch('/api/deposits/consent/googledrive/upload', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
});
```

### Download Binary
```javascript
const params = new URLSearchParams({
    requirementId: 'REQ0000000001',
    bankId: 'BANK001',
    fileName: 'consent.pdf'
});

const a = document.createElement('a');
a.href = `/api/deposits/consent/googledrive/download?${params}`;
a.download = 'consent.pdf';
a.click();
```

---

## ?? cURL Quick Examples

### Upload
```bash
curl -X POST "http://localhost/api/deposits/consent/googledrive/upload" \
  -H "Content-Type: application/json" \
  -d '{"requirementId":"REQ001","bankId":"BANK001","fileName":"test.pdf","fileData":"JVBERi0..."}'
```

### Download
```bash
curl "http://localhost/api/deposits/consent/googledrive/download?requirementId=REQ001&bankId=BANK001&fileName=test.pdf" \
  -H "Accept: application/pdf" \
  -o downloaded.pdf
```

### Info
```bash
curl "http://localhost/api/deposits/consent/googledrive/info?requirementId=REQ001&bankId=BANK001&fileName=test.pdf"
```

### Health
```bash
curl "http://localhost/api/deposits/consent/googledrive/health"
```

---

## ?? Storage Structure

```
Google Drive/
??? DepositManager/
    ??? BankConsent/
        ??? {requirementId}/
            ??? {bankId}/
                ??? {fileName}

Example:
My Drive/
??? DepositManager/
    ??? BankConsent/
        ??? REQ0000000001/
            ??? BANK001/
                ??? consent_document.pdf
```

---

## ?? Configuration (Web.config)

```xml
<add key="GoogleDrive_UseServiceAccount" value="true" />
<add key="GoogleDrive_CredentialsPath" value="GoogleDrive\service-account-credentials.json" />
<add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
```

---

## ?? Service Account

**Email:** `smkc-analytics@smkc-website.iam.gserviceaccount.com`  
**Project:** `smkc-website`

**?? IMPORTANT:** Share `DepositManager` folder with service account email!

---

## ?? Sample Test Data

**Minimal Valid PDF (Base64):**
```
JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKL01lZGlhQm94IFswIDAgNTk1IDg0Ml0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovQ29udGVudHMgNCAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwKL0xlbmd0aCA0NAo+PgpzdHJlYW0KQlQKL0YxIDI0IFRmCjEwMCA3MDAgVGQKKFRlc3QgUERGKSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCnhyZWYKMCA1CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxOCAwMDAwMCBuIAowMDAwMDAwMDc3IDAwMDAwIG4gCjAwMDAwMDAxNzggMDAwMDAgbiAKMDAwMDAwMDI1NyAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDUKL1Jvb3QgMSAwIFIKPj4Kc3RhcnR4cmVmCjM0MQolJUVPRgo=
```

---

## ?? Logs

**Location:** `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

**Success Indicators:**
- `GOOGLE DRIVE UPLOAD REQUEST COMPLETED SUCCESSFULLY`
- `GOOGLE DRIVE DOWNLOAD REQUEST COMPLETED SUCCESSFULLY`

**Error Indicators:**
- `GOOGLE DRIVE UPLOAD REQUEST FAILED`
- `GOOGLE DRIVE DOWNLOAD REQUEST FAILED`

---

## ? Testing Checklist

- [ ] Health check returns success
- [ ] Upload minimal payload works
- [ ] Upload full payload works
- [ ] Download binary works
- [ ] Download JSON works
- [ ] Get info works
- [ ] File appears in Google Drive
- [ ] Logs show no errors
- [ ] Service account authenticated silently

---

## ?? Support

**Full Documentation:** `DOCS\GOOGLE_DRIVE_API_REFERENCE.md`  
**GitHub:** https://github.com/atulbee/apismkc  
**Logs:** `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

---

**? Quick Reference v1.0 | .NET Framework 4.6.2 | February 2025**
