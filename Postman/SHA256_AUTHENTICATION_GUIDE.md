# Booth Mapping API - SHA-256 Authentication Guide

## ?? Overview

The Booth Mapping API uses **HMAC-SHA256 signature-based authentication** for all protected endpoints, matching the security implementation used across the SMKC API project.

---

## ?? Authentication Types

### 1. Public Endpoints (No Authentication)
```
POST /api/booth/login
```
- ? No authentication headers required
- Used for user login only

### 2. Protected Endpoints (SHA-256 Authentication)
```
GET  /api/booth/statistics
GET  /api/booth/booths
GET  /api/booth/booths/search
PUT  /api/booth/booths/{id}/location
```
- ? Requires SHA-256 HMAC signature
- Three headers required: `X-API-Key`, `X-Timestamp`, `X-Signature`

---

## ?? Required Headers

### For Protected Endpoints

| Header | Description | Example |
|--------|-------------|---------|
| `X-API-Key` | API identification key | `BOOTH_API_KEY_12345678...` |
| `X-Timestamp` | Unix timestamp (seconds) | `1736856594` |
| `X-Signature` | HMAC-SHA256 signature (Base64) | `lKj3h4g5h6j7k8l9m0n1o2p3q4r5s6t7u8v9w0x1y2z3==` |
| `Content-Type` | For POST/PUT requests | `application/json` |

---

## ?? Signature Generation Algorithm

### String to Sign Formula
```
StringToSign = HTTP_METHOD + REQUEST_URI + REQUEST_BODY + TIMESTAMP + API_KEY
```

### Components

1. **HTTP_METHOD**: Uppercase HTTP verb
   - Examples: `GET`, `POST`, `PUT`, `DELETE`

2. **REQUEST_URI**: Full path with query parameters
   - Format: `/api/booth/statistics?userId=12345678`
   - Must include leading slash
   - Must include query string if present

3. **REQUEST_BODY**: Raw JSON body
   - For GET requests: empty string `""`
   - For POST/PUT: full JSON payload

4. **TIMESTAMP**: Unix timestamp in seconds
   - Generated at request time
   - Example: `1736856594`

5. **API_KEY**: Your API key
   - Example: `BOOTH_API_KEY_12345678901234567890123456789012`

### Signature Calculation
```
Signature = Base64(HMAC-SHA256(StringToSign, SECRET_KEY))
```

---

## ?? Examples

### Example 1: GET Statistics (No Query Params)

**Request Details:**
```
Method: GET
URI: /api/booth/statistics
Body: (empty)
Timestamp: 1736856594
API Key: BOOTH_API_KEY_12345678901234567890123456789012
Secret Key: BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890
```

**String to Sign:**
```
GET/api/booth/statistics1736856594BOOTH_API_KEY_12345678901234567890123456789012
```

**Signature (Base64):**
```
lKj3h4g5h6j7k8l9m0n1o2p3q4r5s6t7u8v9w0x1y2z3==
```

**cURL Example:**
```bash
curl -X GET "http://localhost:5000/api/booth/statistics" \
  -H "X-API-Key: BOOTH_API_KEY_12345678901234567890123456789012" \
  -H "X-Timestamp: 1736856594" \
  -H "X-Signature: lKj3h4g5h6j7k8l9m0n1o2p3q4r5s6t7u8v9w0x1y2z3=="
```

---

### Example 2: GET Statistics (With Query Params)

**Request Details:**
```
Method: GET
URI: /api/booth/statistics?userId=12345678
Body: (empty)
Timestamp: 1736856595
API Key: BOOTH_API_KEY_12345678901234567890123456789012
Secret Key: BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890
```

**String to Sign:**
```
GET/api/booth/statistics?userId=123456781736856595BOOTH_API_KEY_12345678901234567890123456789012
```

**Important:** Query parameters MUST be included in the URI!

---

### Example 3: PUT Update Location (With Body)

**Request Details:**
```
Method: PUT
URI: /api/booth/booths/BOOTH_000001/location
Body: {"latitude":19.076,"longitude":72.8777,"userId":"12345678","remarks":"GPS verified"}
Timestamp: 1736856596
API Key: BOOTH_API_KEY_12345678901234567890123456789012
Secret Key: BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890
```

**String to Sign:**
```
PUT/api/booth/booths/BOOTH_000001/location{"latitude":19.076,"longitude":72.8777,"userId":"12345678","remarks":"GPS verified"}1736856596BOOTH_API_KEY_12345678901234567890123456789012
```

