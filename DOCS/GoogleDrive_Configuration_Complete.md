# ? Configuration Complete - Verification Steps

## ?? Good News!

Your Google Drive configuration has been successfully updated with your shared folder ID:

```
Folder ID: 1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
Folder URL: https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
```

---

## ?? IMPORTANT: Final Step Required

**Before testing, you MUST share this folder with your service account!**

### How to Share the Folder

1. **Get Service Account Email**:
   - Open: `GoogleDrive\service-account-credentials.json`
   - Find the `"client_email"` field
   - Copy the email (format: `xxx@xxx.iam.gserviceaccount.com`)

2. **Share the Folder**:
   - Open the folder in Google Drive: https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
   - Right-click ? **Share**
   - Paste the service account email
   - Change role to: **Editor** (not Viewer!)
   - **Uncheck** "Notify people" (service accounts don't read emails)
   - Click **Send**

3. **Verify Sharing**:
   - The service account email should appear in "People with access"
   - Permission should be: **Editor**

---

## ?? Testing Checklist

### Step 1: Restart Application
```
- Stop IIS / Application Pool
- Start IIS / Application Pool
```

### Step 2: Check Initialization Logs
**Location**: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

**Look for**:
```
=== Google Drive Storage Service Initialized ===
Authentication Type: Service Account (Silent)
Shared Folder ID: 1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
Upload Mode: Shared Folder (Service Account compatible)
? Google Drive service initialized successfully
```

**Warning Signs** (if folder not shared):
```
? WARNING: Service Account without Shared Folder ID configured!
```
**Action**: Share the folder first!

### Step 3: Test Health Check
**Request**:
```http
GET http://localhost:5000/api/deposits/consent/googledrive/health
```

**Expected Response**:
```json
{
  "success": true,
  "message": "GoogleDriveConsentController is accessible",
  "timestamp": "2024-01-20T...",
  "storageType": "Google Drive",
  "storageLocation": "DepositManager/BankConsent",
  "authenticationRequired": false
}
```

### Step 4: Test Upload
**Request**:
```http
POST http://localhost:5000/api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "REQ0000000001",
  "bankId": "TEST001",
  "fileName": "test_consent.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PC9UeXBlL1BhZ2UvUGFyZW50IDIgMCBSL1Jlc291cmNlczw8L0ZvbnQ8PC9GMSA0IDAgUj4+Pj4vTWVkaWFCb3hbMCAwIDYxMiA3OTJdL0NvbnRlbnRzIDUgMCBSPj4KZW5kb2JqCjQgMCBvYmoKPDwvVHlwZS9Gb250L1N1YnR5cGUvVHlwZTEvQmFzZUZvbnQvVGltZXMtUm9tYW4+PgplbmRvYmoKNSAwIG9iago8PC9MZW5ndGggNDQ+PnN0cmVhbQpCVAovRjEgMTggVGYKMTAwIDcwMCBUZAooVGVzdCBDb25zZW50IERvY3VtZW50KSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCjIgMCBvYmoKPDwvVHlwZS9QYWdlcy9Db3VudCAxL0tpZHNbMyAwIFJdPj4KZW5kb2JqCjEgMCBvYmoKPDwvVHlwZS9DYXRhbG9nL1BhZ2VzIDIgMCBSPj4KZW5kb2JqCnhyZWYKMCA2CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDI4MSAwMDAwMCBuIAowMDAwMDAwMjMwIDAwMDAwIG4gCjAwMDAwMDAwMTUgMDAwMDAgbiAKMDAwMDAwMDEyNiAwMDAwMCBuIAowMDAwMDAwMTk1IDAwMDAwIG4gCnRyYWlsZXIKPDwvU2l6ZSA2L1Jvb3QgMSAwIFI+PgpzdGFydHhyZWYKMzMwCiUlRU9G",
  "fileSize": 330,
  "contentType": "application/pdf"
}
```

**Expected Response**:
```json
{
  "success": true,
  "message": "Consent document uploaded successfully to Google Drive",
  "data": {
    "fileName": "test_consent_20240120103045.pdf",
    "storagePath": "DepositManager/BankConsent/REQ0000000001/TEST001/...",
    "downloadUrl": "/api/deposits/consent/googledrive/download?..."
  }
}
```

### Step 5: Verify in Google Drive
1. Open: https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
2. Navigate to: `DepositManager/BankConsent/REQ0000000001/TEST001/`
3. File should be present: `test_consent_YYYYMMDDHHMMSS.pdf`

---

## ? Common Issues

### Issue 1: "Permission denied" (403 Forbidden)
**Cause**: Folder not shared with service account  
**Fix**: Follow "How to Share the Folder" above

### Issue 2: Folder shared but still getting errors
**Cause**: Wrong permission level  
**Fix**: Ensure role is **Editor**, not Viewer

### Issue 3: "Folder not found"
**Cause**: Folder ID mismatch  
**Fix**: Verify folder ID in Web.config matches URL

### Issue 4: Service account email not found
**Cause**: Wrong credentials file  
**Fix**: Check `GoogleDrive\service-account-credentials.json` exists

---

## ?? Expected Folder Structure in Google Drive

After successful upload, your folder will look like:

```
?? Your Shared Folder (1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp)
??? ?? DepositManager (Created by API)
    ??? ?? BankConsent (Created by API)
        ??? ?? REQ0000000001 (Created by API)
            ??? ?? TEST001 (Created by API)
                ??? ?? test_consent_20240120103045.pdf (Uploaded)
```

**Note**: 
- API creates folder structure automatically
- Only the root folder needs to be shared
- Subfolders inherit permissions

---

## ?? Configuration Summary

| Setting | Value | Status |
|---------|-------|--------|
| Shared Folder ID | `1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp` | ? Configured |
| Folder URL | https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp | ? Valid |
| Web.config Updated | Yes | ? Complete |
| Build Status | Success | ? Passed |
| Folder Shared | **YOUR ACTION** | ?? **PENDING** |

---

## ?? Quick Command Reference

### Check Service Account Email
```powershell
# PowerShell
Get-Content "GoogleDrive\service-account-credentials.json" | ConvertFrom-Json | Select-Object -ExpandProperty client_email
```

### Check Logs
```powershell
# PowerShell - View latest log
Get-Content "C:\smkcapi_published\Logs\FtpLog_$(Get-Date -Format 'yyyyMMdd').txt" -Tail 50
```

### Test with cURL
```bash
# Health Check
curl -X GET "http://localhost:5000/api/deposits/consent/googledrive/health"

# Upload Test
curl -X POST "http://localhost:5000/api/deposits/consent/googledrive/upload" \
  -H "Content-Type: application/json" \
  -d @test_upload.json
```

---

## ?? Documentation References

| Document | Purpose |
|----------|---------|
| `Docs\GoogleDrive_QuickStart.md` | Quick reference guide |
| `Docs\GoogleDrive_ServiceAccount_Setup.md` | Complete setup instructions |
| `Docs\GoogleDrive_Troubleshooting.md` | Problem resolution |
| `Postman\GoogleDriveConsentController.postman_collection.json` | API test collection |

---

## ? Next Steps

1. **NOW**: Share the folder with service account (see above)
2. **THEN**: Restart application
3. **TEST**: Use Postman collection
4. **VERIFY**: Check logs and Google Drive

---

**Configuration Date**: January 2024  
**Folder ID**: 1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp  
**Status**: ? Ready for Testing (after sharing folder)

---

## ?? You're Almost There!

Just one step left: **Share the folder with your service account!**

Then you'll be ready to upload files to Google Drive! ??
