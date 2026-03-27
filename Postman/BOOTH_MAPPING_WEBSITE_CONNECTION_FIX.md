# ? Booth Mapping - WEBSITE Schema Connection Updated

## ?? Issue Fixed

**Problem:** Login was using `ws/ws` credentials instead of `website/website` for the WEBSITE schema.

**Solution:** Created new `OracleDbWebsite` connection string and updated the repository to use it.

---

## ?? Changes Made

### 1. New Connection String Added

**File:** `Web.config`

```xml
<add name="OracleDbWebsite"
     providerName="Oracle.ManagedDataAccess.Client"
     connectionString="User Id=website;Password=website;Data Source=//SMKC-SCAN:1521/hcldb;Pooling=true;Min Pool Size=1;Max Pool Size=100;Connection Timeout=60;Incr Pool Size=5;Decr Pool Size=2;Connection Lifetime=300;Validate Connection=true" />
```

### 2. Repository Updated

**File:** `Repositories\BoothMapping\BoothMappingRepositories.cs`

**Before:**
```csharp
_connectionString = ConfigurationManager.ConnectionStrings["OracleDb"].ConnectionString;
// This used: User Id=ws;Password=ws
```

**After:**
```csharp
_connectionString = ConfigurationManager.ConnectionStrings["OracleDbWebsite"].ConnectionString;
// Now uses: User Id=website;Password=website
```

---

## ??? Complete Connection String Setup

| Connection Name | User/Password | Schema | Purpose |
|----------------|---------------|--------|---------|
| **OracleDbWebsite** | website/website | WEBSITE | ? Booth login (SP_USER_LOGIN) |
| OracleDb | ws/ws | WEBSITE | Booth operations |
| OracleDbUlberp | ulberp/ulberp | ULBERP | Other operations |
| OracleDbAbas | abas/abas | ABAS | Other operations |

---

## ?? Current Architecture

### Login Flow (BoothAuthRepository)
```
Login Request
    ?
BoothAuthRepository
    ?
OracleDbWebsite connection
    ?
User: website
Password: website
    ?
WEBSITE schema
    ?
SP_USER_LOGIN
    ?
Returns: userId, userName, role, token
```

### Booth Operations Flow (BoothRepository)
```
Statistics/Booths/Search/Update
    ?
BoothRepository
    ?
OracleDb connection
    ?
User: ws
Password: ws
    ?
WEBSITE schema
    ?
SP_GET_STATISTICS, SP_GET_ALL_BOOTHS, etc.
    ?
Returns: Booth data
```

---

## ? Build Status

- ? Web.config updated with new connection string
- ? Repository updated to use OracleDbWebsite
- ? Build successful
- ? No compilation errors

---

## ?? Testing Required

### 1. Verify Database User

First, confirm the `website` user exists in your Oracle database:

```sql
-- Connect as DBA or SYSTEM
SELECT username, account_status, default_tablespace, created
FROM dba_users
WHERE username = 'WEBSITE';
```

**Expected Result:**
- Username: WEBSITE
- Account Status: OPEN
- Has access to required tables and procedures

### 2. Verify SP_USER_LOGIN Access

Check that `website` user can access the stored procedure:

```sql
-- Connect as website/website
SELECT object_name, object_type, status
FROM user_objects
WHERE object_name = 'SP_USER_LOGIN';
```

**Expected Result:** One row showing the procedure exists and is VALID.

If the procedure doesn't exist in WEBSITE schema, check if it exists in another schema and needs a grant:

```sql
-- Connect as DBA
SELECT owner, object_name, object_type
FROM all_objects
WHERE object_name = 'SP_USER_LOGIN'
AND object_type = 'PROCEDURE';
```

### 3. Grant Access (If Needed)

If `SP_USER_LOGIN` is in another schema, grant execute permission:

```sql
-- Connect as the schema owner or DBA
GRANT EXECUTE ON owner_schema.SP_USER_LOGIN TO website;

-- Create synonym for easier access
CREATE OR REPLACE SYNONYM website.SP_USER_LOGIN 
FOR owner_schema.SP_USER_LOGIN;
```

### 4. Test Login Endpoint

After restarting the API:

#### Using Postman

