# Google Drive Service Account Setup - Silent Authentication

## ?? Overview

This guide shows how to set up **Service Account credentials** for **silent, automatic authentication** with Google Drive API - **NO USER INTERACTION REQUIRED**.

Perfect for:
- ? Server applications
- ? Automated processes
- ? Production environments
- ? IIS/Windows Services
- ? Headless servers

---

## ?? What You'll Get

With Service Account:
- ? **No browser authorization needed**
- ? **No user interaction required**
- ? **Works in background services**
- ? **Perfect for production**
- ? **No token expiration issues**
- ? **Fully automated**

---

## ?? Step-by-Step: Get Service Account Credentials

### Step 1: Go to Google Cloud Console

```
https://console.cloud.google.com/
```

Sign in with your Google account.

---

### Step 2: Create or Select Project

**Option A: Create New Project**
1. Click project dropdown at top
2. Click **"New Project"**
3. Name: `SMKC-DepositManager`
4. Click **"Create"**
5. Wait and select the project

**Option B: Use Existing Project**
- Select your existing project from dropdown

---

### Step 3: Enable Google Drive API

1. Go to **"APIs & Services"** ? **"Library"**

   Direct link:
```
https://console.cloud.google.com/apis/library
```

2. Search: **"Google Drive API"**

3. Click on **"Google Drive API"**

4. Click **"Enable"**

---

### Step 4: Create Service Account

1. Go to **"IAM & Admin"** ? **"Service Accounts"**

   Direct link:
```
https://console.cloud.google.com/iam-admin/serviceaccounts
```

2. Click **"+ CREATE SERVICE ACCOUNT"** at top

3. **Service account details:**
   - Service account name: `smkc-depositmanager-sa`
   - Service account ID: (auto-generated: `smkc-depositmanager-sa`)
   - Description: `Service account for SMKC Deposit Manager API - automated Google Drive access`

4. Click **"Create and Continue"**

5. **Grant this service account access to project:**
   - Role: Select **"Editor"**
   - Or create custom role with these permissions:
     - `drive.files.create`
     - `drive.files.delete`
     - `drive.files.get`
     - `drive.files.list`

6. Click **"Continue"**

7. **Grant users access to this service account (Optional)**
   - Skip this step
   - Click **"Done"**

---

### Step 5: Create Service Account Key (JSON)

1. You'll see your service account in the list

2. Click on the **service account email** (looks like):
```
smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
```

3. Go to **"Keys"** tab

4. Click **"Add Key"** ? **"Create new key"**

5. Select key type: **"JSON"**

6. Click **"Create"**

7. **File downloads automatically** with name like:
```
your-project-id-abc123def456.json
```

8. **Rename this file to:**
```
service-account-credentials.json
```

---

### Step 6: Verify JSON File Content

Open the downloaded file - it should look like:

```json
{
  "type": "service_account",
  "project_id": "smkc-depositmanager-123456",
  "private_key_id": "abc123def456...",
  "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQE...\n-----END PRIVATE KEY-----\n",
  "client_email": "smkc-depositmanager-sa@smkc-depositmanager-123456.iam.gserviceaccount.com",
  "client_id": "123456789012345678901",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/smkc-depositmanager-sa%40smkc-depositmanager-123456.iam.gserviceaccount.com"
}
```

? **Key fields to verify:**
- `"type": "service_account"` ?
- `"private_key"` contains `-----BEGIN PRIVATE KEY-----` ?
- `"client_email"` is the service account email ?

---

### Step 7: Create Drive Folder and Share It

**IMPORTANT:** Service accounts can only access files/folders explicitly shared with them.

#### Option A: Create Folder in Your Drive and Share

1. **Open Google Drive** (with your personal/admin account):
```
https://drive.google.com/
```

2. **Create folder structure:**
   - Create folder: `DepositManager`
   - Inside that, it will auto-create: `BankConsent/`
   
   Or just create: `DepositManager` (sub-folders created automatically by API)

3. **Right-click** on `DepositManager` folder ? **Share**

4. **Add the service account email:**
```
smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
```

5. **Set permission:** `Editor`

6. **Uncheck** "Notify people"

7. Click **"Share"**

? **Done!** Service account now has access to this folder.

#### Option B: Use Shared Drive (if you have Google Workspace)

1. Create a Shared Drive
2. Add service account email as member with Editor access
3. Update code to use Shared Drive ID

---

## ?? Where to Place Credentials

### Step 1: Copy File to Server

Place `service-account-credentials.json` in:

**Development:**
```
C:\Users\ACER\source\repos\smkcegovernance\apismkc\service-account-credentials.json
```

**Production (IIS):**
```
C:\inetpub\wwwroot\smkcapi\service-account-credentials.json
```