**cURL Example:**
```bash
curl -X PUT "http://localhost:5000/api/booth/booths/BOOTH_000001/location" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: BOOTH_API_KEY_12345678901234567890123456789012" \
  -H "X-Timestamp: 1736856596" \
  -H "X-Signature: [calculated_signature_here]" \
  -d '{
    "latitude": 19.076,
    "longitude": 72.8777,
    "userId": "12345678",
    "remarks": "GPS verified"
  }'
```

---

## ?? Implementation Examples

### JavaScript (Node.js)

```javascript
const crypto = require('crypto');

function generateSignature(method, uri, body, timestamp, apiKey, secretKey) {
  // Build string to sign
  const stringToSign = method + uri + body + timestamp + apiKey;
  
  // Calculate HMAC-SHA256
  const hmac = crypto.createHmac('sha256', secretKey);
  hmac.update(stringToSign);
  const signature = hmac.digest('base64');
  
  return signature;
}

// Example usage
const method = 'GET';
const uri = '/api/booth/statistics';
const body = '';
const timestamp = Math.floor(Date.now() / 1000).toString();
const apiKey = 'BOOTH_API_KEY_12345678901234567890123456789012';
const secretKey = 'BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890';

const signature = generateSignature(method, uri, body, timestamp, apiKey, secretKey);

console.log('Timestamp:', timestamp);
console.log('Signature:', signature);
```

---

### C# (.NET)

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public class SignatureGenerator
{
    public static string GenerateSignature(
        string method, 
        string uri, 
        string body, 
        string timestamp, 
        string apiKey, 
        string secretKey)
    {
        // Build string to sign
        string stringToSign = method + uri + body + timestamp + apiKey;
        
        // Calculate HMAC-SHA256
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
        {
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Convert.ToBase64String(hashBytes);
        }
    }
}

// Example usage
string method = "GET";
string uri = "/api/booth/statistics";
string body = "";
string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
string apiKey = "BOOTH_API_KEY_12345678901234567890123456789012";
string secretKey = "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890";

string signature = SignatureGenerator.GenerateSignature(
    method, uri, body, timestamp, apiKey, secretKey);

Console.WriteLine($"Timestamp: {timestamp}");
Console.WriteLine($"Signature: {signature}");
```

---

### Python

```python
import hmac
import hashlib
import base64
import time

def generate_signature(method, uri, body, timestamp, api_key, secret_key):
    # Build string to sign
    string_to_sign = method + uri + body + timestamp + api_key
    
    # Calculate HMAC-SHA256
    signature = hmac.new(
        secret_key.encode('utf-8'),
        string_to_sign.encode('utf-8'),
        hashlib.sha256
    ).digest()
    
    # Return Base64 encoded
    return base64.b64encode(signature).decode('utf-8')

# Example usage
method = 'GET'
uri = '/api/booth/statistics'
body = ''
timestamp = str(int(time.time()))
api_key = 'BOOTH_API_KEY_12345678901234567890123456789012'
secret_key = 'BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890'

signature = generate_signature(method, uri, body, timestamp, api_key, secret_key)

