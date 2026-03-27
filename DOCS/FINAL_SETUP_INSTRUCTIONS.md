# ?? FINAL SETUP - Copy & Share

## ? Configuration Complete!

Your Google Drive integration is **configured** and **ready to test**.

---

## ?? Your Service Account Email

**Copy this email exactly:**

```
smkc-analytics@smkc-website.iam.gserviceaccount.com
```

---

## ?? Your Shared Folder

**Folder URL:**
```
https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
```

**Folder ID:**
```
1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
```

---

## ?? NEXT STEP: Share the Folder (2 Minutes)

### Step-by-Step:

1. **Open the folder**:
   - Click: https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp

2. **Click Share** (top-right button)

3. **Add service account**:
   - Paste email: `smkc-analytics@smkc-website.iam.gserviceaccount.com`
   - Press Enter

4. **Set Permission**:
   - Change role dropdown to: **Editor**
   - (NOT "Viewer" - must be Editor!)

5. **Disable Notification**:
   - **Uncheck** "Notify people"
   - (Service accounts don't read emails)

6. **Click Send**

7. **Verify**:
   - Service account email appears in "People with access"
   - Role shows: **Editor**

---

## ? That's It! Now Test

### Restart Application
```
Restart IIS or Application Pool
```

### Test with Postman

**1. Health Check:**
```http
GET http://localhost:5000/api/deposits/consent/googledrive/health
```

**Expected**: `200 OK`

**2. Upload Test:**
```http
POST http://localhost:5000/api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "REQ0000000001",
  "bankId": "TEST001",
  "fileName": "test.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PC9UeXBlL1BhZ2UvUGFyZW50IDIgMCBSL1Jlc291cmNlczw8L0ZvbnQ8PC9GMSA0IDAgUj4+Pj4vTWVkaWFCb3hbMCAwIDYxMiA3OTJdL0NvbnRlbnRzIDUgMCBSPj4KZW5kb2JqCjQgMCBvYmoKPDwvVHlwZS9Gb250L1N1YnR5cGUvVHlwZTEvQmFzZUZvbnQvVGltZXMtUm9tYW4+PgplbmRvYmoKNSAwIG9iago8PC9MZW5ndGggNDQ+PnN0cmVhbQpCVAovRjEgMTggVGYKMTAwIDcwMCBUZAooVGVzdCBDb25zZW50IERvY3VtZW50KSBUagpFVAplbmRzdHJlYW0KZW5kb2JqCjIgMCBvYmoKPDwvVHlwZS9QYWdlcy9Db3VudCAxL0tpZHNbMyAwIFJdPj4KZW5kb2JqCjEgMCBvYmoKPDwvVHlwZS9DYXRhbG9nL1BhZ2VzIDIgMCBSPj4KZW5kb2JqCnhyZWYKMCA2CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDI4MSAwMDAwMCBuIAowMDAwMDAwMjMwIDAwMDAwIG4gCjAwMDAwMDAwMTUgMDAwMDAgbiAKMDAwMDAwMDEyNiAwMDAwMCBuIAowMDAwMDAwMTk1IDAwMDAwIG4gCnRyYWlsZXIKPDwvU2l6ZSA2L1Jvb3QgMSAwIFI+PgpzdGFydHhyZWYKMzMwCiUlRU9G"
}
```

**Expected**: `201 Created` with file details

**3. Check Google Drive:**
- Open your folder
- Look for: `DepositManager/BankConsent/REQ0000000001/TEST001/test.pdf`

---

## ?? Configuration Summary

| Item | Value | Status |
|------|-------|--------|
| **Service Account Email** | `smkc-analytics@smkc-website.iam.gserviceaccount.com` | ? Identified |
| **Shared Folder ID** | `1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp` | ? Configured |
| **Web.config Updated** | Yes | ? Complete |
| **Build Status** | Success | ? Passed |
| **Folder Shared** | **YOUR ACTION** | ? **PENDING** |

---

## ?? If Upload Fails

### Check These:

1. **Folder is shared?**
   - Service account email in "People with access"
   - Permission is **Editor**

2. **Check logs:**
   ```
   C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt
   ```

3. **Restart after sharing:**
   - Must restart application after sharing folder

---

## ?? Documentation

- **Full Setup Guide**: `Docs\GoogleDrive_ServiceAccount_Setup.md`
- **Troubleshooting**: `Docs\GoogleDrive_Troubleshooting.md`
- **Quick Reference**: `Docs\GoogleDrive_QuickStart.md`
- **Postman Tests**: `Postman\GoogleDriveConsentController.postman_collection.json`

---

## ?? You're Ready!

1. ? Configuration complete
2. ? Build successful
3. ? Folder ID configured
4. ? **Share folder with service account** ? DO THIS NOW
5. ? Restart application
6. ? Test upload

**Estimated Time Remaining**: 2-3 minutes

---

**Service Account**: smkc-analytics@smkc-website.iam.gserviceaccount.com  
**Folder ID**: 1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp  
**Status**: ? Awaiting folder sharing  
**Next**: Share folder ? Restart ? Test

---

Good luck! ??
