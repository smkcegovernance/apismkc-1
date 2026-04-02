-- =====================================================
-- Stored Procedure: SP_CHANGE_USER_PASSWORD
-- Description: Change password for user after validating current password.
--              Uses ULBERP.USERROLEDET (same as login) as credential store.
--              Plain-text comparison consistent with SP_UNIFIED_LOGIN.
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
  V_STORED_PASSWORD  VARCHAR2(1000);
  V_STATUS           VARCHAR2(20);
  V_VALIDFLAG        VARCHAR2(1);
BEGIN
  IF P_USER_ID IS NULL OR TRIM(P_USER_ID) IS NULL THEN
    O_SUCCESS := 0; O_MESSAGE := 'User ID is required'; RETURN;
  END IF;
  IF P_OLD_PASSWORD IS NULL OR TRIM(P_OLD_PASSWORD) IS NULL THEN
    O_SUCCESS := 0; O_MESSAGE := 'Current password is required'; RETURN;
  END IF;
  IF P_NEW_PASSWORD IS NULL OR TRIM(P_NEW_PASSWORD) IS NULL THEN
    O_SUCCESS := 0; O_MESSAGE := 'New password is required'; RETURN;
  END IF;
  IF LENGTH(TRIM(P_NEW_PASSWORD)) < 8 THEN
    O_SUCCESS := 0; O_MESSAGE := 'New password must be at least 8 characters'; RETURN;
  END IF;

  BEGIN
    -- USERROLEDET is the authoritative credential store (same as login)
    SELECT EMP_PASSWORD, STATUS, VALIDFLAG
    INTO   V_STORED_PASSWORD, V_STATUS, V_VALIDFLAG
    FROM   ULBERP.USERROLEDET
    WHERE  UPPER(TRIM(USER_ID)) = UPPER(TRIM(P_USER_ID));
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      O_SUCCESS := 0; O_MESSAGE := 'User not found'; RETURN;
    WHEN OTHERS THEN
      O_SUCCESS := 0; O_MESSAGE := 'Error reading user: ' || SQLERRM; RETURN;
  END;

  IF V_VALIDFLAG <> 'Y' THEN
    O_SUCCESS := 0; O_MESSAGE := 'User account is locked'; RETURN;
  END IF;

  IF V_STATUS <> 'A' THEN
    O_SUCCESS := 0; O_MESSAGE := 'User account is inactive'; RETURN;
  END IF;

  IF TRIM(V_STORED_PASSWORD) <> TRIM(P_OLD_PASSWORD) THEN
    O_SUCCESS := 0; O_MESSAGE := 'Current password is incorrect'; RETURN;
  END IF;

  IF TRIM(P_OLD_PASSWORD) = TRIM(P_NEW_PASSWORD) THEN
    O_SUCCESS := 0; O_MESSAGE := 'New password must be different from the current password'; RETURN;
  END IF;

  UPDATE ULBERP.USERROLEDET
  SET    EMP_PASSWORD = P_NEW_PASSWORD
  WHERE  UPPER(TRIM(USER_ID)) = UPPER(TRIM(P_USER_ID));

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
