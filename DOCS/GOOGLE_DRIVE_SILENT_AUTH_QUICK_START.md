# Quick Reference: Silent Google Drive Authentication

## ? What Was Changed

Your `GoogleDriveStorageService.cs` now supports **two authentication modes**:

### 1. Service Account (Silent - No User Interaction) ? **RECOMMENDED**
- No browser authorization
- Works automatically
- Perfect for production

### 2. OAuth 2.0 (User Authorization)
- Requires browser
- User must approve
- Good for testing/development

---

## ?? Quick Setup for Silent Authentication

### 1. Get Service Account Credentials

Go to: https://console.cloud.google.com/

**Quick steps:**
1. Create project (or select existing)
2. Enable Google Drive API
3. IAM & Admin ? Service Accounts ? Create Service Account
4. Name: `smkc-depositmanager-sa`
5. Keys tab ? Add Key ? Create new key ? JSON
6. Download file ? Rename to `service-account-credentials.json`

**?? Detailed guide:** `DOCS\GOOGLE_DRIVE_SERVICE_ACCOUNT_SETUP.md`

---

### 2. Share Drive Folder with Service Account

1. Open Google Drive
2. Create folder: `DepositManager`
3. Right-click ? Share
4. Add service account email (from JSON file `client_email` field):
```
smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
```
5. Role: Editor
6. Uncheck "Notify people"
7. Click Share

---

### 3. Configure Web.config

```xml
<appSettings>
  <!-- Google Drive Service Account (Silent Authentication) -->
  <add key="GoogleDrive_CredentialsPath" value="C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" />
  <add key="GoogleDrive_ApplicationName" value="SMKC Deposit Manager API" />
  <add key="GoogleDrive_UseServiceAccount" value="true" />
</appSettings>
```

**Key setting:**
```xml
<add key="GoogleDrive_UseServiceAccount" value="true" />
```
- `true` = Service Account (silent, no user interaction) ?
- `false` or omitted = OAuth 2.0 (requires user authorization)

---

### 4. Place Credentials File

**Production:**
```
C:\inetpub\wwwroot\smkcapi\service-account-credentials.json
```

**Development:**
```
C:\Users\ACER\source\repos\smkcegovernance\apismkc\service-account-credentials.json
```

---

### 5. Set File Permissions

```powershell
# Grant IIS read access
icacls "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" /grant:r "IIS_IUSRS:(R)"
icacls "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json" /inheritance:r
```

---

### 6. Add to .gitignore

```
service-account-credentials.json
credentials.json
token.json
```

---

## ?? How It Works

### When `GoogleDrive_UseServiceAccount = true`:

```csharp
// Your application reads the setting
_useServiceAccount = true;

// Calls InitializeServiceAccount() method
// No browser needed ?
// No user interaction ?
// Works silently in background ?

GoogleCredential credential = GoogleCredential.FromStream(stream)
    .CreateScoped(Scopes);
    
// Creates DriveService automatically
// Ready to upload/download files!
```

**Result:**
- Application starts
- Authenticates automatically
- No browser windows
- No user prompts
- Works perfectly in IIS/Windows Services

---

## ?? Service Account JSON Format

Your `service-account-credentials.json` should look like:

```json
{
  "type": "service_account",
  "project_id": "your-project-id",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com",
  "client_id": "123456789...",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/..."
}
```

**Key field:** `"type": "service_account"` ?

---

## ?? Testing

### Test Upload (Silent Authentication)

```http
POST http://localhost/api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "REQ001",
  "bankId": "BANK001",
  "fileName": "test.pdf",
  "fileData": "base64_encoded_content...",
  "fileSize": 1024,
  "contentType": "application/pdf"
}
```

**Expected:** Works immediately, no browser popup! ?

---

### Check Logs

```powershell
Get-Content "C:\inetpub\wwwroot\smkcapi\Logs\FtpLog_*.txt" -Tail 20
```

**Look for:**
```
=== Google Drive Storage Service Initialized ===
Authentication Type: Service Account (Silent)
? Service Account authentication successful (silent)
? No user interaction required for authentication
```

---

## ?? Important Notes

### Service Account Can Only Access:
- ? NOT your personal Drive files
- ? NOT other users' files
- ? ONLY folders explicitly shared with it

**Solution:** Share the `DepositManager` folder with service account email.

---

### Folder Sharing is REQUIRED

Service accounts work in their own isolated space. To use your Drive:

1. Create folder in YOUR Drive: `DepositManager`
2. Share it with service account email
3. Service account creates files IN that shared folder
4. You can see files in YOUR Drive

---

## ?? Troubleshooting

### Error: "Access denied"

**Fix:** Share Drive folder with service account email
```
smkc-depositmanager-sa@your-project-id.iam.gserviceaccount.com
```

### Error: "Credentials file not found"

**Fix:** Check path in Web.config matches actual file location
```powershell
Test-Path "C:\inetpub\wwwroot\smkcapi\service-account-credentials.json"
```

### Error: "Invalid credentials"

**Fix:** Verify JSON file has `"type": "service_account"`

---

## ?? Quick Comparison

| Authentication Type | User Interaction | Best For |
|---------------------|-----------------|----------|
| **Service Account** | ? No | Production, IIS, Windows Services |
| **OAuth 2.0** | ? Yes (browser) | Desktop apps, development |

---

## ?? Summary

### For Silent Authentication (No User Interaction):

1. ? Get service account credentials (JSON file)
2. ? Share Drive folder with service account email
3. ? Set `GoogleDrive_UseServiceAccount = true` in Web.config
4. ? Place credentials file in app directory
5. ? Done! No browser, no prompts, fully automated

### Files You Need:

- `service-account-credentials.json` - From Google Cloud Console
- `Web.config` - Updated with settings

### Files You DON'T Need:

- ? `credentials.json` (OAuth 2.0 only)
- ? `token.json` (OAuth 2.0 only)

---

## ?? Full Documentation

- **Detailed Setup:** `DOCS\GOOGLE_DRIVE_SERVICE_ACCOUNT_SETUP.md`
- **OAuth 2.0 Setup:** `DOCS\GOOGLE_DRIVE_CREDENTIALS_GUIDE.md`
- **Implementation Guide:** `DOCS\GOOGLE_DRIVE_INTEGRATION_GUIDE.md`

---

**You're ready for silent, automatic Google Drive authentication!** ??

No browser popups. No user interaction. Works perfectly in production.
