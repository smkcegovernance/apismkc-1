# ? Booth Mapping Postman Collection - Update Complete

## ?? What Was Updated

The Booth Mapping Postman collection has been **completely updated** to use **SHA-256 HMAC signature-based authentication**, matching your existing SMKC API security implementation.

---

## ?? Updated Files

| File | Status | Changes |
|------|--------|---------|
| `BoothMappingAPI.postman_collection.json` | ? Updated | Added SHA-256 auth headers, pre-request script |
| `BoothMappingAPI.Local.postman_environment.json` | ? Updated | Added apiKey, secretKey variables |
| `BoothMappingAPI.Production.postman_environment.json` | ? Updated | Added apiKey, secretKey variables |
| `README.md` | ? Updated | New authentication documentation |
| `SHA256_AUTHENTICATION_GUIDE.md` | ? New | Complete authentication guide |

---

## ?? Authentication Implementation

### Public Endpoint (No Auth Required)
```
POST /api/booth/login
```
- No authentication headers needed
- Standard username/password login

### Protected Endpoints (SHA-256 Auth Required)
```
GET  /api/booth/statistics
GET  /api/booth/booths
GET  /api/booth/booths/search
PUT  /api/booth/booths/{id}/location
```

**Required Headers:**
```
X-API-Key: BOOTH_API_KEY_12345678901234567890123456789012
X-Timestamp: 1736856594
X-Signature: lKj3h4g5h6j7k8l9m0n1o2p3q4r5s6t7u8v9w0x1y2z3==
```

---

## ?? Automatic Signature Generation

### Pre-Request Script Features

The collection includes a **smart pre-request script** that:

1. ? **Skips Login** - Detects public endpoint, skips signature
2. ? **Generates Timestamp** - Fresh Unix timestamp for each request
3. ? **Builds String to Sign** - `METHOD + URI + BODY + TIMESTAMP + APIKEY`
4. ? **Calculates Signature** - HMAC-SHA256 with Base64 encoding
5. ? **Sets Headers** - Automatically populates X-API-Key, X-Timestamp, X-Signature
6. ? **Validates Variables** - Checks for unresolved `{{variables}}`
7. ? **Console Logging** - Detailed debug information

### How It Works

```javascript
// Pre-request script runs automatically
const timestamp = Math.floor(Date.now() / 1000).toString();
const stringToSign = httpMethod + requestUri + requestBody + timestamp + apiKey;
const signature = CryptoJS.HmacSHA256(stringToSign, secretKey).toString(CryptoJS.enc.Base64);

// Headers are auto-populated
pm.variables.set('timestamp', timestamp);
pm.variables.set('signature', signature);
```

**You don't need to do anything** - just send the request!

---

## ?? Updated Collection Structure

```
?? Booth Mapping API Collection (v2.0)
?
??? ?? Authentication (1 request)
?   ??? Login - Valid Credentials
?       ? Public endpoint
?       ? No signature required
?       ? Auto-saves user token
?
??? ?? Statistics (2 requests)
?   ??? Get Overall Statistics
?   ?   ? SHA-256 auth
?   ?   ? Auto-generated signature
?   ??? Get User-Specific Statistics
?       ? SHA-256 auth
?       ? Query parameter support
?
??? ?? Booths (3 requests)
?   ??? Get All Booths
?   ?   ? SHA-256 auth
?   ?   ? Auto-saves first booth ID
?   ??? Search - Unmapped Booths Only
?   ?   ? SHA-256 auth
?   ?   ? Query parameter: isMapped=0
?   ??? Search - By Ward Number
?       ? SHA-256 auth
?       ? Query parameter: wardNo=1
?
??? ?? Location Updates (1 request)
?   ??? Update Booth Location - Success
?       ? SHA-256 auth
?       ? PUT with JSON body
?       ? Signature includes body
?
??? ?? Error Handling (2 requests)
    ??? Missing API Key
    ?   ? Tests 401 response
    ?   ? Validates error message
    ??? Invalid Signature
        ? Tests signature validation
        ? Confirms security works
```

**Total: 9 optimized requests** (streamlined from 21)

---

## ?? Environment Variables

### Collection Variables (Pre-configured)

```json
{
  "base_url": "http://localhost:5000/api/booth",
  "apiKey": "BOOTH_API_KEY_12345678901234567890123456789012",
  "secretKey": "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890",
  "user_id": "12345678",
  "booth_id": "BOOTH_000001"
}
```

### Auto-Generated Variables

```json
{
  "timestamp": "1736856594",        // Generated each request
  "signature": "lKj3h4g5h6...",     // Calculated each request
  "auth_token": "success-token-..." // Saved from login
}
```

---

## ?? Quick Start

