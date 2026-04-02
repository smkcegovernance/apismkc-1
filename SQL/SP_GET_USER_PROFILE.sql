-- =====================================================
-- Stored Procedure: SP_GET_USER_PROFILE
-- Description: Fetch user profile details for deposit manager API.
--              Queries ULBERP.USERROLEDET for EMP_NAME, STATUS, DEPT_CODE
--              (same source as SP_UNIFIED_LOGIN).
--              For bank users (DEPT_CODE = 3), resolves bank name from
--              ABAS.TBL_BANKS using BANK_ID = USER_ID.
-- Database: ULBERP + ABAS
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
  V_BANK_NAME    VARCHAR2(300);
BEGIN
  IF P_USER_ID IS NULL OR TRIM(P_USER_ID) IS NULL THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User ID is required';
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
    RETURN;
  END IF;

  BEGIN
    -- USERROLEDET is the authoritative user store (same table as SP_UNIFIED_LOGIN)
    SELECT USER_ID, EMP_NAME, STATUS, DEPT_CODE
    INTO   V_USER_ID, V_NAME, V_STATUS, V_DEPT_CODE
    FROM   ULBERP.USERROLEDET
    WHERE  UPPER(TRIM(USER_ID)) = UPPER(TRIM(P_USER_ID));
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      O_SUCCESS := 0;
      O_MESSAGE := 'User not found';
      OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
      RETURN;
    WHEN OTHERS THEN
      O_SUCCESS := 0;
      O_MESSAGE := 'Error fetching user data: ' || SQLERRM;
      OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
      RETURN;
  END;

  V_BANK_ID   := NULL;
  V_BANK_NAME := NULL;

  -- For bank users (dept_code = 3), resolve bank name from ABAS.TBL_BANKS
  IF V_DEPT_CODE = 3 THEN
    V_BANK_ID := V_USER_ID;
    BEGIN
      SELECT TRIM(BANK_NAME) INTO V_BANK_NAME
      FROM   ABAS.TBL_BANKS
      WHERE  UPPER(TRIM(BANK_ID)) = UPPER(TRIM(V_USER_ID));
    EXCEPTION
      WHEN NO_DATA_FOUND THEN V_BANK_NAME := NULL;
      WHEN OTHERS       THEN V_BANK_NAME := NULL;
    END;
  END IF;

  O_SUCCESS := 1;
  O_MESSAGE := 'Profile fetched successfully';

  OPEN O_USER_DATA FOR
    SELECT
      V_USER_ID   AS USER_ID,
      V_NAME      AS NAME,
      V_STATUS    AS STATUS,
      V_DEPT_CODE AS ROLE_ID,
      CASE
        WHEN V_DEPT_CODE = 1 THEN 'commissioner'
        WHEN V_DEPT_CODE = 2 THEN 'account'
        WHEN V_DEPT_CODE = 3 THEN 'bank'
        ELSE 'unknown'
      END         AS ROLE,
      V_BANK_ID   AS BANK_ID,
      V_BANK_NAME AS BANK_NAME
    FROM DUAL;

EXCEPTION
  WHEN OTHERS THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'An unexpected error occurred: ' || SQLERRM;
    OPEN O_USER_DATA FOR SELECT NULL FROM DUAL WHERE 1 = 0;
END SP_GET_USER_PROFILE;
/

-- GRANT EXECUTE ON SP_GET_USER_PROFILE TO <API_USER>;
