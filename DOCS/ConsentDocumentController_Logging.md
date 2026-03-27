# Consent Document Controller - Comprehensive Logging

## Overview
The `ConsentDocumentController` now includes comprehensive file-based logging for all operations using the `FtpLogger` service. All events are logged to the `Logs` folder in the application directory.

## ?? IMPORTANT: NO AUTHENTICATION REQUIRED
**This controller allows plain access without API key authentication.**
- No `X-API-Key`, `X-Timestamp`, or `X-Signature` headers required
- No authentication tokens needed
- Simple HTTP GET requests work directly
- Client IP addresses are logged for security monitoring

## Log File Location
- **Directory**: `C:\smkcapi_published\Logs\`
- **File Pattern**: `FtpLog_YYYYMMDD.txt` (e.g., `FtpLog_20240115.txt`)
- **Rotation**: Automatic daily rotation (new file created each day)

## Logged Endpoints

### 1. Health Check Endpoint
**Route**: `GET /api/deposits/consent/health`

**Authentication**: ? None Required

**Logged Events**:
- Request received
- Controller accessibility status
- Success/failure status
- Configuration information

### 2. Download Consent Document
**Route**: `GET /api/deposits/consent/downloadconsent`

**Authentication**: ? None Required

**Logged Events**:
```
================================================================================
=== CONSENT DOWNLOAD REQUEST START - RequestId: {guid} ===
================================================================================
Client IP: {ip_address}
RequestId: {guid}
Timestamp: {timestamp}
Authentication: NONE (Plain Access)
--- Request Parameters ---
  RequirementId: {value}
  QuoteId: {value}
  BankId: {value}
  FileName: {value}

STEP 1: Validating request parameters - ? SUCCESS
  Details: All required parameters present

STEP 2: Preparing to download file from storage - ? SUCCESS
  Target FileName: {file}
  RequirementId: {id}
  BankId: {id}

STEP 3: Calling FTP Storage Service - ? SUCCESS
  Details: Downloading: {file} for {req}/{bank}
  File retrieved, Base64 size: {size} chars

STEP 4: Determining response format - ? SUCCESS
  Accept Header: {header}
  Response Format: Binary PDF / JSON with Base64

STEP 5: Preparing binary/JSON response - ? SUCCESS
  File size: {bytes} bytes ({KB} KB)
  Content-Type: application/pdf

=== CONSENT DOWNLOAD REQUEST COMPLETED SUCCESSFULLY (BINARY/JSON) ===
RequestId: {guid}
================================================================================
```

**Error Scenarios Logged**:
- Missing required parameters (requirementId, bankId)
- Missing both fileName and quoteId
- File not found on storage
- Exception details with full stack trace

### 3. Get Consent Document Info
**Route**: `GET /api/deposits/consent/info`

**Authentication**: ? None Required

**Logged Events**:
```
================================================================================
=== CONSENT INFO REQUEST START - RequestId: {guid} ===
================================================================================
Client IP: {ip_address}
RequestId: {guid}
Timestamp: {timestamp}
Authentication: NONE (Plain Access)
--- Request Parameters ---
  RequirementId: {value}
  BankId: {value}
  FileName: {value}

STEP 1: Validating request parameters - ? SUCCESS
  Details: All required parameters present

STEP 2: Checking file existence - ? SUCCESS
  Details: File: {file}, Requirement: {req}, Bank: {bank}

STEP 3: File information retrieved - ? SUCCESS
  Details: File size: {bytes} bytes ({KB} KB)
  Content-Type: application/pdf
  File exists: true

=== CONSENT INFO REQUEST COMPLETED SUCCESSFULLY ===
RequestId: {guid}, FileSize: {bytes} bytes
================================================================================
```

## Log Entry Format
Each log entry includes:
- **Timestamp**: `yyyy-MM-dd HH:mm:ss.fff` format
- **Log Level**: `[INFO]`, `[WARN]`, or `[ERROR]`
- **Message**: Detailed event description

Example:
```
[2024-01-15 14:23:45.123] [INFO ] === CONSENT DOWNLOAD REQUEST START - RequestId: abc-123 ===
[2024-01-15 14:23:45.156] [INFO ] STEP 1: Validating request parameters - ? SUCCESS
[2024-01-15 14:23:45.234] [ERROR] FILE NOT FOUND - consent.pdf [RequestId: abc-123]
```

## Logged Information

### Request Context (Always Logged)
- Request ID (unique identifier for each request)
- Client IP address (for security monitoring)
- Timestamp (UTC)
- All input parameters
- Accept header (for format determination)
- **Authentication status**: Always shows "NONE (Plain Access)"

### Success Path
- Parameter validation results
- File download progress
- Response format determination
- File size and metadata
- Completion status

### Error Path
- Validation failures with specific missing parameters
- File not found errors with expected path
- Exception type and message
- Full stack trace
- Request context for debugging

## Security Features
- Client IP addresses are logged for monitoring
- No sensitive authentication data (no API keys used)
- Request IDs allow correlation
- All access attempts are logged

## Integration with Existing Logging
The controller maintains dual logging:
1. **File-based logging** via `FtpLogger` (detailed, structured logs in Logs folder)
2. **System.Diagnostics.Trace** logging (for IIS/Windows diagnostics)

Both logging mechanisms run in parallel for comprehensive monitoring.

## Simple API Usage (No Authentication)

### Using Browser
Simply open URL in browser:
```
https://your-api-domain.com/api/deposits/consent/downloadconsent?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf
```

### Using curl
```bash
curl "https://your-api-domain.com/api/deposits/consent/downloadconsent?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf" -o consent.pdf
```

### Using Postman
1. Create new GET request
2. Enter URL with parameters
3. **NO headers required**
4. Click Send
5. PDF downloads automatically

### Using C# HttpClient
```csharp
var client = new HttpClient();
var url = "https://your-api-domain.com/api/deposits/consent/downloadconsent?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf";
var response = await client.GetAsync(url);
var fileBytes = await response.Content.ReadAsByteArrayAsync();
```

### Using JavaScript/Fetch
```javascript
fetch('https://your-api-domain.com/api/deposits/consent/downloadconsent?requirementId=REQ001&bankId=BANK001&fileName=consent.pdf')
  .then(response => response.blob())
  .then(blob => {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'consent.pdf';
    a.click();
  });
```

## Troubleshooting

### View Recent Logs
Use the diagnostic endpoint to view recent log entries:
```
GET /api/ftp-diagnostic/recent-logs?lines=50
```

### Check Log File Status
```
GET /api/ftp-diagnostic/log-info
```

### Common Log Patterns

**Successful Download**:
```
Look for: "CONSENT DOWNLOAD REQUEST COMPLETED SUCCESSFULLY"
```

**File Not Found**:
```
Look for: "FILE NOT FOUND" followed by expected path details
```

**Parameter Validation Error**:
```
Look for: "VALIDATION FAILED - Missing {parameter}"
```

**System Exception**:
```
Look for: "CONSENT DOWNLOAD REQUEST FAILED" with exception details
```

## Performance Considerations
- Logging is thread-safe (uses lock mechanism)
- Minimal performance impact (write operations are buffered)
- Automatic log rotation prevents unlimited file growth
- Both file and trace logging run asynchronously

## Maintenance
- Log files are created daily with date suffix
- Old log files are not automatically deleted (manual cleanup recommended)
- No special configuration required - works out of the box
- No authentication configuration needed - plain access enabled

## Security Note
While authentication is not required for these endpoints, all access is logged with:
- Client IP address
- Request timestamp
- All parameters
- Success/failure status
- Detailed error information

This allows for security monitoring and audit trails even without authentication.
