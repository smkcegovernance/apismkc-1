# Booth Mapping API - Postman Collection Guide

## ?? Overview

Complete Postman collection for testing the Booth Mapping Application API with:
- **21 Pre-configured Requests** across 5 categories
- **Automated Tests** for response validation
- **Environment Variables** for easy configuration
- **Test Scripts** that auto-save tokens and IDs

---

## ?? Quick Start

### Step 1: Import Collection

1. Open Postman
2. Click **Import** button (top left)
3. Select **File** tab
4. Choose `BoothMappingAPI.postman_collection.json`
5. Click **Import**

### Step 2: Import Environment

1. Click **Import** button again
2. Select `BoothMappingAPI.Local.postman_environment.json`
3. Click **Import**
4. Select the environment from dropdown (top right)

### Step 3: Update Base URL (if needed)

1. Click the environment dropdown (top right)
2. Select "Booth Mapping - Local"
3. Update `base_url` if your API is not on `http://localhost:5000`
4. Click **Save**

### Step 4: Run Login Request

1. Expand **Authentication** folder
2. Select **Login - Valid Credentials**
3. Click **Send**
4. Token will be automatically saved to environment

### Step 5: Test Other Endpoints

Now you can test any endpoint - the token is automatically used!

---

## ?? Collection Structure

### 1. Authentication (3 requests)
- ? **Login - Valid Credentials** - Get auth token
- ? **Login - Invalid Credentials** - Test error handling
- ? **Login - Missing UserId** - Test validation

### 2. Statistics (2 requests)
- ?? **Get Overall Statistics** - All booth counts
- ?? **Get User-Specific Statistics** - User contribution stats

### 3. Booths (8 requests)
- ?? **Get All Booths** - Complete booth list
- ?? **Search - All Booths** - No filters
- ?? **Search - By Ward Number** - Filter by ward
- ?? **Search - Unmapped Booths Only** - isMapped=0
- ?? **Search - Mapped Booths Only** - isMapped=1
- ?? **Search - By Booth Name (Hindi)** - Hindi search
- ?? **Search - By Booth Name (English)** - English search
- ?? **Search - Multiple Filters** - Combined filters

### 4. Location Updates (6 requests)
- ? **Update Booth Location - Success** - Valid update
- ? **Update Location - Invalid Latitude** - Test validation
- ? **Update Location - Invalid Longitude** - Test validation
- ? **Update Location - Booth Not Found** - Test 404
- ? **Update Location - Without Remarks** - Optional field test

### 5. Error Handling (2 requests)
- ? **Unauthorized - No Token** - Test auth requirement
- ? **Unauthorized - Invalid Token** - Test token validation

---

## ?? Environment Variables

### Automatic Variables (Set by Tests)
- `auth_token` - Authentication token (auto-saved after login)
- `booth_id` - First booth ID (auto-saved from booth list)

### Manual Variables (You can customize)
- `base_url` - API base URL (default: `http://localhost:5000/api/booth`)
- `user_id` - Test user ID (default: `12345678`)
- `test_latitude` - Test GPS latitude (default: `19.0760`)
- `test_longitude` - Test GPS longitude (default: `72.8777`)

---

## ?? Automated Tests

Every request includes automated tests that verify:

### Global Tests (All Requests)
- ? Response time < 3000ms
- ? Response has timestamp field
- ? Logs response status

### Request-Specific Tests

#### Login Tests
- ? Status code is 200 (success) or 401 (failure)
- ? Response has success property
- ? Data has token (on success)
- ? Token is auto-saved to environment
- ? User data is complete

#### Statistics Tests
- ? Status code is 200
- ? All count fields are numbers
- ? totalBooths = mappedBooths + unmappedBooths

#### Booth List Tests
- ? Status code is 200
- ? Data is an array
- ? Booth objects have required properties
- ? First booth ID is auto-saved

#### Search Tests
- ? Filtered results match criteria
- ? Ward numbers match filter
- ? Mapping status matches filter

#### Location Update Tests
- ? Status code is 200 (success) or 400/404 (error)
- ? Booth is marked as mapped
- ? Location matches request
- ? mappedBy and mappedDate are set

---

## ?? Usage Examples

### Example 1: Complete Test Flow

