-- =====================================================
-- Stored Procedure: SP_GET_USER_PROFILE
-- Description: Fetch user profile details for deposit manager API
--              Retrieves user info from USERDET and USERROLEDET
--              Bank ID is returned (bank name resolution handled by frontend)
-- Database: ULBERP
-- =====================================================

CREATE OR REPLACE PROCEDURE SP_GET_USER_PROFILE (
  P_USER_ID IN VARCHAR2,
  O_SUCCESS OUT NUMBER,
  O_MESSAGE OUT VARCHAR2,
  O_USER_DATA OUT SYS_REFCURSOR
)
AS
  V_USER_ID      VARCHAR2(100);
  V_NAME         VARCHAR2(200);
  V_STATUS       VARCHAR2(20);
  V_DEPT_CODE    NUMBER;
  V_BANK_ID      VARCHAR2(100);
BEGIN
  -- Validate input
  IF P_USER_ID IS NULL OR TRIM(P_USER_ID) IS NULL THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User ID is required';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
    RETURN;
  END IF;

  BEGIN
    -- Get user details from USERDET table
    SELECT
      U.USER_ID,
      U.USER_NAME,
      U.USER_STATUS,
      U.DEPT_CODE
    INTO
      V_USER_ID,
      V_NAME,
      V_STATUS,
      V_DEPT_CODE
    FROM ULBERP.USERDET U
    WHERE UPPER(TRIM(U.USER_ID)) = UPPER(TRIM(P_USER_ID));
    
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      O_SUCCESS := 0;
      O_MESSAGE := 'User not found in USERDET table';
      OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
      RETURN;
    WHEN OTHERS THEN
      O_SUCCESS := 0;
      O_MESSAGE := 'Error fetching user data: ' || SQLERRM;
      OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
      RETURN;
  END;

  -- For bank users (dept_code = 3), the USER_ID is the BANK_ID
  IF V_DEPT_CODE = 3 THEN
    V_BANK_ID := V_USER_ID;
  ELSE
    V_BANK_ID := NULL;
  END IF;

  -- Return profile data successfully
  O_SUCCESS := 1;
  O_MESSAGE := 'Profile fetched successfully';

  OPEN O_USER_DATA FOR
    SELECT
      V_USER_ID AS USER_ID,
      V_NAME AS NAME,
      V_STATUS AS STATUS,
      V_DEPT_CODE AS ROLE_ID,
      CASE
        WHEN V_DEPT_CODE = 3 THEN 'bank'
        WHEN V_DEPT_CODE = 2 THEN 'account'
        WHEN V_DEPT_CODE = 1 THEN 'commissioner'
        ELSE 'unknown'
      END AS ROLE,
      V_BANK_ID AS BANK_ID,
      NULL AS BANK_NAME
    FROM DUAL;

EXCEPTION
  WHEN OTHERS THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'An unexpected error occurred: ' || SQLERRM;
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
END SP_GET_USER_PROFILE;
/

-- GRANT EXECUTE ON SP_GET_USER_PROFILE TO <API_USER>;
