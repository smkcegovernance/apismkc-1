# ? Booth Mapping API - Schema Connection Fix Applied

## ?? Change Summary

**Issue:** The login endpoint was using the wrong database schema connection.

**Solution:** Changed `BoothAuthRepository` to use the **WEBSITE schema** (`OracleDb` connection) instead of the ULBERP schema.

---

## ?? Changes Made

### 1. Repository Connection String Updated

**File:** `Repositories\BoothMapping\BoothMappingRepositories.cs`

**Before:**
```csharp
public BoothAuthRepository()
{
    // Use ULBERP schema connection for authentication
    _connectionString = ConfigurationManager.ConnectionStrings["OracleDbUlberp"].ConnectionString;
}
```

**After:**
```csharp
public BoothAuthRepository()
{
    // Use WEBSITE schema connection (ws/ws) for authentication
    // SP_USER_LOGIN is in the WEBSITE schema
    _connectionString = ConfigurationManager.ConnectionStrings["OracleDb"].ConnectionString;
}
```

### 2. Controller Documentation Updated

**File:** `Controllers\BoothMapping\BoothAuthController.cs`

Updated XML documentation to reflect WEBSITE schema usage:
```csharp
/// <summary>
/// Authentication controller for Booth Mapping Application
/// Uses WEBSITE schema for user authentication
/// </summary>
```

---

## ??? Schema Configuration

### Current Schema Setup

| Component | Schema | Connection String | Purpose |
|-----------|--------|-------------------|---------|
| **Login (SP_USER_LOGIN)** | WEBSITE | `OracleDb` (ws/ws) | ? User authentication |
| **Booth Operations** | WEBSITE | `OracleDb` (ws/ws) | ? Booth CRUD operations |

### All Operations Now Use WEBSITE Schema

```
? BoothAuthRepository ? OracleDb (WEBSITE schema)
   ??? SP_USER_LOGIN

? BoothRepository ? OracleDb (WEBSITE schema)
   ??? SP_GET_STATISTICS
   ??? SP_GET_ALL_BOOTHS
   ??? SP_SEARCH_BOOTHS
   ??? SP_UPDATE_BOOTH_LOCATION
```

---

## ?? Testing Required

After this change, you need to verify:

### 1. Web.config Connection String

Ensure `OracleDb` connection string is configured:

```xml
<connectionStrings>
  <add name="OracleDb" 
       connectionString="Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=your_host)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=your_service)));User Id=ws;Password=ws;" 
       providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
```

### 2. Database Stored Procedure

Verify `SP_USER_LOGIN` exists in WEBSITE schema:

```sql
-- Connect as WEBSITE schema user (ws/ws)
SELECT object_name, object_type 
FROM user_objects 
WHERE object_name = 'SP_USER_LOGIN' 
AND object_type = 'PROCEDURE';
```

Expected result: Should return one row showing the procedure exists.

### 3. Test Login Endpoint

#### Using Postman

1. **Restart API** (Important - rebuild and restart)
   ```
   Stop debugging (Shift+F5)
   Rebuild Solution (Ctrl+Shift+B)
   Start debugging (F5)
   ```

2. **Test Login Request**
   ```
   POST http://localhost:5000/api/booth/login
   Content-Type: application/json
   
   {
     "userId": "12345678",
     "password": "password123"
   }
   ```

3. **Expected Success Response (200 OK)**
   ```json
   {
     "success": true,
     "message": "Login successful",
     "data": {
       "userId": "12345678",
       "userName": "User Name",
       "role": "Mapper",
       "token": "generated-token-here"
     },
     "timestamp": "2026-01-06T10:30:00Z"
   }
   ```

4. **Expected Error Response (401 Unauthorized)**
   ```json
   {
     "success": false,
     "message": "Invalid credentials",
     "timestamp": "2026-01-06T10:30:00Z"
   }
   ```

#### Using cURL

```bash
curl -X POST "http://localhost:5000/api/booth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "12345678",
    "password": "password123"
  }'
```

---

## ?? Troubleshooting

### Issue: Still getting connection error

**Possible Causes:**

1. **Connection string not configured**
   - Check `Web.config` has `OracleDb` connection string
   - Verify credentials: `ws/ws`

2. **Stored procedure doesn't exist**
   ```sql
   -- Check if SP_USER_LOGIN exists
   SELECT * FROM user_objects 
   WHERE object_name = 'SP_USER_LOGIN';
   ```

3. **Wrong schema selected**
   - Make sure you're connecting to WEBSITE schema (ws/ws)
   - Not ULBERP schema (ulberp/ulberp)

### Issue: Invalid credentials error

**This is normal** - means connection is working, but:
- User doesn't exist in database
- Password is incorrect
- Stored procedure logic is checking credentials

**To verify connection is working:**
- Check Visual Studio Output window for any Oracle connection errors
- If you see "Invalid credentials" message, the connection is working correctly

---

## ?? Summary of All Booth Mapping Endpoints

All endpoints now correctly use **WEBSITE schema** (`ws/ws`):

| Endpoint | Method | Schema | Stored Procedure |
|----------|--------|--------|------------------|
| `/api/booth/login` | POST | WEBSITE | SP_USER_LOGIN |
| `/api/booth/statistics` | GET | WEBSITE | SP_GET_STATISTICS |
| `/api/booth/booths` | GET | WEBSITE | SP_GET_ALL_BOOTHS |
| `/api/booth/booths/search` | GET | WEBSITE | SP_SEARCH_BOOTHS |
| `/api/booth/booths/{id}/location` | PUT | WEBSITE | SP_UPDATE_BOOTH_LOCATION |

---

## ? Verification Checklist

- [x] Repository updated to use OracleDb connection
- [x] Controller documentation updated
- [x] Build successful
- [ ] **You need to test:** Login endpoint with real credentials
- [ ] **You need to verify:** SP_USER_LOGIN exists in WEBSITE schema
- [ ] **You need to verify:** OracleDb connection string configured

---

## ?? Next Steps

1. **Restart the API**
   ```
   Stop debugging in Visual Studio
   Rebuild solution
   Start debugging again
   ```

2. **Test with Postman/cURL**
   - Use the test commands above
   - Verify successful connection to WEBSITE schema

3. **Check Stored Procedure**
   - Ensure SP_USER_LOGIN exists in WEBSITE schema
   - Verify it returns expected columns (userId, userName, role, token)

---

## ?? Support

### If Login Still Fails

Check these in order:

1. **Web.config Connection String**
   ```xml
   <add name="OracleDb" connectionString="...User Id=ws;Password=ws..." />
   ```

2. **Database Connection**
   ```bash
   # Test connection using SQL*Plus or Oracle SQL Developer
   sqlplus ws/ws@your_service
   ```

3. **Stored Procedure**
   ```sql
   SELECT text FROM user_source 
   WHERE name = 'SP_USER_LOGIN' 
   ORDER BY line;
   ```

4. **Visual Studio Output**
   - View ? Output
   - Show output from: Debug
   - Look for Oracle error messages

---

**Status:** ? **Code Changes Complete**  
**Build:** ? **Successful**  
**Schema:** ? **Changed to WEBSITE**  
**Testing:** ? **Required - Please test login endpoint**

**Remember:** Restart the API after this change!