```
1. Login - Valid Credentials
   ? Token saved automatically

2. Get Statistics
   ? See overall booth counts

3. Search - Unmapped Booths Only
   ? Find booths that need mapping

4. Update Booth Location - Success
   ? Map first unmapped booth

5. Get User-Specific Statistics
   ? Verify your contribution increased
```

### Example 2: Search Testing

```
1. Search - All Booths
   ? Get complete list

2. Search - By Ward Number (wardNo=1)
   ? Filter by ward

3. Search - Mapped Booths Only (isMapped=1)
   ? See completed booths

4. Search - Multiple Filters
   ? Combine ward + status + name
```

### Example 3: Validation Testing

```
1. Update Location - Invalid Latitude (95.0)
   ? Verify validation works

2. Update Location - Invalid Longitude (200.0)
   ? Verify validation works

3. Update Location - Booth Not Found
   ? Verify 404 handling

4. Update Location - Success
   ? Confirm valid data works
```

---

## ?? Customization

### Change Base URL

**Option 1: Edit Environment**
1. Click environment dropdown
2. Click edit icon
3. Update `base_url` value
4. Save

**Option 2: Edit Collection Variables**
1. Right-click collection
2. Select **Edit**
3. Go to **Variables** tab
4. Update `base_url`
5. Save

### Add Custom Test User

1. Edit environment
2. Change `user_id` to your test user ID (8 digits)
3. Update login request body with matching credentials
4. Save

### Test Different Booth

1. After running "Get All Booths"
2. Copy desired booth ID from response
3. Edit environment
4. Update `booth_id` variable
5. Save

---

## ?? Running Collections

### Run Entire Collection

1. Click **...** (three dots) on collection
2. Select **Run collection**
3. Select environment
4. Click **Run Booth Mapping API**
5. View results

### Run Specific Folder

1. Click **...** on folder (e.g., "Authentication")
2. Select **Run folder**
3. View results

### Run with Newman (CLI)

```bash
# Install Newman
npm install -g newman

# Run collection
newman run BoothMappingAPI.postman_collection.json \
  -e BoothMappingAPI.Local.postman_environment.json

# Generate HTML report
newman run BoothMappingAPI.postman_collection.json \
  -e BoothMappingAPI.Local.postman_environment.json \
  -r html
```

---

## ?? Troubleshooting

### Issue: "Unauthorized" on All Requests

**Solution:**
1. Run **Login - Valid Credentials** first
2. Check that token was saved (Console tab shows "Token saved: ...")
3. Verify environment is selected (top right dropdown)

### Issue: "Connection Refused"

**Solution:**
1. Check API is running (`http://localhost:5000`)
2. Verify `base_url` in environment
3. Test API directly in browser: `http://localhost:5000/api/booth/statistics`

### Issue: "Booth Not Found" (404)

**Solution:**
1. Run **Get All Booths** to see available booth IDs
2. Update `booth_id` environment variable
3. Verify booth ID format (e.g., "BOOTH_000001")

### Issue: "Invalid Credentials" on Login

**Solution:**
1. Verify database has test user
2. Check user ID is exactly 8 characters
3. Verify password matches database
4. Check ULBERP schema connection

### Issue: Tests Failing

**Solution:**
1. Check response in **Body** tab
2. Look at **Test Results** tab for details
3. Verify database has required data
4. Check stored procedures are created

---

## ?? Request Details

### 1. POST /api/booth/login

**Purpose:** Authenticate user and get token

**Request:**
```json
{
  "userId": "12345678",
  "password": "password123"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": "12345678",
    "userName": "User 12345678",
    "role": "Mapper",
    "token": "success-token-12345678"
  },
  "timestamp": "2026-01-14T10:30:00.000Z"
}
```

**Auto-Actions:**
- ? Saves token to `auth_token` variable
- ? Saves userId to `user_id` variable

---

### 2. GET /api/booth/statistics

**Purpose:** Get booth mapping statistics

**Query Params:**
- `userId` (optional) - Get user-specific stats

**Response (200):**
```json
{
  "success": true,
  "message": "Statistics retrieved successfully",
  "data": {
    "totalBooths": 527,
    "mappedBooths": 145,
    "unmappedBooths": 382,
    "userMappedBooths": 23,
    "userUnmappedBooths": 504
  },
  "timestamp": "2026-01-14T10:30:00.000Z"
}
```

---

### 3. GET /api/booth/booths

**Purpose:** Get all booths

