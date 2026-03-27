# ?? Google Drive - Quick Reference Card

## ? 5-Minute Setup Checklist

```
? 1. Create service account in Google Cloud Console
? 2. Download credentials JSON
? 3. Create folder in your Google Drive
? 4. Share folder with service account (Editor role)
? 5. Copy folder ID from URL
? 6. Add folder ID to Web.config
? 7. Restart application
? 8. Test with Postman
```

---

## ?? Critical Configuration

### Web.config (MUST CONFIGURE!)
```xml
<!-- THIS IS REQUIRED! -->
<add key="GoogleDrive_SharedFolderId" value="PASTE_YOUR_FOLDER_ID_HERE" />
```

### How to Get Folder ID
1. Open folder in Google Drive
2. URL: `https://drive.google.com/drive/folders/1a2b3c4d5e6f7g8h9i0j`
3. Copy: `1a2b3c4d5e6f7g8h9i0j`

### Service Account Email (for Sharing)
Find in `service-account-credentials.json`:
```json
{
  "client_email": "xxx@xxx.iam.gserviceaccount.com"
}
```

---

## ?? File Locations

```
Project Root
??? GoogleDrive\
?   ??? service-account-credentials.json  ? Put credentials here
??? Web.config                            ? Add folder ID here
??? Docs\
    ??? GoogleDrive_ServiceAccount_Setup.md ? Full setup guide
```

---

## ?? Quick Test

### 1. Health Check
```http
GET /api/deposits/consent/googledrive/health
```
**Expected**: `200 OK` with service info

### 2. Upload Test
```http
POST /api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "REQ0000000001",
  "bankId": "TEST001",
  "fileName": "test.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MK..."
}
```
**Expected**: `201 Created` with file details

### 3. Verify in Google Drive
```
Your Folder ? DepositManager ? BankConsent ? REQ0000000001 ? TEST001 ? test.pdf
```

---

## ?? Common Errors & Quick Fixes

### ? "Service Accounts do not have storage quota"
**Fix**: Add `GoogleDrive_SharedFolderId` to Web.config

### ? "Credentials file not found"
**Fix**: Copy JSON to `GoogleDrive\service-account-credentials.json`

### ? "Permission denied" / 403 Forbidden
**Fix**: Share folder with service account as **Editor**

### ? "Folder not found" / 404 Not Found
**Fix**: Check folder ID is correct in Web.config

---

## ?? Service Status Check

### Logs Location
```
C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt
```

### Good Initialization Looks Like
```
=== Google Drive Storage Service Initialized ===
Authentication Type: Service Account (Silent)
Shared Folder ID: 1a2b3c4d5e6f7g8h9i0j
Upload Mode: Shared Folder (Service Account compatible)
? Google Drive service initialized successfully
```

### Warning Signs
```
? WARNING: Service Account without Shared Folder ID configured!
? Service Accounts cannot upload to their own drive.
```
**Action**: Add folder ID to Web.config immediately!

---

## ?? Quick Links

| Resource | Link |
|----------|------|
| **Full Setup Guide** | `Docs\GoogleDrive_ServiceAccount_Setup.md` |
| **Troubleshooting** | `Docs\GoogleDrive_Troubleshooting.md` |
| **Postman Tests** | `Postman\GoogleDriveConsentController.postman_collection.json` |
| **Google Cloud Console** | https://console.cloud.google.com/ |
| **Google Drive** | https://drive.google.com/ |

---

## ?? Key Facts

| Fact | Value |
|------|-------|
| Service Account Storage | **0 GB** (NONE!) |
| Solution | Upload to shared folder |
| Required Permission | **Editor** role |
| Configuration Required | Folder ID in Web.config |
| User Interaction | **None** (fully automated) |
| Browser Required | **No** (works on servers) |

---

## ?? Environment Variables

```xml
<!-- Required -->
<add key="GoogleDrive_SharedFolderId" value="YOUR_FOLDER_ID" />

<!-- Optional (defaults shown) -->
<add key="GoogleDrive_UseServiceAccount" value="true" />
<add key="GoogleDrive_CredentialsPath" value="GoogleDrive\service-account-credentials.json" />
<add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
```

---

## ?? Pre-Deployment Checklist

```
Configuration:
? SharedFolderId in Web.config (NOT EMPTY!)
? Credentials file in GoogleDrive\ folder
? Web.config has correct paths

Google Cloud:
? Service account created
? Google Drive API enabled
? Credentials downloaded

Google Drive:
? Folder created
? Folder shared with service account
? Permission is Editor (not Viewer)

Testing:
? Build succeeds
? Health check works
? Upload test succeeds
? File visible in Google Drive
? Logs show no errors
```

---

## ?? Success Indicators

? Health check: `200 OK`  
? Upload: `201 Created`  
? File in Google Drive: ? Visible  
? Logs: No errors or warnings  
? Download: Returns file correctly  

---

## ?? Need Help?

1. **Read**: `Docs\GoogleDrive_ServiceAccount_Setup.md`
2. **Check Logs**: `C:\smkcapi_published\Logs\`
3. **Test**: Use Postman collection
4. **Verify**: Folder sharing in Google Drive

---

**Quick Setup Time**: ?? 5-10 minutes  
**Complexity**: ?? Easy (with this guide)  
**Status**: ? Production Ready

---

_Last Updated: January 2024_
