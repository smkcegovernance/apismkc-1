# Google Drive API - Troubleshooting Guide

## Common Issues and Solutions

This document provides solutions to common issues when working with the Google Drive API integration.

---

## ?? Issue: "File upload failed - no response received"

### Problem
The upload process completes but the API doesn't return file information.

### Root Cause
The `Upload()` method completes successfully, but the `ResponseBody` is sometimes null, especially with larger files or slower connections.

### Solution (IMPLEMENTED)
The code now:
1. ? Checks the upload status explicitly
2. ? Handles cases where `ResponseBody` is null
3. ? Falls back to finding the file by name if needed
4. ? Provides detailed logging at each step

### Code Fix Location
**File**: `Services\DepositManager\GoogleDriveStorageService.cs`  
**Method**: `UploadFile()`  
**Lines**: ~500-550

---

## ?? Issue: "Credentials file not found"

### Problem
```
FileNotFoundException: Credentials file not found at: GoogleDrive\credentials.json
```

### Solution
1. **Check Web.config** settings:
   ```xml
   <add key="GoogleDrive_CredentialsPath" value="GoogleDrive\credentials.json" />
   ```

2. **Verify file exists**:
   - Path: `C:\Users\ACER\source\repos\smkcegovernance\apismkc\GoogleDrive\credentials.json`
   - Or published path: `C:\smkcapi_published\GoogleDrive\credentials.json`

3. **Ensure file is copied to output**:
   - Right-click `credentials.json` in Visual Studio
   - Properties ? Copy to Output Directory: "Copy if newer"

4. **Check file format**:
   - Service Account: JSON with `private_key`, `client_email`, `type: "service_account"`
   - OAuth 2.0: JSON with `client_id`, `client_secret`, `redirect_uris`

---

## ?? Issue: "OAuth 2.0 authentication failed"

### Problem
Browser-based authentication fails or times out.

### Solution Options

#### Option 1: Use Service Account (RECOMMENDED for servers)
1. Update `Web.config`:
   ```xml
   <add key="GoogleDrive_UseServiceAccount" value="true" />
   ```

2. Use Service Account credentials file (see setup guide below)

#### Option 2: Pre-authorize OAuth 2.0
1. Run application on a machine with browser access first
2. Complete authorization flow
3. Copy `token.json` folder to server
4. Token will be reused for subsequent requests

---

## ?? Issue: "Access Denied" or "Insufficient Permission"

### Problem
```
403 Forbidden: Insufficient Permission
```

### Solutions

#### For Service Account:
1. **Enable Google Drive API** in Google Cloud Console
2. **Share target folder** with service account email:
   - Find email in credentials: `"client_email": "xxx@xxx.iam.gserviceaccount.com"`
   - Share Google Drive folder with this email
   - Grant "Editor" or "Owner" permissions

#### For OAuth 2.0:
1. Verify scopes in code include:
   ```csharp
   DriveService.Scope.DriveFile
   ```

2. Re-authorize if scopes changed:
   - Delete `token.json` folder
   - Restart application
   - Complete authorization flow again

---

## ?? Issue: "The upload failed with status: Failed"

### Problem
Upload starts but fails during transfer.

### Possible Causes & Solutions

1. **Network Issues**
   - Check internet connectivity
   - Check firewall rules (allow Google Drive API)
   - Verify proxy settings if behind corporate network

2. **File Size Issues**
   - Google Drive API has limits
   - For large files (>5MB), consider using resumable uploads
   - Check quota limits in Google Cloud Console

3. **Invalid Credentials**
   - Regenerate credentials in Google Cloud Console
   - Update credentials.json file
   - Restart application

4. **API Limits**
   - Google Drive API has rate limits
   - Check quota usage in Google Cloud Console
   - Implement retry logic with exponential backoff

---

## ?? Issue: File Uploaded but Can't Download

### Problem
Upload succeeds, but download returns "File not found".

### Solution
1. **Check folder structure** matches exactly:
   ```
   My Drive/
   ??? DepositManager/
       ??? BankConsent/
           ??? {requirementId}/
               ??? {bankId}/
                   ??? {fileName}
   ```