```
POST http://localhost:5000/api/booth/login
Content-Type: application/json

{
  "userId": "12345678",
  "password": "password123"
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

#### Expected Success Response (200 OK)

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

---

## ?? Troubleshooting

### Error: "ORA-01017: invalid username/password"

**Possible Causes:**

1. **User doesn't exist**
   ```sql
   -- Create user if it doesn't exist (as DBA)
   CREATE USER website IDENTIFIED BY website
   DEFAULT TABLESPACE USERS
   TEMPORARY TABLESPACE TEMP
   QUOTA UNLIMITED ON USERS;
   
   GRANT CONNECT, RESOURCE TO website;
   ```

2. **Wrong password**
   - Verify credentials in Web.config match database
   - Passwords are case-sensitive in Oracle

3. **Account locked**
   ```sql
   -- Unlock account (as DBA)
   ALTER USER website ACCOUNT UNLOCK;
   ```

### Error: "ORA-00942: table or view does not exist"

**Cause:** `website` user can't access required objects

**Solution:**
```sql
-- Grant access to tables (as table owner or DBA)
GRANT SELECT, INSERT, UPDATE ON tbl_booths TO website;
GRANT SELECT ON tbl_users TO website;  -- If users table exists

-- Grant execute on procedures
GRANT EXECUTE ON sp_user_login TO website;
GRANT EXECUTE ON sp_get_statistics TO website;
-- etc.
```

### Error: "ORA-06550: procedure or function not found"

**Cause:** Procedure doesn't exist in WEBSITE schema

**Solutions:**

**Option 1: Create Synonym**
```sql
-- If procedure exists in another schema (as DBA)
CREATE OR REPLACE SYNONYM website.SP_USER_LOGIN 
FOR actual_owner.SP_USER_LOGIN;
```

**Option 2: Use Fully Qualified Name**

Update repository code to use schema prefix:
```csharp
// In BoothAuthRepository.Login()
using (OracleCommand cmd = new OracleCommand("actual_owner.SP_USER_LOGIN", conn))
```

**Option 3: Create Procedure in WEBSITE Schema**

Copy the procedure to WEBSITE schema (see database scripts in documentation).

---

## ?? Connection String Comparison

### Before Fix

```
BoothAuthRepository
    ? OracleDb
        ? User Id=ws;Password=ws
            ? WEBSITE schema via 'ws' user
                ? May not have all required permissions
```

### After Fix

```
BoothAuthRepository
    ? OracleDbWebsite
        ? User Id=website;Password=website
            ? WEBSITE schema via 'website' user
                ? Dedicated user with proper permissions
```

---

## ?? Next Steps

### Immediate Actions

1. **Verify Database User**
   ```sql
   -- Test connection
   sqlplus website/website@//SMKC-SCAN:1521/hcldb
   ```

2. **Check Procedure Access**
   ```sql
   -- As website user
   SELECT * FROM user_objects WHERE object_name = 'SP_USER_LOGIN';
   ```

3. **Restart API**
   ```
   Stop debugging (Shift+F5)
   Rebuild solution (Ctrl+Shift+B)
   Start debugging (F5)
   ```

4. **Test Login**
   - Use Postman collection
   - Verify successful authentication

### If User Doesn't Exist

Work with your DBA to:

1. **Create the website user**
2. **Grant necessary permissions**
3. **Create synonyms for shared objects**
4. **Test connection before using in API**

### Alternative: Use Existing ws User

If `website` user doesn't exist and you want to keep using `ws`:

1. **Revert Web.config change:**
   - Remove `OracleDbWebsite` connection string
   
2. **Revert Repository change:**
   ```csharp
   _connectionString = ConfigurationManager.ConnectionStrings["OracleDb"].ConnectionString;
   ```

3. **Verify ws user has all required permissions**

---

## ?? Summary

### What Changed

- ? Added `OracleDbWebsite` connection string with `website/website` credentials
- ? Updated `BoothAuthRepository` to use new connection string
- ? Build successful

### What to Verify

- ? Database user `website` exists and is unlocked
- ? User has access to `SP_USER_LOGIN` procedure
- ? User has necessary grants on tables
- ? Connection test successful
- ? Login endpoint works

### Connection Strings Available

| Name | Credentials | Purpose |
|------|-------------|---------|
| OracleDbWebsite | website/website | Booth login (NEW) |
| OracleDb | ws/ws | Booth operations |
| OracleDbUlberp | ulberp/ulberp | ULBERP operations |
| OracleDbAbas | abas/abas | ABAS operations |

---

## ?? Important Notes

1. **Database User Must Exist**
   - The `website` user must be created in Oracle database
   - Contact your DBA if user doesn't exist

2. **Permissions Required**
   - CONNECT privilege
   - EXECUTE on SP_USER_LOGIN
   - Access to any tables the procedure uses

3. **Alternative Approach**
   - If `website` user doesn't exist, you can continue using `ws/ws`
   - Just make sure `ws` user has all required permissions

4. **Testing is Critical**
   - Always test database connection before testing API
   - Verify procedure access before running application

---

**Status:** ? **Code Updated**  
**Build:** ? **Successful**  
**Database:** ? **Needs Verification**  
**Connection:** `User Id=website;Password=website`

**Next:** Verify database user exists and test the connection!