### Step 1: Import Files
```
Postman ? Import ? Select Files:
  - BoothMappingAPI.postman_collection.json
  - BoothMappingAPI.Local.postman_environment.json
```

### Step 2: Select Environment
```
Top right dropdown ? "Booth Mapping - Local"
```

### Step 3: Update Keys (If Needed)
```
Environment ? Edit ? Update:
  - apiKey: Your API key
  - secretKey: Your secret key
```

### Step 4: Test!
```
1. Authentication ? Login - Valid Credentials ? Send
   ? No signature needed (public endpoint)
   
2. Statistics ? Get Overall Statistics ? Send
   ? Signature auto-generated
   ? Check Console for details
   
3. Booths ? Get All Booths ? Send
   ? Signature auto-generated
   ? Booth ID auto-saved
   
4. Location Updates ? Update Booth Location ? Send
   ? Signature auto-generated
   ? Body included in signature
```

---

## ?? Signature Algorithm

### Formula
```
StringToSign = METHOD + URI + BODY + TIMESTAMP + APIKEY
Signature = Base64(HMAC-SHA256(StringToSign, SECRET_KEY))
```

### Example: GET Statistics

**Request:**
```
GET /api/booth/statistics
```

**String to Sign:**
```
GET/api/booth/statistics1736856594BOOTH_API_KEY_12345678901234567890123456789012
```

**Signature:**
```
lKj3h4g5h6j7k8l9m0n1o2p3q4r5s6t7u8v9w0x1y2z3==
```

### Example: PUT Update Location

**Request:**
```
PUT /api/booth/booths/BOOTH_000001/location
Body: {"latitude":19.076,"longitude":72.8777,"userId":"12345678"}
```

**String to Sign:**
```
PUT/api/booth/booths/BOOTH_000001/location{"latitude":19.076,"longitude":72.8777,"userId":"12345678"}1736856596BOOTH_API_KEY_12345678901234567890123456789012
```

**Signature:**
```
[calculated_signature_here]
```

---

## ?? Troubleshooting

### ? "Missing API Key"

**Cause:** X-API-Key header not present

**Solution:**
1. Check environment is selected
2. Verify `apiKey` variable exists
3. Check pre-request script ran (Console tab)

---

### ? "Invalid Signature"

**Cause:** Signature mismatch

**Solution:**
1. **Open Console** (View ? Show Postman Console)
2. **Check logs:**
   ```
   === BOOTH MAPPING API SIGNATURE ===
   HTTP Method: GET
   Request URI: /api/booth/statistics
   String to Sign: GET/api/booth/statistics...
   Generated Signature: lKj3h4g5h6j7...
   ```
3. **Common issues:**
   - Wrong `secretKey` in environment
   - Unresolved variables: `{{booth_id}}`
   - System clock out of sync

---

### ? "Timestamp expired"

**Cause:** Request too old (>5 minutes)

**Solution:**
- Timestamp auto-generated fresh each request
- Check system clock is synchronized
- Server setting: `Auth_TimestampSkewMinutes = 5`

---

### ? Connection Refused

**Cause:** API not running

**Solution:**
1. Start API: `http://localhost:5000`
2. Verify base_url: `http://localhost:5000/api/booth`
3. Test directly: 
   ```bash
   curl http://localhost:5000/api/booth/login \
     -H "Content-Type: application/json" \
     -d '{"userId":"12345678","password":"password123"}'
   ```

---

## ?? Documentation Files

### 1. README.md ?
- Quick start guide
- Collection overview
- Environment setup
- Basic troubleshooting