2. **Verify file ownership**:
   - Service account must own or have access to the file
   - Check sharing permissions

3. **Check logs** for actual path used:
   ```
   C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt
   ```

---

## ?? Issue: "Service Account authentication failed"

### Problem
Service account credentials are rejected.

### Verification Steps

1. **Validate JSON format**:
   ```json
   {
     "type": "service_account",
     "project_id": "your-project-id",
     "private_key_id": "...",
     "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
     "client_email": "xxx@xxx.iam.gserviceaccount.com",
     "client_id": "...",
     "auth_uri": "https://accounts.google.com/o/oauth2/auth",
     "token_uri": "https://oauth2.googleapis.com/token",
     "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
     "client_x509_cert_url": "..."
   }
   ```

2. **Check private key format**:
   - Must include `-----BEGIN PRIVATE KEY-----` and `-----END PRIVATE KEY-----`
   - Must have `\n` characters preserved
   - No extra spaces or line breaks

3. **Verify service account is enabled**:
   - Google Cloud Console ? IAM & Admin ? Service Accounts
   - Check status is "Enabled"

4. **Enable required APIs**:
   - Google Drive API must be enabled in project

---

## ?? Issue: "Service Accounts do not have storage quota"

### Problem
```
403 Forbidden: Service Accounts do not have storage quota. 
Leverage shared drives or use OAuth delegation instead.
```

### Root Cause
**Service accounts cannot upload files to their own Google Drive storage** because they don't have storage quota. This is by design from Google.

### Solution (IMPLEMENTED)
Upload to a **user's shared folder** instead. The service account needs permission to access a real user's Google Drive folder.

### Steps to Fix

#### 1. Create a Folder in Your Google Drive
1. Log into Google Drive with a **real user account** (not the service account)
2. Create a new folder (e.g., "SMKC_API_Storage")
3. This folder will be the root for all API uploads

#### 2. Get the Folder ID
1. Open the folder in Google Drive
2. Look at the URL: `https://drive.google.com/drive/folders/1a2b3c4d5e6f7g8h9i0j`
3. Copy the folder ID: `1a2b3c4d5e6f7g8h9i0j`

#### 3. Share the Folder with Service Account
1. Right-click the folder ? **Share**
2. Add the service account email:
   - Find it in `service-account-credentials.json`
   - Look for: `"client_email": "xxx@xxx.iam.gserviceaccount.com"`
3. Grant **Editor** permissions
4. Click **Send**

#### 4. Update Web.config
```xml
<add key="GoogleDrive_SharedFolderId" value="1a2b3c4d5e6f7g8h9i0j" />
```

#### 5. Restart Application
- Restart IIS or your application
- The service account will now upload to the shared folder

### Verification
1. Test upload with Postman
2. Check the shared folder in Google Drive
3. Files should appear in: `SMKC_API_Storage/DepositManager/BankConsent/...`
4. Check logs: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

---

## ?? Setup Guide: Service Account (Recommended)

### Step 1: Create Service Account

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select or create a project
3. Navigate to **IAM & Admin** ? **Service Accounts**
4. Click **Create Service Account**
5. Enter details:
   - Name: `smkc-drive-service`
   - Description: `Service account for SMKC API Google Drive access`
