# Network Share Configuration Guide

## Direct Access to Admin Share

The consent document storage now directly accesses the network admin share without needing additional share setup.

### Configuration Details

**Network Path**: `\\192.168.40.47\c$\inetpub\ftproot\BankConsents`

**Credentials**:
- Username: `administrator`
- Password: `smkc@1234`

### Web.config Settings

```xml
<add key="Storage_Type" value="network" />
<add key="Network_Server" value="192.168.40.47" />
<add key="Network_Share" value="c$\inetpub\ftproot\BankConsents" />
<add key="Ftp_User" value="administrator" />
<add key="Ftp_Password" value="smkc@1234" />
```

### How It Works

1. **Upload**: Files are uploaded to `\\192.168.40.47\c$\inetpub\ftproot\BankConsents\{requirementId}\{bankId}\{fileName}`
2. **Download**: Files are downloaded from the same path structure
3. **Authentication**: Uses Windows admin share authentication with provided credentials
4. **No Additional Setup**: No need to create custom shares - uses built-in admin share (c$)

### Directory Structure

```
\\192.168.40.47\c$\inetpub\ftproot\BankConsents\
??? REQ0000000001\
?   ??? BANK001\
?   ?   ??? consent_document.pdf
?   ?   ??? agreement.pdf
?   ??? BANK002\
?       ??? consent_form.pdf
??? REQ0000000002\
    ??? BANK001\
        ??? authorization.pdf
```

### Testing the Connection

You can test the connection from the API server using PowerShell:

```powershell
# Test network connection
Test-NetConnection -ComputerName 192.168.40.47 -Port 445

# Access the admin share
$cred = Get-Credential -UserName "administrator" -Message "Enter password"
New-PSDrive -Name "TestDrive" -PSProvider FileSystem -Root "\\192.168.40.47\c$\inetpub\ftproot\BankConsents" -Credential $cred

# List contents
Get-ChildItem TestDrive:\

# Remove test drive
Remove-PSDrive -Name "TestDrive"
```

### Security Considerations

1. **Admin Share Access**: The `c$` admin share requires administrator credentials
2. **Network Security**: Ensure SMB/CIFS (port 445) is open between API server and 192.168.40.47
3. **Credential Protection**: Credentials are stored in Web.config (consider encryption for production)
4. **Firewall Rules**: Verify Windows Firewall allows File and Printer Sharing

### Troubleshooting

#### Error: "Access Denied"
- Verify administrator credentials are correct
- Check if UAC is blocking admin share access
- Ensure the account has permissions on the target directory

#### Error: "Network path not found"
- Verify server 192.168.40.47 is reachable
- Check if SMB/CIFS protocol is enabled on both machines
- Test with: `ping 192.168.40.47` and `Test-NetConnection -ComputerName 192.168.40.47 -Port 445`

#### Error: "Directory not found"
- Verify the path `C:\inetpub\ftproot\BankConsents` exists on 192.168.40.47
- Create the directory if it doesn't exist

### Alternative: Local Path (If API runs on same server)

If the API is deployed on 192.168.40.47, you can use local path for better performance:

```xml
<!-- Comment out network share settings and use: -->
<add key="Network_LocalPath" value="C:\inetpub\ftproot\BankConsents" />
```

This bypasses network authentication and uses local file system access.

### API Endpoints

#### Upload Consent Document
```http
POST /api/deposits/bank/{bankId}/requirements/{requirementId}/quote
Content-Type: application/json

{
  "ConsentDocument": {
    "FileName": "consent_form.pdf",
    "FileData": "base64_encoded_pdf_content..."
  }
}
```

#### Download Consent Document
```http
GET /api/deposits/consent/download?requirementId=REQ001&bankId=BANK001&fileName=consent_form.pdf
Accept: application/pdf
```

### Logging

All network storage operations are logged to:
- **Location**: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`
- **Details**: Connection attempts, file operations, errors

Check logs for detailed troubleshooting information.