Or your custom path.

---

### Step 2: Update Web.config

Open `Web.config` and add/update these settings:

```xml
<configuration>
  <appSettings>
    <!-- Existing settings... -->
    
    <!-- Google Drive Service Account Configuration -->
    <add key="GoogleDrive_CredentialsPath" value="C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" />
    <add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
    <add key="GoogleDrive_UseServiceAccount" value="true" />
  </appSettings>
</configuration>
```

**Key Setting:**
```xml
<add key="GoogleDrive_UseServiceAccount" value="true" />
```

This tells the application to use **Service Account** (silent) instead of OAuth 2.0 (user authorization).

---

## ?? Secure the Credentials File

### Set File Permissions (PowerShell as Administrator)

```powershell
# Grant IIS_IUSRS read access
icacls "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" /grant:r "IIS_IUSRS:(R)"

# Remove inheritance
icacls "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" /inheritance:r

# Grant NETWORK SERVICE read access (if using IIS)
icacls "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" /grant:r "NETWORK SERVICE:(R)"
```

---

### Add to .gitignore (CRITICAL!)

**NEVER commit service account credentials to Git!**

Add to `.gitignore`:
```
# Google Drive Credentials
credentials.json
service-account-credentials.json
token.json
*.json
!package.json
!tsconfig.json
```

If already committed, remove:
```sh
git rm --cached service-account-credentials.json
git commit -m "Remove service account credentials from repository"
```

---

## ? Verification

### Check if Service Account is Active

Run these checks:

#### 1. Verify File Exists
```powershell
Test-Path "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json"
# Should return: True
```

#### 2. Verify File Content
```powershell
$json = Get-Content "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" | ConvertFrom-Json
$json.type
# Should return: service_account

$json.client_email
# Should return: smkc-depositmanager-sa@...
```

#### 3. Verify Web.config
```powershell
[xml]$config = Get-Content "C:\inetpub\wwwroot\smkcapi\Web.config"
$config.configuration.appSettings.add | Where-Object {$_.key -eq "GoogleDrive_UseServiceAccount"} | Select-Object value
# Should return: value=true
```

---

## ?? Testing

### Test 1: Health Check

```http
GET http://localhost/api/deposits/consent/googledrive/health
```

**Expected Response:**
```json
{
  "success": true,
  "message": "GoogleDriveConsentController is accessible",
  "storageType": "Google Drive",
  "storageLocation": "DepositManager/BankConsent"
}
```

---

### Test 2: Upload Test File

```http
POST http://localhost/api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "TEST001",
  "bankId": "TESTBANK",
  "fileName": "test_consent.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PC9UeXBlL0NhdGFsb2cvUGFnZXMgMiAwIFI+PgplbmRvYmoKMiAwIG9iago8PC9UeXBlL1BhZ2VzL0tpZHNbMyAwIFJdL0NvdW50IDE+PgplbmRvYmoKMyAwIG9iago8PC9UeXBlL1BhZ2UvTWVkaWFCb3hbMCAwIDYxMiA3OTJdL1Jlc291cmNlczw8L0ZvbnQ8PC9GMSA0IDAgUj4+Pj4vQ29udGVudHMgNSAwIFI+PgplbmRvYmoKNCAwIG9iago8PC9UeXBlL0ZvbnQvU3VidHlwZS9UeXBlMS9CYXNlRm9udC9IZWx2ZXRpY2E+PgplbmRvYmoKNSAwIG9iago8PC9MZW5ndGggNDQ+PgpzdHJlYW0KQlQKL0YxIDI0IFRmCjEwMCA3MDAgVGQKKFRlc3QgRG9jdW1lbnQpIFRqCkVUCmVuZHN0cmVhbQplbmRvYmoKeHJlZgowIDYKMDAwMDAwMDAwMCA2NTUzNSBmDQowMDAwMDAwMDE1IDAwMDAwIG4NCjAwMDAwMDAwNzQgMDAwMDAgbg0KMDAwMDAwMDEyNCAwMDAwMCBuDQowMDAwMDAwMjQxIDAwMDAwIG4NCjAwMDAwMDAzMjAgMDAwMDAgbg0KdHJhaWxlcgo8PC9TaXplIDYvUm9vdCAxIDAgUj4+CnN0YXJ0eHJlZgo0MTQKJSVFT0Y=",
  "fileSize": 1024,
  "contentType": "application/pdf",
  "uploadedBy": "system"
}
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Consent document uploaded successfully to Google Drive",
  "data": {
    "fileName": "test_consent.pdf",
    "requirementId": "TEST001",
    "bankId": "TESTBANK",
    "storagePath": "DepositManager/BankConsent/TEST001/TESTBANK/test_consent.pdf"
  }
}
```

