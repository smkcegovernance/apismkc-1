# ?? Service Account Storage Quota Error - FIXED

## Error Message
```
System.InvalidOperationException: Failed to upload file to Google Drive: 
File upload failed: The service drive has thrown an exception. 
HttpStatusCode is Forbidden. Service Accounts do not have storage quota.
```

## ?? Root Cause
**Service accounts cannot upload to their own drive** - they have ZERO storage quota.

## ? Solution Implemented

### What Changed

#### 1. Added Shared Folder Support
**File**: `Services\DepositManager\GoogleDriveStorageService.cs`

- ? Added `_sharedFolderId` configuration field
- ? Modified folder operations to use shared folder as root
- ? Added `SupportsAllDrives = true` to all Drive API requests
- ? Enhanced logging to show shared folder usage

#### 2. Added Configuration Setting
**File**: `Web.config`

```xml
<!-- NEW: Configure the shared folder ID -->
<add key="GoogleDrive_SharedFolderId" value="YOUR_FOLDER_ID_HERE" />
```

#### 3. Documentation Created
- ? `Docs\GoogleDrive_ServiceAccount_Setup.md` - Complete setup guide
- ? `Docs\GoogleDrive_Troubleshooting.md` - Updated with this issue
- ? Instructions for obtaining folder ID and sharing

---

## ?? Quick Fix Steps (5 Minutes)

### 1. Create Folder in Google Drive
- Log into Google Drive with a **real user account**
- Create folder: `SMKC_API_Storage`

### 2. Get Folder ID
- Open the folder
- Copy ID from URL: `https://drive.google.com/drive/folders/FOLDER_ID_HERE`

### 3. Share with Service Account
- Right-click folder ? Share
- Add service account email (from credentials JSON: `client_email`)
- Grant **Editor** permission
- Click Send

### 4. Update Web.config
```xml
<add key="GoogleDrive_SharedFolderId" value="PASTE_FOLDER_ID_HERE" />
```

### 5. Restart Application
- Restart IIS or application pool
- Test upload with Postman

---

## ?? Verification

### 1. Check Logs
Location: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

**Should see:**
```
=== Google Drive Storage Service Initialized ===
Authentication Type: Service Account (Silent)
Shared Folder ID: 1a2b3c4d5e6f7g8h9i0j
Upload Mode: Shared Folder (Service Account compatible)
```

### 2. Test Upload
Use Postman collection: `POST /api/deposits/consent/googledrive/upload`

**Expected Response:**
```json
{
  "success": true,
  "message": "Consent document uploaded successfully to Google Drive",
  "data": {
    "fileName": "consent_document_20240120103045.pdf",
    "storagePath": "DepositManager/BankConsent/REQ0000000001/BANK001/..."
  }
}
```

### 3. Verify in Google Drive
- Open your shared folder
- Navigate to: `SMKC_API_Storage/DepositManager/BankConsent/`
- File should be present

---

## ?? Technical Details

### How It Works Now

**Before (BROKEN):**
```
Service Account ? Tries to upload to own drive ? ? No storage quota!
```

**After (WORKING):**
```
Service Account ? Uploads to shared user folder ? ? Uses user's quota!
```

### Code Changes

#### Constructor Initialization
```csharp
// NEW: Get shared folder ID configuration
_sharedFolderId = ConfigurationManager.AppSettings["GoogleDrive_SharedFolderId"];

// Warn if not configured
if (_useServiceAccount && string.IsNullOrEmpty(_sharedFolderId))
{
    FtpLogger.LogWarning("? Service Account without Shared Folder ID!");
}
```

#### Folder Creation
```csharp
// Use shared folder as root if configured
if (_useServiceAccount && !string.IsNullOrEmpty(_sharedFolderId))
{
    folderMetadata.Parents = new List<string> { _sharedFolderId };
}
```

#### All API Requests
```csharp
request.SupportsAllDrives = true;
request.IncludeItemsFromAllDrives = true;
```

---

## ?? Important Notes

### Storage Quota
- Files use the **real user's** Google Drive quota (not service account)
- Service account acts as a "robot" with permission to upload

### Folder Structure
```
User's Google Drive
??? SMKC_API_Storage (You create & share)
    ??? DepositManager (API creates)
        ??? BankConsent (API creates)
            ??? {requirementId} (API creates)
                ??? {bankId} (API creates)
                    ??? {file.pdf} (API uploads)
```

### Permissions Required
- Service account needs **Editor** role on shared folder
- **Viewer** won't work (read-only)
- **Owner** transfer not recommended

---

## ?? If Still Not Working

### Error: "Folder not found"
- ? Check folder ID is correct in Web.config
- ? Folder exists in Google Drive
- ? Folder is shared with service account

### Error: "Permission denied"
- ? Service account email is exact match from credentials JSON
- ? Role is **Editor** (not Viewer)
- ? Google Drive API is enabled in Cloud Console

### Error: Empty folder ID warning in logs
- ? Add folder ID to Web.config: `GoogleDrive_SharedFolderId`
- ? Restart application

### Files not appearing in Google Drive
- ? Check you're looking in the correct shared folder
- ? Navigate to full path: `SMKC_API_Storage/DepositManager/BankConsent/...`
- ? Check logs for actual upload path

---

## ?? Related Documentation

| Document | Purpose |
|----------|---------|
| `GoogleDrive_ServiceAccount_Setup.md` | Complete setup instructions |
| `GoogleDrive_Troubleshooting.md` | Common issues and solutions |
| `GoogleDrive_Upload_Fix.md` | Previous upload issue fix |
| `GoogleDriveConsentController_README.md` | API usage guide |

---

## ? Resolution Status

- [x] Root cause identified
- [x] Code changes implemented
- [x] Configuration added
- [x] Build successful
- [x] Documentation created
- [x] Setup guide written
- [x] Testing instructions provided
- [ ] **Configuration needed**: Add folder ID to Web.config
- [ ] **Testing needed**: Verify with your Google Drive folder

---

## ?? Next Steps

1. **Create Google Drive folder** (if not already done)
2. **Get folder ID** from the URL
3. **Share folder** with service account (Editor permission)
4. **Update Web.config** with folder ID
5. **Restart application**
6. **Test upload** with Postman
7. **Verify file** appears in Google Drive

**Estimated Time**: 5-10 minutes

---

**Status**: ? **CODE FIXED - CONFIGURATION REQUIRED**  
**Fixed By**: AI Assistant  
**Date**: January 2024  
**Version**: 1.1
