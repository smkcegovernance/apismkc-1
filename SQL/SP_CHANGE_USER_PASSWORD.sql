-- =====================================================
-- Stored Procedure: SP_CHANGE_USER_PASSWORD
-- Description: Change password for user after validating old password
--              Password is stored using existing Base64 convention
-- Database: ULBERP
-- =====================================================

CREATE OR REPLACE PROCEDURE SP_CHANGE_USER_PASSWORD (
  P_USER_ID       IN VARCHAR2,
  P_OLD_PASSWORD  IN VARCHAR2,
  P_NEW_PASSWORD  IN VARCHAR2,
  O_SUCCESS       OUT NUMBER,
  O_MESSAGE       OUT VARCHAR2
)
AS
  V_STORED_PASSWORD    VARCHAR2(1000);
  V_DECODED_PASSWORD   VARCHAR2(1000);
  V_ENCODED_PASSWORD   VARCHAR2(1000);
  V_STATUS             VARCHAR2(20);
BEGIN
  IF P_USER_ID IS NULL OR TRIM(P_USER_ID) IS NULL THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User ID is required';
    RETURN;
  END IF;

  IF P_OLD_PASSWORD IS NULL OR TRIM(P_OLD_PASSWORD) IS NULL THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'Old password is required';
    RETURN;
  END IF;

  IF P_NEW_PASSWORD IS NULL OR TRIM(P_NEW_PASSWORD) IS NULL THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'New password is required';
    RETURN;
  END IF;

  IF LENGTH(TRIM(P_NEW_PASSWORD)) < 8 THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'New password must be at least 8 characters';
    RETURN;
  END IF;

  BEGIN
    SELECT USER_PASSWD, USER_STATUS
    INTO V_STORED_PASSWORD, V_STATUS
    FROM ULBERP.USERDET
    WHERE UPPER(TRIM(USER_ID)) = UPPER(TRIM(P_USER_ID));
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      O_SUCCESS := 0;
      O_MESSAGE := 'User not found';
      RETURN;
  END;

  IF V_STATUS <> 'A' THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'User account is inactive';
    RETURN;
  END IF;

  BEGIN
    V_DECODED_PASSWORD := UTL_RAW.CAST_TO_VARCHAR2(
      UTL_ENCODE.BASE64_DECODE(UTL_RAW.CAST_TO_RAW(V_STORED_PASSWORD))
    );
  EXCEPTION
    WHEN OTHERS THEN
      V_DECODED_PASSWORD := V_STORED_PASSWORD;
  END;

  IF TRIM(V_DECODED_PASSWORD) <> TRIM(P_OLD_PASSWORD) THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'Old password is incorrect';
    RETURN;
  END IF;

  IF TRIM(P_OLD_PASSWORD) = TRIM(P_NEW_PASSWORD) THEN
    O_SUCCESS := 0;
    O_MESSAGE := 'New password must be different from old password';
    RETURN;
  END IF;

  V_ENCODED_PASSWORD := UTL_RAW.CAST_TO_VARCHAR2(
    UTL_ENCODE.BASE64_ENCODE(UTL_RAW.CAST_TO_RAW(P_NEW_PASSWORD))
  );

  UPDATE ULBERP.USERDET
  SET USER_PASSWD = V_ENCODED_PASSWORD
  WHERE UPPER(TRIM(USER_ID)) = UPPER(TRIM(P_USER_ID));

  COMMIT;

  O_SUCCESS := 1;
  O_MESSAGE := 'Password changed successfully';

EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    O_SUCCESS := 0;
    O_MESSAGE := 'An unexpected error occurred: ' || SQLERRM;
END SP_CHANGE_USER_PASSWORD;
/

-- GRANT EXECUTE ON SP_CHANGE_USER_PASSWORD TO <API_USER>;
