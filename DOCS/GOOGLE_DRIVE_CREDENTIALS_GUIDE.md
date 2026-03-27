# Google Drive API Credentials - Quick Reference

## Required Credentials Summary

### 1. OAuth 2.0 Desktop Application (Recommended for Development)

**What you need from Google Cloud Console:**

#### credentials.json
```json
{
  "installed": {
    "client_id": "YOUR_CLIENT_ID.apps.googleusercontent.com",
    "project_id": "your-project-id",
    "auth_uri": "https://accounts.google.com/o/oauth2/auth",
    "token_uri": "https://oauth2.googleapis.com/token",
    "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
    "client_secret": "YOUR_CLIENT_SECRET",
    "redirect_uris": ["http://localhost"]
  }
}
```

**Location:** `C:\smkcapi_published\credentials.json`

#### token.json (Auto-generated after first authorization)
```json
{
  "access_token": "ya29.a0AfH6SMBx...",
  "refresh_token": "1//0gFfP7N4...",
  "token_type": "Bearer",
  "expires_in": 3599,
  "scope": "https://www.googleapis.com/auth/drive.file"
}
```

**Location:** `C:\smkcapi_published\token.json`

---

### 2. Service Account (Recommended for Production)

**What you need from Google Cloud Console:**

#### service-account-credentials.json
```json
{
  "type": "service_account",
  "project_id": "your-project-id",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBg...\n-----END PRIVATE KEY-----\n",
  "client_email": "smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com",
  "client_id": "123456789...",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/..."
}
```

**Location:** `C:\smkcapi_published\service-account-credentials.json`

---

## How to Get Credentials

### Option 1: OAuth 2.0 Desktop App (Step-by-Step)

1. **Go to Google Cloud Console**
   - URL: https://console.cloud.google.com/

2. **Create Project**
   - Click "Select a project" dropdown
   - Click "New Project"
   - Name: `SMKC-DepositManager`
   - Click "Create"

3. **Enable Google Drive API**
   - Navigation Menu ? "APIs & Services" ? "Library"
   - Search: "Google Drive API"
   - Click on it
   - Click "Enable"

4. **Configure OAuth Consent Screen**
   - "APIs & Services" ? "OAuth consent screen"
   - User Type: Select "Internal" (for Google Workspace) or "External"
   - Click "Create"
   - Fill in:
     - App name: `SMKC Deposit Manager API`
     - User support email: `your-email@domain.com`
     - Developer contact: `your-email@domain.com`
   - Click "Save and Continue"
   
5. **Add Scopes**
   - Click "Add or Remove Scopes"
   - Manually add scope: `https://www.googleapis.com/auth/drive.file`
   - Or select: "Google Drive API" ? `.../auth/drive.file`
   - Click "Update"
   - Click "Save and Continue"

6. **Create OAuth 2.0 Client ID**
   - "APIs & Services" ? "Credentials"
   - Click "Create Credentials"
   - Select "OAuth client ID"
   - Application type: "Desktop app"
   - Name: `SMKC-DepositManager-Desktop`
   - Click "Create"

7. **Download Credentials**
   - Click the download icon (?) next to your Client ID
   - File downloads as: `client_secret_XXXXX.json`
   - Rename to: `credentials.json`
   - Copy to: `C:\smkcapi_published\credentials.json`

---

### Option 2: Service Account (Step-by-Step)

1. **Follow Steps 1-3 from Option 1** (Create project and enable API)

2. **Create Service Account**
   - "IAM & Admin" ? "Service Accounts"
   - Click "Create Service Account"
   - Service account name: `smkc-depositmanager-sa`
   - Service account ID: (auto-generated)
   - Description: "Service account for SMKC Deposit Manager API"
   - Click "Create and Continue"

3. **Grant Permissions**
   - Role: Select "Editor" or create custom role with these permissions:
     - `drive.files.create`
     - `drive.files.delete`
     - `drive.files.get`
     - `drive.files.list`
   - Click "Continue"
   - Click "Done"

4. **Create Key**
   - Click on the service account you just created
   - Go to "Keys" tab
   - Click "Add Key" ? "Create new key"
   - Select "JSON"
   - Click "Create"
   - File downloads as: `your-project-id-abc123.json`
   - Rename to: `service-account-credentials.json`
   - Copy to: `C:\smkcapi_published\service-account-credentials.json`

5. **Share Drive Folder** (Important!)
   - Open Google Drive
   - Create folder: `DepositManager`
   - Right-click folder ? Share
   - Add service account email: 
     ```
     smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
     ```
   - Role: "Editor"
   - Uncheck "Notify people"
   - Click "Share"

---

## Web.config Configuration

### For OAuth 2.0 Desktop App:

```xml
<appSettings>
  <!-- Google Drive Configuration -->
  <add key="GoogleDrive_CredentialsPath" value="C:\smkcapi_published\credentials.json" />
  <add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
</appSettings>
```

### For Service Account:

```xml
<appSettings>
  <!-- Google Drive Configuration -->
  <add key="GoogleDrive_CredentialsPath" value="C:\smkcapi_published\service-account-credentials.json" />
  <add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
  <add key="GoogleDrive_UseServiceAccount" value="true" />
</appSettings>
```

---

## First-Time Setup

### OAuth 2.0 Desktop App:

1. Place `credentials.json` in application folder
2. Configure Web.config
3. Start the application
4. **Browser will open automatically** with authorization prompt
5. Sign in with Google account that has Drive access
6. Click "Allow" to grant permissions
7. `token.json` will be created automatically
8. Subsequent requests use the token automatically