print(f'Timestamp: {timestamp}')
print(f'Signature: {signature}')
```

---

## ?? Postman Collection

### Automatic Signature Generation

The Postman collection includes a **pre-request script** that automatically generates the signature for every protected endpoint.

**How it works:**

1. **Pre-Request Script** runs before each request
2. Generates Unix timestamp
3. Extracts request method, URI, and body
4. Calculates HMAC-SHA256 signature
5. Sets `{{timestamp}}` and `{{signature}}` variables
6. Headers automatically use these variables

**Configuration:**

Collection variables:
```json
{
  "apiKey": "BOOTH_API_KEY_12345678901234567890123456789012",
  "secretKey": "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890"
}
```

Headers (auto-populated):
```
X-API-Key: {{apiKey}}
X-Timestamp: {{timestamp}}
X-Signature: {{signature}}
```

### Testing Flow

1. **Login** (no signature required)
   ```
   POST /api/booth/login
   ? Returns user token (saved automatically)
   ```

2. **Get Statistics** (signature auto-generated)
   ```
   GET /api/booth/statistics
   Headers: X-API-Key, X-Timestamp, X-Signature
   ? Returns booth counts
   ```

3. **Update Location** (signature auto-generated)
   ```
   PUT /api/booth/booths/{id}/location
   Headers: X-API-Key, X-Timestamp, X-Signature, Content-Type
   Body: JSON with latitude/longitude
   ? Updates booth GPS coordinates
   ```

---

## ?? Troubleshooting

### Error: "Missing API Key"

**Cause:** `X-API-Key` header not present

**Solution:**
```bash
# Add header
-H "X-API-Key: YOUR_API_KEY_HERE"
```

---

### Error: "Invalid Signature"

**Cause:** Signature doesn't match server calculation

**Common Issues:**

1. **Wrong Timestamp**
   - Client and server clocks out of sync
   - Timestamp too old (check timestamp skew setting)

2. **Wrong URI**
   - Missing leading slash
   - Missing or wrong query parameters
   - Variable not resolved (e.g., `{{booth_id}}` still present)

3. **Wrong Body**
   - Extra whitespace
   - Different JSON formatting
   - Body not matching what was signed

4. **Wrong Method**
   - Case mismatch (must be uppercase)

**Debug Steps:**

1. **Check Console Logs** in Postman
   ```
   === BOOTH MAPPING API SIGNATURE ===
   HTTP Method: GET
   Request URI: /api/booth/statistics
   Request Body: (empty)
   Timestamp: 1736856594
   String to Sign: GET/api/booth/statistics...
   Generated Signature: lKj3h4g5h6j7k8l9...
   ```

2. **Verify Variables** are resolved
   - Check for `{{variable}}` in URI
   - Ensure environment is selected

3. **Check Request Order**
   - Body must match exactly what's sent
   - Query params must be included in URI

---

### Error: "Timestamp expired"

**Cause:** Request timestamp too old

**Server Settings:**
```xml
<add key="Auth_TimestampSkewMinutes" value="5" />
```

**Solution:**
- Request must be made within 5 minutes of timestamp
- Check system clock is synchronized
- Generate fresh timestamp for each request

---

## ?? Security Best Practices

### 1. Keep Secret Key Secure

? **Don't:**
- Store in client-side code
- Commit to version control
- Share in plain text

? **Do:**
- Store in environment variables
- Use secret management systems
- Rotate regularly

### 2. Use HTTPS in Production

? **Don't:**
- Send over HTTP
- Use in unsecured networks

? **Do:**
- Always use HTTPS
- Verify SSL certificates
- Use TLS 1.2+

### 3. Implement Rate Limiting

? **Recommended:**
- Limit requests per minute
- Track by API key
- Implement exponential backoff

### 4. Monitor for Abuse

? **Track:**
- Failed signature attempts
- Timestamp anomalies
- Unusual request patterns

---

## ?? Request Flow Diagram

```
???????????????
?   Client    ?
???????????????
       ?
       ? 1. Generate Timestamp
       ????????????????????????????????????
       ?                                  ?
       ? 2. Build StringToSign            ?
       ?    METHOD + URI + BODY +         ?
       ?    TIMESTAMP + APIKEY            ?
       ?                                  ?
       ????????????????????????????????????
       ?
       ? 3. Calculate HMAC-SHA256
       ?    Signature = HMAC(StringToSign, SECRET)
       ?
       ? 4. Send Request with Headers
       ?    - X-API-Key
       ?    - X-Timestamp
       ?    - X-Signature
       ?
       ?
???????????????
?   Server    ?
?  (API)      ?
???????????????
       ?
       ? 5. Validate API Key exists
       ????????????????????????????????????
       ?                                  ?
       ? 6. Check Timestamp not expired   ?
       ?    (within skew window)          ?
       ?                                  ?
       ????????????????????????????????????
       ?
       ? 7. Rebuild StringToSign
       ?    METHOD + URI + BODY +
       ?    TIMESTAMP + APIKEY
       ?
       ? 8. Calculate Expected Signature
       ?    ExpectedSig = HMAC(StringToSign, SECRET)
       ?
       ? 9. Compare Signatures
       ?    ClientSig == ExpectedSig?
       ?
       ??????????????????????????????
       ? YES         ? NO           ?
       ?             ?              ?
       ?             ?              ?
   ? Allow     ? Reject         ?
   Process      401 Unauthorized  ?
   Request                         ?
                                   ?
                                   ?
                              Log attempt
```

---

## ?? Related Documentation

- **Postman Collection:** `BoothMappingAPI.postman_collection.json`
- **Environment Setup:** `BoothMappingAPI.Local.postman_environment.json`
- **API Documentation:** `BOOTH_MAPPING_API_DOCUMENTATION.md`
- **Quick Reference:** `BOOTH_MAPPING_QUICK_REFERENCE.md`

---

## ?? Tips

1. **Use Postman Collection** - Signature generation is automatic
2. **Check Console Logs** - Debug signature issues
3. **Verify Environment** - Ensure correct API key/secret
4. **Test Login First** - Verify API is accessible
5. **Watch Timestamp** - Must be fresh (within 5 minutes)

---

**Updated:** January 2025  
**Version:** 2.0 (SHA-256 Authentication)  
**Status:** ? Production Ready