6. Click **Create and Continue**
7. Skip role assignment (we'll use folder sharing)
8. Click **Done**

### Step 2: Generate Credentials

1. Click on the created service account
2. Go to **Keys** tab
3. Click **Add Key** ? **Create new key**
4. Select **JSON** format
5. Click **Create**
6. Save the downloaded JSON file as `credentials.json`

### Step 3: Configure Application

1. Copy `credentials.json` to `GoogleDrive\credentials.json` in your project
2. Update `Web.config`:
   ```xml
   <add key="GoogleDrive_UseServiceAccount" value="true" />
   <add key="GoogleDrive_CredentialsPath" value="GoogleDrive\credentials.json" />
   ```

### Step 4: Share Google Drive Folder

1. Open Google Drive
2. Create folder structure: `DepositManager/BankConsent/`
3. Right-click on `DepositManager` folder ? **Share**
4. Add the service account email (from credentials.json: `client_email`)
5. Grant **Editor** permissions
6. Click **Send**

### Step 5: Enable API

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Navigate to **APIs & Services** ? **Library**
3. Search for "Google Drive API"
4. Click **Enable**

### Step 6: Test

1. Build and run your application
2. Test with the Health Check endpoint
3. Try uploading a test file
4. Check logs: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

---

## ?? Configuration Reference

### Web.config Settings

```xml
<appSettings>
  <!-- Google Drive Configuration -->
  
  <!-- Path to credentials file (relative or absolute) -->
  <add key="GoogleDrive_CredentialsPath" value="GoogleDrive\credentials.json" />
  
  <!-- Application name shown in Google Drive API -->
  <add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
  
  <!-- Use Service Account (true) or OAuth 2.0 (false) -->
  <add key="GoogleDrive_UseServiceAccount" value="true" />
</appSettings>
```

### Authentication Types Comparison

| Feature | Service Account | OAuth 2.0 |
|---------|----------------|-----------|
| User interaction | ? None | ? Required (first time) |
| Best for | Servers, automation | Desktop apps, user-facing |
| Setup complexity | Medium | Low |
| Folder access | Via sharing | User's drive only |
| Token management | Automatic | Manual (token.json) |
| **Recommended for API** | ? **YES** | ? No |

---

## ?? Monitoring and Debugging

### Check Detailed Logs

All operations are logged to:
```
C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt
```

Log entries include:
- ? Initialization details
- ? Authentication type used
- ? Folder creation/finding operations
- ? File upload progress
- ? Download operations
- ? Error messages with stack traces

### Enable Verbose Logging

The code already includes detailed logging. To view:

1. Open log file: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`
2. Search for your operation (e.g., "Upload", "Download")
3. Look for step-by-step progress
4. Check for error messages

### Test API Endpoints

Use the provided Postman collection:
- Location: `Postman\GoogleDriveConsentController.postman_collection.json`
- See: `Postman\GoogleDriveConsentController_README.md`

Test sequence:
1. **Health Check** - Verify service is running
2. **Upload** - Test file upload
3. **Get Info** - Verify file exists
4. **Download** - Test file retrieval

---

## ?? Error Codes Reference

| Error Code | HTTP Status | Meaning | Solution |
|------------|-------------|---------|----------|
| `NULL_REQUEST` | 400 | Request body is null | Send valid JSON body |
| `MISSING_PARAMETER` | 400 | Required field missing | Check all required fields |
| `INVALID_ARGUMENT` | 400 | Invalid parameter value | Verify parameter format |
| `INVALID_OPERATION` | 400 | Operation failed | Check logs for details |
| `FILE_NOT_FOUND` | 404 | File doesn't exist | Verify file was uploaded |
| `STORAGE_ERROR` | 500 | Upload/download failed | Check connectivity, credentials |
| `SERVER_ERROR` | 500 | Unexpected error | Check logs and server status |

---

## ?? Getting Help

1. **Check Logs First**: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`
2. **Review this guide**: Common issues covered above
3. **Test with Postman**: Use provided collection to isolate issues
4. **Google Cloud Console**: Check API quotas and limits
5. **Service Account**: Verify sharing and permissions

---

## ? Quick Checklist

Before deploying to production:

- [ ] Service Account created and credentials downloaded
- [ ] `credentials.json` file in correct location
- [ ] Google Drive API enabled in Google Cloud Console
- [ ] Service account email added to Google Drive folder with Editor access
- [ ] `Web.config` settings updated
- [ ] Application built and deployed successfully
- [ ] Health check endpoint returns success
- [ ] Test upload completes successfully
- [ ] Test download retrieves file correctly
- [ ] Logs are being written to expected location
- [ ] Error handling tested with invalid inputs

---

**Last Updated**: January 2024  
**Version**: 1.0  
**Framework**: .NET Framework 4.6.2
