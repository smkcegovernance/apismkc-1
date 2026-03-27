# Google Drive Service Account - Quick Setup Guide

## ?? Important: Service Account Storage Limitation

**Service accounts DO NOT have their own storage quota!**

You cannot upload files directly to a service account's drive. Instead, you must:
1. Create a folder in a **real user's Google Drive**
2. **Share** that folder with the service account
3. Configure the **folder ID** in Web.config

---

## ?? Complete Setup (Step-by-Step)

### Step 1: Create Service Account (Google Cloud Console)

1. **Go to Google Cloud Console**
   - URL: https://console.cloud.google.com/

2. **Select or Create Project**
   - Click project dropdown at top
   - Select existing project or click "New Project"

3. **Create Service Account**
   - Navigate to: **IAM & Admin** ? **Service Accounts**
   - Click **+ CREATE SERVICE ACCOUNT**
   - Fill in details:
     ```
     Service account name: smkc-drive-service
     Service account ID: smkc-drive-service (auto-generated)
     Description: Service account for SMKC API Google Drive access
     ```
   - Click **CREATE AND CONTINUE**
   - Skip role assignment (we'll use folder sharing)
   - Click **DONE**

4. **Generate Credentials JSON**
   - Click on the newly created service account
   - Go to **KEYS** tab
   - Click **ADD KEY** ? **Create new key**
   - Select **JSON** format
   - Click **CREATE**
   - Save the downloaded file as `service-account-credentials.json`

5. **Copy Service Account Email**
   - From the keys page, copy the email address
   - Format: `smkc-drive-service@project-id.iam.gserviceaccount.com`
   - **You'll need this email in Step 3!**

### Step 2: Enable Google Drive API

1. **In Google Cloud Console**
   - Navigate to: **APIs & Services** ? **Library**

2. **Search for "Google Drive API"**
   - Click on **Google Drive API**
   - Click **ENABLE**
   - Wait for confirmation

### Step 3: Create & Share Google Drive Folder

#### 3.1 Create Folder (Use Real User Account!)

1. **Log into Google Drive**
   - Use a **real Google account** (NOT the service account)
   - URL: https://drive.google.com/

2. **Create New Folder**
   - Click **+ New** ? **Folder**
   - Name: `SMKC_API_Storage` (or any name you prefer)
   - Click **CREATE**

#### 3.2 Get Folder ID

1. **Open the folder** you just created

2. **Copy Folder ID from URL**
   - URL format: `https://drive.google.com/drive/folders/FOLDER_ID_HERE`
   - Example: `https://drive.google.com/drive/folders/1a2b3c4d5e6f7g8h9i0j`
   - Copy just the ID part: `1a2b3c4d5e6f7g8h9i0j`

#### 3.3 Share with Service Account

1. **Right-click on the folder** ? **Share**

2. **Add service account email**
   - Paste the service account email from Step 1.5
   - Example: `smkc-drive-service@project-id.iam.gserviceaccount.com`

3. **Set Permissions**
   - Change role to: **Editor**
   - ? Editor allows: Create, Read, Update, Delete files

4. **Uncheck "Notify people"** (service accounts don't read emails)

5. **Click SEND**

6. **Verify Sharing**
   - The service account email should appear in the "People with access" list
   - Permission: Editor

### Step 4: Configure Application

#### 4.1 Copy Credentials File

1. **Copy `service-account-credentials.json`** to your project:
   ```
   C:\Users\ACER\source\repos\smkcegovernance\apismkc\GoogleDrive\service-account-credentials.json
   ```

2. **Set File Properties** (in Visual Studio):
   - Right-click file ? **Properties**
   - **Build Action**: Content
   - **Copy to Output Directory**: Copy if newer

#### 4.2 Update Web.config

Open `Web.config` and update these settings:

```xml
<!-- Google Drive Configuration -->
<add key="GoogleDrive_UseServiceAccount" value="true" />
<add key="GoogleDrive_CredentialsPath" value="GoogleDrive\service-account-credentials.json" />
<add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />

<!-- CRITICAL: Add the Folder ID from Step 3.2 -->
<add key="GoogleDrive_SharedFolderId" value="1a2b3c4d5e6f7g8h9i0j" />
```

**Replace** `1a2b3c4d5e6f7g8h9i0j` with **YOUR** actual folder ID!

### Step 5: Build & Deploy

1. **Build Solution**
   ```
   Visual Studio ? Build ? Rebuild Solution
   ```

2. **Check for Errors**
   - Ensure build succeeds with no errors

3. **Deploy to Server**
   - Copy build output to: `C:\smkcapi_published\`
   - Ensure `GoogleDrive` folder and credentials file are copied
   - Restart IIS or application pool

### Step 6: Test the Setup

#### 6.1 Test with Postman

1. **Import Collection**
   - File: `Postman\GoogleDriveConsentController.postman_collection.json`

2. **Set Environment Variable**
   - Variable: `base_url`
   - Value: Your API URL (e.g., `http://localhost:5000`)

3. **Test Health Check**
   ```
   GET /api/deposits/consent/googledrive/health
   ```
   - Should return status `200 OK`
   - Check `authenticationRequired: false`

4. **Test Upload**
   ```
   POST /api/deposits/consent/googledrive/upload
   ```
   - Use sample request from collection
   - Should return `201 Created`
   - Check response for `fileName` and `storagePath`

#### 6.2 Verify in Google Drive

1. **Open your shared folder** in Google Drive
2. **Navigate to**: `SMKC_API_Storage/DepositManager/BankConsent/`
3. **Check for uploaded file** in appropriate subfolder

#### 6.3 Check Logs

1. **Open log file**:
   ```
   C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt
   ```

2. **Look for**:
   ```
   === Google Drive Storage Service Initialized ===
   Authentication Type: Service Account (Silent)
   Shared Folder ID: 1a2b3c4d5e6f7g8h9i0j
   Upload Mode: Shared Folder (Service Account compatible)
   ```

3. **Check upload logs**:
   ```
   [STEP 1] Getting/Creating root folder: DepositManager
   [STEP 2] Getting/Creating sub folder: BankConsent
   ...
   Upload completed successfully!
   ```

---

## ? Verification Checklist

Before going to production, verify:

- [ ] Service account created in Google Cloud Console
- [ ] Service account credentials JSON downloaded
- [ ] Google Drive API enabled in project
- [ ] Folder created in real user's Google Drive
- [ ] Folder ID copied correctly
- [ ] Folder shared with service account email as **Editor**
- [ ] Credentials file in `GoogleDrive\service-account-credentials.json`
- [ ] `Web.config` updated with folder ID
- [ ] Application builds without errors
- [ ] Health check returns success
- [ ] Test upload succeeds
- [ ] File visible in Google Drive shared folder
- [ ] Logs show successful operations

---

## ?? Common Mistakes

### ? Mistake 1: Forgot to Share Folder
**Symptom**: `403 Forbidden` error  
**Solution**: Share the folder with service account email as Editor

### ? Mistake 2: Wrong Folder ID
**Symptom**: Files not appearing in expected location  
**Solution**: Double-check folder ID from Google Drive URL

### ? Mistake 3: Empty Folder ID in Web.config
**Symptom**: Warning in logs, uploads fail  
**Solution**: Add the folder ID to `GoogleDrive_SharedFolderId` setting

### ? Mistake 4: Shared Folder Then Deleted
**Symptom**: `404 Not Found` errors  
**Solution**: Don't delete or move the shared folder

### ? Mistake 5: Wrong Service Account Email
**Symptom**: `403 Forbidden` error  
**Solution**: Use exact email from `client_email` field in credentials JSON

---

## ?? Architecture Overview

```
Your Application (Service Account)
    ?
    Uploads to...
    ?
Real User's Google Drive
    ??? SMKC_API_Storage (Shared Folder) ? You configure this ID
        ??? DepositManager (Created by API)
            ??? BankConsent (Created by API)
                ??? REQ0000000001 (Created by API)
                    ??? BANK001 (Created by API)
                        ??? consent_document.pdf (Uploaded file)
```

### Key Points:
1. **SMKC_API_Storage**: You create this manually and share it
2. **DepositManager** onwards: API creates these automatically
3. **Service account** needs Editor permission on SMKC_API_Storage
4. **All files** are stored in the real user's quota, not service account

---

## ?? Troubleshooting

### Issue: "Service Accounts do not have storage quota"
**Status**: ? FIXED with shared folder approach  
**Solution**: Follow this guide completely

### Issue: "Folder not found" 
**Check**:
1. Folder ID is correct in Web.config
2. Folder exists in Google Drive
3. Folder is shared with service account
4. Service account has Editor permission

### Issue: "Permission denied"
**Check**:
1. Service account email is exactly from credentials JSON
2. Folder is shared with **Editor** role (not just Viewer)
3. Google Drive API is enabled

### More Help
- See: `Docs\GoogleDrive_Troubleshooting.md`
- Check logs: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

---

## ?? Support

If you encounter issues:

1. **Check logs first**: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`
2. **Review this setup guide**: Follow all steps carefully
3. **Verify folder sharing**: Ensure service account has Editor access
4. **Test with Postman**: Use provided collection to isolate issues
5. **Check Google Cloud Console**: Verify API is enabled

---

**Last Updated**: January 2024  
**Version**: 1.0  
**Framework**: .NET Framework 4.6.2