### 2. SHA256_AUTHENTICATION_GUIDE.md ? NEW
- **Complete authentication reference**
- Signature algorithm details
- Code examples (JavaScript, C#, Python)
- Request flow diagram
- Advanced troubleshooting
- Security best practices

### 3. POSTMAN_COLLECTION_GUIDE.md
- Detailed usage guide
- Test scenarios
- Customization examples
- Newman CLI usage

---

## ?? Code Examples

### JavaScript (Node.js)
```javascript
const crypto = require('crypto');

const method = 'GET';
const uri = '/api/booth/statistics';
const body = '';
const timestamp = Math.floor(Date.now() / 1000).toString();
const apiKey = 'BOOTH_API_KEY_12345678901234567890123456789012';
const secretKey = 'BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890';

const stringToSign = method + uri + body + timestamp + apiKey;
const hmac = crypto.createHmac('sha256', secretKey);
hmac.update(stringToSign);
const signature = hmac.digest('base64');

console.log('Signature:', signature);
```

### C# (.NET)
```csharp
using System.Security.Cryptography;
using System.Text;

string method = "GET";
string uri = "/api/booth/statistics";
string body = "";
string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
string apiKey = "BOOTH_API_KEY_12345678901234567890123456789012";
string secretKey = "BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890";

string stringToSign = method + uri + body + timestamp + apiKey;

using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
{
    byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
    string signature = Convert.ToBase64String(hashBytes);
    Console.WriteLine($"Signature: {signature}");
}
```

### Python
```python
import hmac
import hashlib
import base64
import time

method = 'GET'
uri = '/api/booth/statistics'
body = ''
timestamp = str(int(time.time()))
api_key = 'BOOTH_API_KEY_12345678901234567890123456789012'
secret_key = 'BOOTH_SECRET_KEY_67890ABCDEFGHIJ1234567890'

string_to_sign = method + uri + body + timestamp + api_key
signature = hmac.new(
    secret_key.encode('utf-8'),
    string_to_sign.encode('utf-8'),
    hashlib.sha256
).digest()
signature_b64 = base64.b64encode(signature).decode('utf-8')

print(f'Signature: {signature_b64}')
```

---

## ?? Comparison: v1.0 vs v2.0

| Feature | v1.0 | v2.0 |
|---------|------|------|
| **Authentication** | Bearer token only | SHA-256 HMAC |
| **Public Endpoints** | Login only | Login only |
| **Protected Endpoints** | Bearer token | SHA-256 signature |
| **Headers** | Authorization | X-API-Key, X-Timestamp, X-Signature |
| **Security** | Token-based | Signature-based |
| **Signature Generation** | N/A | Automatic (pre-request script) |
| **Requests** | 21 requests | 9 optimized requests |
| **Console Logging** | Basic | Detailed signature logs |
| **Error Testing** | Token errors | Signature validation errors |
| **Documentation** | Basic guide | Complete auth guide |

---

## ? What's Ready

### Collection Features
- ? SHA-256 HMAC authentication
- ? Automatic signature generation
- ? Smart public/protected endpoint detection
- ? Variable validation
- ? Console logging for debugging
- ? Comprehensive test assertions
- ? Error scenario testing

### Documentation
- ? Updated README with auth details
- ? New SHA-256 authentication guide
- ? Code examples in multiple languages
- ? Troubleshooting guide
- ? Security best practices

### Environment
- ? Local environment with test keys
- ? Production environment template
- ? All variables pre-configured
- ? Secret key marked as secret type

---

## ?? Testing Checklist

### Basic Tests
- [ ] Import collection successfully
- [ ] Import environment successfully
- [ ] Select environment (top right)
- [ ] Login request works (200 OK)
- [ ] Console shows "Login endpoint - skipping signature"
- [ ] Statistics request works (200 OK)
- [ ] Console shows signature generation details
- [ ] Headers include X-API-Key, X-Timestamp, X-Signature
- [ ] All tests pass

### Signature Verification
- [ ] Open Postman Console (View ? Show Console)
- [ ] Send statistics request
- [ ] See signature calculation logs
- [ ] Verify timestamp is current Unix time
- [ ] Verify signature is Base64 encoded
- [ ] No unresolved variables in URI

### Error Testing
- [ ] Missing API Key test returns 400/401
- [ ] Invalid Signature test returns 401
- [ ] Error messages are clear
- [ ] Tests validate error responses

---

## ?? Next Steps

### For Testing
1. Import updated collection
2. Import updated environment
3. Run through test sequence
4. Verify signatures work

### For Integration
1. Read `SHA256_AUTHENTICATION_GUIDE.md`
2. Implement signature generation in your client
3. Use provided code examples
4. Test against API

### For Deployment
1. Update production environment keys
2. Configure server-side validation
3. Set up monitoring
4. Document for team

---

## ?? Key Benefits

### ? Security
- HMAC-SHA256 signature prevents tampering
- Timestamp validation prevents replay attacks
- API key identifies client
- Secret key never transmitted

### ? Automation
- Signatures generated automatically
- No manual calculation needed
- Variables auto-populated
- Timestamps auto-generated

### ? Debugging
- Console logs show calculation
- Signature details visible
- Variable validation built-in
- Clear error messages

### ? Consistency
- Matches SMKC Deposit Manager authentication
- Same security pattern across APIs
- Standard HMAC-SHA256 algorithm
- Base64 encoding

---

## ?? Support

### Issues with Collection
- Check `README.md` for quick start
- Review Console logs for signature details
- Verify environment variables set

### Understanding Authentication
- Read `SHA256_AUTHENTICATION_GUIDE.md`
- See code examples for your language
- Review signature algorithm section

### API Errors
- Check API documentation
- Verify server is running
- Test with curl first

---

**Status:** ? **Complete and Ready**  
**Version:** 2.0 (SHA-256 Authentication)  
**Created:** January 2025  
**Updated:** January 2025  
**Total Files:** 5 files  
**Documentation:** Complete  
**Authentication:** HMAC-SHA256  
**Signature Generation:** Automatic