**Important:** First authorization **MUST** be done on the server where API runs, with an active user session.

### Service Account:

1. Place `service-account-credentials.json` in application folder
2. Configure Web.config
3. Share Drive folder with service account email
4. Start the application
5. **No browser authorization needed** - works immediately
6. Perfect for headless/production servers

---

## File Permissions

Set appropriate file permissions for security:

```powershell
# For credentials.json or service-account-credentials.json
icacls "C:\smkcapi_published\credentials.json" /grant:r "IIS_IUSRS:(R)"
icacls "C:\smkcapi_published\credentials.json" /inheritance:r

# For token.json (if using OAuth 2.0)
icacls "C:\smkcapi_published\token.json" /grant:r "IIS_IUSRS:(R,W)"
icacls "C:\smkcapi_published\token.json" /inheritance:r
```

---

## Security Best Practices

### ? DO:
- Store credentials outside of web root if possible
- Use service accounts for production
- Set restrictive file permissions
- Add credentials files to `.gitignore`
- Use minimal OAuth scopes (`drive.file`)
- Rotate credentials periodically
- Monitor API usage in Google Cloud Console

### ? DON'T:
- Commit credentials to source control
- Share credentials via email or chat
- Use production credentials in development
- Grant more permissions than needed
- Store credentials in plain text in database
- Use personal Google accounts for production

---

## Troubleshooting Credentials

### Error: "Credentials file not found"
**Check:**
- File exists at path specified in Web.config
- File name is correct: `credentials.json` or `service-account-credentials.json`
- Path uses double backslashes: `C:\\path\\to\\file`
- Application has read permissions to the file

**Fix:**
```powershell
# Verify file exists
Test-Path "C:\smkcapi_published\credentials.json"

# Check permissions
icacls "C:\smkcapi_published\credentials.json"
```

---

### Error: "Invalid credentials" or "Authentication failed"
**Check:**
- Credentials JSON file is valid JSON format
- Client ID and Client Secret are correct
- For OAuth 2.0: Token hasn't been revoked
- For Service Account: Private key is intact

**Fix:**
1. Re-download credentials from Google Cloud Console
2. Delete `token.json` (for OAuth 2.0) and re-authorize
3. Verify API is enabled in Google Cloud Console

---

### Error: "Access denied" or "Permission denied"
**Check:**
- OAuth scopes include `https://www.googleapis.com/auth/drive.file`
- For Service Account: Folder is shared with service account email
- For Service Account: Service account has "Editor" role

**Fix:**
```
1. Google Drive ? Right-click folder ? Share
2. Add: smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
3. Role: Editor
4. Share
```

---

### Error: "Token expired" or "Token refresh failed"
**Check:**
- Refresh token is present in `token.json`
- Application has internet access to reach Google OAuth servers
- OAuth client hasn't been deleted or disabled

**Fix:**
1. Delete `token.json`
2. Re-authorize application
3. New token will be generated

---

## Credentials Checklist

Before deploying to production:

- [ ] Google Cloud project created
- [ ] Google Drive API enabled
- [ ] OAuth consent screen configured (if using OAuth 2.0)
- [ ] Credentials created and downloaded
- [ ] Credentials file placed in correct location
- [ ] Web.config updated with correct path
- [ ] File permissions set correctly
- [ ] `.gitignore` includes credentials files
- [ ] First authorization completed (for OAuth 2.0)
- [ ] Drive folder shared with service account (for Service Account)
- [ ] Test upload successful
- [ ] Test download successful
- [ ] Logs showing successful operations

---

## Quick Start Commands

### Download Credentials (via gcloud CLI)
```bash
# Install gcloud CLI first: https://cloud.google.com/sdk/docs/install

# Login
gcloud auth login

# Set project
gcloud config set project your-project-id

# Download service account key
gcloud iam service-accounts keys create service-account-credentials.json \
  --iam-account=smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
```

### Test Credentials Validity
```powershell
# Test if file is valid JSON
Get-Content "C:\smkcapi_published\credentials.json" | ConvertFrom-Json

# Test if service account email is correct
$creds = Get-Content "C:\smkcapi_published\service-account-credentials.json" | ConvertFrom-Json
$creds.client_email
```

---

## Support Resources

- **Google Cloud Console:** https://console.cloud.google.com/
- **Drive API Documentation:** https://developers.google.com/drive/api/v3/about-sdk
- **OAuth 2.0 Guide:** https://developers.google.com/identity/protocols/oauth2
- **Service Account Guide:** https://cloud.google.com/iam/docs/service-accounts
- **API Quotas:** https://console.cloud.google.com/apis/api/drive.googleapis.com/quotas

---

## Summary

### What You Need:

**For Development (OAuth 2.0):**
1. `credentials.json` - Downloaded from Google Cloud Console
2. `token.json` - Auto-generated after first authorization
3. Browser access for first-time authorization

**For Production (Service Account):**
1. `service-account-credentials.json` - Downloaded from Google Cloud Console
2. Drive folder shared with service account email
3. No browser authorization needed

### Where to Get Them:
- **Google Cloud Console:** https://console.cloud.google.com/
- **Credentials Page:** https://console.cloud.google.com/apis/credentials

### Where to Put Them:
- Application Folder: `C:\smkcapi_published\`
- Configure path in Web.config

### How to Secure Them:
- Restrict file permissions to IIS_IUSRS
- Add to `.gitignore`
- Never commit to source control
- Use service accounts for production

---

**You're ready to integrate Google Drive! Follow the step-by-step guide above to get your credentials.** ??