---

### Test 3: Check Application Logs

```powershell
# Check logs
Get-Content "C:\inetpub\wwwroot\smkcapi\Logs\FtpLog_*.txt" -Tail 50
```

**Look for:**
```
=== Google Drive Storage Service Initialized ===
Authentication Type: Service Account (Silent)
? Service Account authentication successful (silent)
? No user interaction required for authentication
```

---

### Test 4: Verify Folder Creation in Drive

1. Go to Google Drive (with account that has folder access)
2. Navigate to: `DepositManager/BankConsent/TEST001/TESTBANK/`
3. You should see: `test_consent.pdf`

---

## ?? Troubleshooting

### Error: "Failed to load credentials"

**Cause:** File not found or invalid JSON

**Solution:**
```powershell
# Check file exists
Test-Path "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json"

# Check JSON is valid
Get-Content "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" | ConvertFrom-Json
```

---

### Error: "Access denied" or "Permission denied"

**Cause:** Service account doesn't have access to Drive folder

**Solution:**
1. Open Google Drive
2. Find `DepositManager` folder
3. Right-click ? Share
4. Add service account email with Editor permission:
```
smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
```

---

### Error: "The service driveService was not authenticated"

**Cause:** Web.config not configured correctly

**Solution:**
Verify Web.config has:
```xml
<add key="GoogleDrive_UseServiceAccount" value="true" />
<add key="GoogleDrive_CredentialsPath" value="C:\full\path\to\service-account-credentials.json" />
```

---

### Files Not Appearing in Drive

**Cause:** Service account creates files in its own "My Drive"

**Solution:**
You need to share a folder FROM your personal Drive TO the service account:
1. Create folder in YOUR Drive
2. Share it with service account email
3. Service account will create files in that shared folder

---

## ?? Comparison: Service Account vs OAuth 2.0

| Feature | Service Account | OAuth 2.0 |
|---------|----------------|-----------|
| **User Interaction** | ? No | ? Yes (browser) |
| **Silent Authentication** | ? Yes | ? No |
| **Production Ready** | ? Yes | ?? Limited |
| **Headless Servers** | ? Yes | ? No |
| **Windows Services** | ? Yes | ? No |
| **IIS Applications** | ? Yes | ?? Limited |
| **Setup Complexity** | Medium | Low |
| **Token Management** | ? Automatic | Manual refresh |
| **Access Scope** | Shared folders only | User's entire Drive |
| **Best For** | Production servers | Desktop apps |

---

## ?? Service Account Email Format

Your service account email looks like:
```
{service-account-name}@{project-id}.iam.gserviceaccount.com
```

Example:
```
smkc-depositmanager-sa@smkc-depositmanager-123456.iam.gserviceaccount.com
```

**Where to find it:**
1. Google Cloud Console ? IAM & Admin ? Service Accounts
2. Or in `service-account-credentials.json` ? `client_email` field

---

## ?? Quick Summary

### What You Need from Google Cloud:

1. ? Service Account created
2. ? Service Account key (JSON) downloaded
3. ? Google Drive API enabled
4. ? Drive folder shared with service account email

### What You Need in Your App:

1. ? `service-account-credentials.json` placed in app folder
2. ? Web.config updated with:
   - `GoogleDrive_CredentialsPath`
   - `GoogleDrive_UseServiceAccount = true`
3. ? File permissions set for IIS_IUSRS
4. ? Added to `.gitignore`

---

## ?? Useful Links

- **Service Accounts Guide:** https://cloud.google.com/iam/docs/service-accounts
- **Google Cloud Console:** https://console.cloud.google.com/
- **Service Accounts Page:** https://console.cloud.google.com/iam-admin/serviceaccounts
- **Drive API Scopes:** https://developers.google.com/drive/api/guides/api-specific-auth

---

## ? Final Checklist

Before going to production:

- [ ] Service account created in Google Cloud
- [ ] Google Drive API enabled
- [ ] Service account key (JSON) downloaded
- [ ] File renamed to `service-account-credentials.json`
- [ ] File placed in application directory
- [ ] Web.config updated with correct path
- [ ] `GoogleDrive_UseServiceAccount` set to `true`
- [ ] File permissions set (IIS_IUSRS read access)
- [ ] Added to `.gitignore`
- [ ] Drive folder created and shared with service account
- [ ] Health endpoint tested
- [ ] Upload tested
- [ ] Download tested
- [ ] Logs reviewed for "Service Account (Silent)" message
- [ ] Files visible in Drive folder

---

**You're now ready for silent, automatic Google Drive authentication!** ??

No browser popups, no user interaction, works perfectly in production!
