-- =====================================================
-- Stored Procedure: SP_UNIFIED_LOGIN
-- Description: Unified login for all user types (Bank, Account, Commissioner)
--              Uses USERROLEDET for role resolution and TBL_BANK only for bank users
-- Database: ULBERP
-- Tables: ULBERP.USERDET, ULBERP.USERROLEDET, ABAS.TBL_BANK
-- Author: SMKC API Team
-- Date: January 2025
-- =====================================================

CREATE OR REPLACE PROCEDURE SP_UNIFIED_LOGIN (
  P_USER_ID IN VARCHAR2,
  P_PASSWORD IN VARCHAR2,
  O_SUCCESS OUT NUMBER,
  O_MESSAGE OUT VARCHAR2,
  O_USER_DATA OUT SYS_REFCURSOR
)
AS
  V_COUNT NUMBER;
  V_USER_ID VARCHAR2(50);
  V_USER_NAME VARCHAR2(100);
  V_STATUS VARCHAR2(20);
  V_ROLE VARCHAR2(50);
  V_ROLE_ID NUMBER;
  V_STORED_PASSWORD VARCHAR2(1000);
  V_DECODED_PASSWORD VARCHAR2(1000);
  V_VALIDFLAG VARCHAR2(5);
  V_USER_LOCK VARCHAR2(5);
  V_USER_FROM DATE;
  V_USER_TO DATE;
BEGIN
  -- Validate input parameters
  IF P_USER_ID IS NULL OR P_PASSWORD IS NULL THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User ID and password are required';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
    RETURN;
  END IF;

  -- Retrieve user details from ULBERP.USERDET table
  BEGIN
    SELECT 
      USER_ID,
      USER_NAME,
      USER_STATUS,
      USER_PASSWD,
      DEPT_CODE,
      USER_VIFLAG,
      USER_LOCK,
      USER_FROM,
      USER_TO
    INTO 
      V_USER_ID,
      V_USER_NAME,
      V_STATUS,
      V_STORED_PASSWORD,
      V_ROLE_ID,
      V_VALIDFLAG,
      V_USER_LOCK,
      V_USER_FROM,
      V_USER_TO
    FROM ULBERP.USERDET
    WHERE UPPER(TRIM(USER_ID)) = UPPER(TRIM(P_USER_ID));

    V_COUNT := 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      V_COUNT := 0;
    WHEN OTHERS THEN
      O_SUCCESS := 0;
      O_MESSAGE := 'Database error: ' || SQLERRM;
      OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
      RETURN;
  END;

  -- Check if user exists
  IF V_COUNT = 0 THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'Invalid user ID or password';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
    RETURN;
  END IF;

  -- Decode Base64 encoded password
  BEGIN
    V_DECODED_PASSWORD := UTL_RAW.CAST_TO_VARCHAR2(
      UTL_ENCODE.BASE64_DECODE(UTL_RAW.CAST_TO_RAW(V_STORED_PASSWORD))
    );
  EXCEPTION
    WHEN OTHERS THEN
      -- If decoding fails, assume password is not encoded
      V_DECODED_PASSWORD := V_STORED_PASSWORD;
  END;

  -- Validate password
  IF TRIM(V_DECODED_PASSWORD) != TRIM(P_PASSWORD) THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'Invalid user ID or password';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
    RETURN;
  END IF;

  -- Check user status (case-insensitive A = Active)
  IF UPPER(NVL(TRIM(V_STATUS), 'I')) != 'A' THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User account is inactive';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
    RETURN;
  END IF;

  -- Respect lock flag when present
  IF UPPER(NVL(TRIM(V_USER_LOCK), 'N')) = 'Y' THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User account is locked';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
    RETURN;
  END IF;

  -- Enforce validity date range
  IF TRUNC(SYSDATE) < NVL(TRUNC(V_USER_FROM), TRUNC(SYSDATE))
     OR TRUNC(SYSDATE) > NVL(TRUNC(V_USER_TO), TRUNC(SYSDATE)) THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User account is outside valid date range';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
    RETURN;
  END IF;

  CASE V_ROLE_ID
    WHEN 1 THEN V_ROLE := 'commissioner';
    WHEN 2 THEN V_ROLE := 'account';
    WHEN 3 THEN V_ROLE := 'bank';
    ELSE V_ROLE := 'operator';
  END CASE;

  -- Login successful - return user data with role ID
  O_SUCCESS := 1;
  O_MESSAGE := 'Login successful';

  OPEN O_USER_DATA FOR
    SELECT 
      V_USER_ID AS USER_ID,
      V_ROLE AS ROLE,
      V_USER_NAME AS NAME,
      V_STATUS AS STATUS,
      CAST(NULL AS VARCHAR2(100)) AS BANK_ID,
      CAST(NULL AS VARCHAR2(300)) AS BANK_NAME,
      V_ROLE_ID AS ROLE_ID     -- Role ID: 1=Commissioner, 2=Account, 3=Bank
    FROM DUAL;

EXCEPTION
  WHEN OTHERS THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'An unexpected error occurred: ' || SQLERRM;
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
END SP_UNIFIED_LOGIN;
/

-- =====================================================
-- Grant Execute Permission
-- =====================================================
-- GRANT EXECUTE ON SP_UNIFIED_LOGIN TO <API_USER>;

-- =====================================================
-- Role ID Mapping
-- =====================================================
-- 1 = Commissioner
-- 2 = Account User
-- 3 = Bank User