**Response (200):**
```json
{
  "success": true,
  "message": "Booths retrieved successfully",
  "data": [
    {
      "id": "BOOTH_000001",
      "boothNo": "1",
      "boothName": "???????? ????, ???? ?????",
      "boothNameEnglish": "Primary School, Koli Galli",
      "wardNo": "1",
      "latitude": 19.0760,
      "longitude": 72.8777,
      "isMapped": "true",
      "mappedBy": "12345678",
      "mappedDate": "2026-01-05T14:30:45.123Z"
    }
  ],
  "timestamp": "2026-01-14T10:30:00.000Z"
}
```

**Auto-Actions:**
- ? Saves first booth ID to `booth_id` variable

---

### 4. GET /api/booth/booths/search

**Purpose:** Search booths with filters

**Query Params:**
- `boothNo` - Booth number (partial match)
- `boothName` - Name in Hindi or English (partial match)
- `boothAddress` - Address in Hindi or English (partial match)
- `wardNo` - Ward number (partial match)
- `isMapped` - 0=unmapped, 1=mapped

**Examples:**
```
/booths/search?wardNo=1
/booths/search?isMapped=0
/booths/search?boothName=school
/booths/search?wardNo=1&isMapped=0
```

---

### 5. PUT /api/booth/booths/{id}/location

**Purpose:** Update booth GPS location

**URL Param:**
- `id` - Booth ID (e.g., BOOTH_000001)

**Request:**
```json
{
  "latitude": 19.0760,
  "longitude": 72.8777,
  "userId": "12345678",
  "remarks": "GPS verified on site"
}
```

**Validation:**
- Latitude: -90 to 90
- Longitude: -180 to 180
- UserId: 8 characters
- Remarks: Optional, max 500 chars

**Response (200):**
```json
{
  "success": true,
  "message": "Booth location updated successfully",
  "data": {
    "id": "BOOTH_000001",
    "latitude": 19.0760,
    "longitude": 72.8777,
    "isMapped": "true",
    "mappedBy": "12345678",
    "mappedDate": "2026-01-06T10:25:30.456Z"
  },
  "timestamp": "2026-01-14T10:30:00.000Z"
}
```

---

## ?? Testing Checklist

### Initial Setup
- [ ] Collection imported
- [ ] Environment imported and selected
- [ ] Base URL configured
- [ ] Test user credentials updated

### Basic Tests
- [ ] Login with valid credentials (200)
- [ ] Login with invalid credentials (401)
- [ ] Get statistics without userId
- [ ] Get statistics with userId
- [ ] Get all booths
- [ ] Search without filters

### Filter Tests
- [ ] Search by ward number
- [ ] Search by booth name (Hindi)
- [ ] Search by booth name (English)
- [ ] Search unmapped booths only
- [ ] Search mapped booths only
- [ ] Search with multiple filters

### Update Tests
- [ ] Update location successfully
- [ ] Update with invalid latitude (400)
- [ ] Update with invalid longitude (400)
- [ ] Update non-existent booth (404)
- [ ] Update without remarks (optional field)

### Security Tests
- [ ] Request without token (401)
- [ ] Request with invalid token (401)

### Data Verification
- [ ] Hindi text displays correctly
- [ ] English text displays correctly
- [ ] Dates are in ISO 8601 format
- [ ] Null values handled properly
- [ ] Numbers are correct type

---

## ?? Additional Resources

- **API Documentation:** `Documentation\BOOTH_MAPPING_API_DOCUMENTATION.md`
- **Quick Reference:** `BOOTH_MAPPING_QUICK_REFERENCE.md`
- **Deployment Guide:** `BOOTH_MAPPING_DEPLOYMENT_CHECKLIST.md`

---

## ?? Pro Tips

1. **Use Console Tab**: See auto-saved variables and logs
2. **Use Test Results**: View detailed test pass/fail
3. **Use Pre-request Script**: Automatically sets up requests
4. **Save Responses**: Right-click response ? Save as Example
5. **Environment Switching**: Quickly switch Local ? Production
6. **Collection Runner**: Run all tests in sequence
7. **Newman Reports**: Generate HTML test reports
8. **Share Collection**: Export and share with team

---

**Created:** January 2025  
**Version:** 1.0  
**Collection Items:** 21 requests  
**Automated Tests:** 50+ test assertions  
**Status:** ? Ready to Use
