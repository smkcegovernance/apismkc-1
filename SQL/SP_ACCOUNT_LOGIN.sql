-- =====================================================
-- Stored Procedure: SP_ACCOUNT_LOGIN
-- Description: Authenticates account department users
-- Database: ULBERP
-- Table: ULBERP.USERDET
-- Author: SMKC API Team
-- Date: January 2025
-- =====================================================

CREATE OR REPLACE PROCEDURE SP_ACCOUNT_LOGIN (
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
  V_STORED_PASSWORD VARCHAR2(1000);
  V_DECODED_PASSWORD VARCHAR2(1000);
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
      USER_PASSWD
    INTO 
      V_USER_ID,
      V_USER_NAME,
      V_STATUS,
      V_STORED_PASSWORD
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

  -- Check user status (A = Active)
  IF V_STATUS != 'A' THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User account is inactive';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
    RETURN;
  END IF;

  -- Login successful - return user data
  O_SUCCESS := 1;
  O_MESSAGE := 'Login successful';

  OPEN O_USER_DATA FOR
    SELECT 
      V_USER_ID AS USER_ID,
      'account' AS ROLE,          -- Account department role
      V_USER_NAME AS NAME,
      V_STATUS AS STATUS
    FROM DUAL;

EXCEPTION
  WHEN OTHERS THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'An unexpected error occurred: ' || SQLERRM;
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1=0;
END SP_ACCOUNT_LOGIN;
/

-- =====================================================
-- Grant Execute Permission
-- =====================================================
-- GRANT EXECUTE ON SP_ACCOUNT_LOGIN TO <API_USER>;

-- =====================================================
-- Test Cases
-- =====================================================
-- Test 1: Valid credentials
/*
DECLARE
  v_success NUMBER;
  v_message VARCHAR2(4000);
  v_cursor SYS_REFCURSOR;
BEGIN
  SP_ACCOUNT_LOGIN(
    P_USER_ID => 'testaccount',
    P_PASSWORD => 'TestPassword123',
    O_SUCCESS => v_success,
    O_MESSAGE => v_message,
    O_USER_DATA => v_cursor
  );
  
  DBMS_OUTPUT.PUT_LINE('Success: ' || v_success);
  DBMS_OUTPUT.PUT_LINE('Message: ' || v_message);
END;
/
*/

-- Test 2: Invalid credentials
/*
DECLARE
  v_success NUMBER;
  v_message VARCHAR2(4000);
  v_cursor SYS_REFCURSOR;
BEGIN
  SP_ACCOUNT_LOGIN(
    P_USER_ID => 'testaccount',
    P_PASSWORD => 'WrongPassword',
    O_SUCCESS => v_success,
    O_MESSAGE => v_message,
    O_USER_DATA => v_cursor
  );
  
  DBMS_OUTPUT.PUT_LINE('Success: ' || v_success);
  DBMS_OUTPUT.PUT_LINE('Message: ' || v_message);
END;
/
*/

-- Test 3: Missing parameters
/*
DECLARE
  v_success NUMBER;
  v_message VARCHAR2(4000);
  v_cursor SYS_REFCURSOR;
BEGIN
  SP_ACCOUNT_LOGIN(
    P_USER_ID => NULL,
    P_PASSWORD => 'TestPassword123',
    O_SUCCESS => v_success,
    O_MESSAGE => v_message,
    O_USER_DATA => v_cursor
  );
  
  DBMS_OUTPUT.PUT_LINE('Success: ' || v_success);
  DBMS_OUTPUT.PUT_LINE('Message: ' || v_message);
END;
/
*/