-- =====================================================
-- Test Cases
-- =====================================================
-- Test 1: Commissioner login
/*
DECLARE
  v_success NUMBER;
  v_message VARCHAR2(4000);
  v_cursor SYS_REFCURSOR;
  v_user_id VARCHAR2(50);
  v_role VARCHAR2(50);
  v_name VARCHAR2(100);
  v_status VARCHAR2(20);
  v_role_id NUMBER;
BEGIN
  SP_UNIFIED_LOGIN(
    P_USER_ID => 'COMMIS01',
    P_PASSWORD => 'TestPassword123',
    O_SUCCESS => v_success,
    O_MESSAGE => v_message,
    O_USER_DATA => v_cursor
  );
  
  DBMS_OUTPUT.PUT_LINE('Success: ' || v_success);
  DBMS_OUTPUT.PUT_LINE('Message: ' || v_message);
  
  IF v_success = 1 THEN
    FETCH v_cursor INTO v_user_id, v_role, v_name, v_status, v_role_id;
    DBMS_OUTPUT.PUT_LINE('User ID: ' || v_user_id);
    DBMS_OUTPUT.PUT_LINE('Role: ' || v_role);
    DBMS_OUTPUT.PUT_LINE('Role ID: ' || v_role_id);  -- Should be 1 for Commissioner
    CLOSE v_cursor;
  END IF;
END;
/
*/

-- Test 2: Account user login
/*
DECLARE
  v_success NUMBER;
  v_message VARCHAR2(4000);
  v_cursor SYS_REFCURSOR;
  v_user_id VARCHAR2(50);
  v_role VARCHAR2(50);
  v_name VARCHAR2(100);
  v_status VARCHAR2(20);
  v_role_id NUMBER;
BEGIN
  SP_UNIFIED_LOGIN(
    P_USER_ID => 'ACCOUNT1',
    P_PASSWORD => 'TestPassword123',
    O_SUCCESS => v_success,
    O_MESSAGE => v_message,
    O_USER_DATA => v_cursor
  );
  
  DBMS_OUTPUT.PUT_LINE('Success: ' || v_success);
  DBMS_OUTPUT.PUT_LINE('Message: ' || v_message);
  
  IF v_success = 1 THEN
    FETCH v_cursor INTO v_user_id, v_role, v_name, v_status, v_role_id;
    DBMS_OUTPUT.PUT_LINE('User ID: ' || v_user_id);
    DBMS_OUTPUT.PUT_LINE('Role: ' || v_role);
    DBMS_OUTPUT.PUT_LINE('Role ID: ' || v_role_id);  -- Should be 2 for Account User
    CLOSE v_cursor;
  END IF;
END;
/
*/

-- Test 3: Bank user login
/*
DECLARE
  v_success NUMBER;
  v_message VARCHAR2(4000);
  v_cursor SYS_REFCURSOR;
  v_user_id VARCHAR2(50);
  v_role VARCHAR2(50);
  v_name VARCHAR2(100);
  v_status VARCHAR2(20);
  v_role_id NUMBER;
BEGIN
  SP_UNIFIED_LOGIN(
    P_USER_ID => 'BANK001',
    P_PASSWORD => 'TestPassword123',
    O_SUCCESS => v_success,
    O_MESSAGE => v_message,
    O_USER_DATA => v_cursor
  );
  
  DBMS_OUTPUT.PUT_LINE('Success: ' || v_success);
  DBMS_OUTPUT.PUT_LINE('Message: ' || v_message);
  
  IF v_success = 1 THEN
    FETCH v_cursor INTO v_user_id, v_role, v_name, v_status, v_role_id;
    DBMS_OUTPUT.PUT_LINE('User ID: ' || v_user_id);
    DBMS_OUTPUT.PUT_LINE('Role: ' || v_role);
    DBMS_OUTPUT.PUT_LINE('Role ID: ' || v_role_id);  -- Should be 3 for Bank User
    CLOSE v_cursor;
  END IF;
END;
/
*/

-- Test 4: Invalid credentials
/*
DECLARE
  v_success NUMBER;
  v_message VARCHAR2(4000);
  v_cursor SYS_REFCURSOR;
BEGIN
  SP_UNIFIED_LOGIN(
    P_USER_ID => 'BANK001',
    P_PASSWORD => 'WrongPassword',
    O_SUCCESS => v_success,
    O_MESSAGE => v_message,
    O_USER_DATA => v_cursor
  );
  
  DBMS_OUTPUT.PUT_LINE('Success: ' || v_success);
  DBMS_OUTPUT.PUT_LINE('Message: ' || v_message);
  -- Expected: Success=0, Message='Invalid user ID or password'
END;
/
*/

-- =====================================================
-- Notes
-- =====================================================
-- 1. The procedure assumes ULBERP.USERDET has columns:
--    - USER_ID (VARCHAR2)
--    - EMP_NAME (VARCHAR2) - Employee name
--    - USER_STATUS (VARCHAR2) - 'A' for active
--    - USER_PASSWD (VARCHAR2) - Base64 encoded password
--    - USER_ROLE (VARCHAR2) - Role name (e.g., 'commissioner', 'account', 'bank')
--    - USER_ROLE_ID (NUMBER) - Role ID (1, 2, or 3)
--
-- 2. If USER_ROLE and USER_ROLE_ID columns don't exist,
--    they need to be added to ULBERP.USERDET table:
--    ALTER TABLE ULBERP.USERDET ADD (
--      USER_ROLE VARCHAR2(50),
--      USER_ROLE_ID NUMBER
--    );
--
-- 3. Role ID mapping:
--    1 = Commissioner (highest authority)
--    2 = Account User (middle authority)
--    3 = Bank User (lowest authority)
