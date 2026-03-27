-- ============================================================================
-- VOTING STATISTICS 2025 - Database Setup Script
-- Schema: WEBSITE
-- Description: Creates all database objects for Voting Statistics API
-- ============================================================================

-- Connect as WEBSITE user
-- sqlplus website/website@//SMKC-SCAN:1521/hcldb

-- ============================================================================
-- 1. CREATE TABLE
-- ============================================================================

CREATE TABLE WEBSITE.VOTING_STATISTICS_2025 (
    ID NUMBER PRIMARY KEY,
    TOTAL_VOTERS NUMBER NOT NULL,
    MALE_VOTERS NUMBER NOT NULL,
    FEMALE_VOTERS NUMBER NOT NULL,
    OTHER_VOTERS NUMBER NOT NULL,
    CASTED_VOTES NUMBER NOT NULL,
    MALE_CASTED NUMBER NOT NULL,
    FEMALE_CASTED NUMBER NOT NULL,
    OTHER_CASTED NUMBER NOT NULL,
    TIME_SLOT VARCHAR2(50) NOT NULL,
    OVERALL_TURNOUT_PERCENT NUMBER(5,2),
    MALE_TURNOUT_PERCENT NUMBER(5,2),
    FEMALE_TURNOUT_PERCENT NUMBER(5,2),
    OTHER_TURNOUT_PERCENT NUMBER(5,2),
    CREATED_DATE TIMESTAMP DEFAULT SYSTIMESTAMP,
    UPDATED_DATE TIMESTAMP DEFAULT SYSTIMESTAMP,
    CREATED_BY VARCHAR2(100),
    UPDATED_BY VARCHAR2(100),
    IS_ACTIVE CHAR(1) DEFAULT 'Y' CHECK (IS_ACTIVE IN ('Y', 'N')),
    REMARKS VARCHAR2(500)
);

-- Add comments
COMMENT ON TABLE WEBSITE.VOTING_STATISTICS_2025 IS 'Stores voting statistics for SMKC Election 2025';
COMMENT ON COLUMN WEBSITE.VOTING_STATISTICS_2025.ID IS 'Primary key - auto-generated from sequence';
COMMENT ON COLUMN WEBSITE.VOTING_STATISTICS_2025.TOTAL_VOTERS IS 'Total number of registered voters';
COMMENT ON COLUMN WEBSITE.VOTING_STATISTICS_2025.IS_ACTIVE IS 'Y = Active record, N = Inactive (only one active record at a time)';

-- ============================================================================
-- 2. CREATE SEQUENCE
-- ============================================================================

CREATE SEQUENCE WEBSITE.SEQ_VOTING_STATS_2025
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- ============================================================================
-- 3. CREATE INDEXES
-- ============================================================================

-- Index for quick lookup of active record
CREATE INDEX WEBSITE.IDX_VOTING_STATS_ACTIVE 
ON WEBSITE.VOTING_STATISTICS_2025(IS_ACTIVE, UPDATED_DATE DESC);

-- Index for date range queries
CREATE INDEX WEBSITE.IDX_VOTING_STATS_DATE 
ON WEBSITE.VOTING_STATISTICS_2025(UPDATED_DATE);

-- ============================================================================
-- 4. CREATE STORED PROCEDURE: SP_GET_VOTING_STATISTICS
-- ============================================================================

CREATE OR REPLACE PROCEDURE WEBSITE.SP_GET_VOTING_STATISTICS (
    o_success OUT NUMBER,
    o_message OUT VARCHAR2,
    o_statistics OUT SYS_REFCURSOR
) AS
    v_count NUMBER;
BEGIN
    -- Check if any active records exist
    SELECT COUNT(*) INTO v_count
    FROM WEBSITE.VOTING_STATISTICS_2025
    WHERE IS_ACTIVE = 'Y';
    
    IF v_count = 0 THEN
        o_success := 0;
        o_message := 'No active voting statistics found';
        OPEN o_statistics FOR SELECT * FROM DUAL WHERE 1=0;
        RETURN;
    END IF;
    
    -- Return the latest active record with calculated percentages
    OPEN o_statistics FOR
        SELECT 
            ID,
            TOTAL_VOTERS,
            MALE_VOTERS,
            FEMALE_VOTERS,
            OTHER_VOTERS,
            CASTED_VOTES,
            MALE_CASTED,
            FEMALE_CASTED,
            OTHER_CASTED,
            TIME_SLOT,
            ROUND((CASTED_VOTES / NULLIF(TOTAL_VOTERS, 0)) * 100, 2) AS OVERALL_TURNOUT_PERCENT,
            ROUND((MALE_CASTED / NULLIF(MALE_VOTERS, 0)) * 100, 2) AS MALE_TURNOUT_PERCENT,
            ROUND((FEMALE_CASTED / NULLIF(FEMALE_VOTERS, 0)) * 100, 2) AS FEMALE_TURNOUT_PERCENT,
            ROUND((OTHER_CASTED / NULLIF(OTHER_VOTERS, 0)) * 100, 2) AS OTHER_TURNOUT_PERCENT,
            CREATED_DATE,
            UPDATED_DATE,
            CREATED_BY,
            UPDATED_BY,
            IS_ACTIVE,
            REMARKS
        FROM WEBSITE.VOTING_STATISTICS_2025
        WHERE IS_ACTIVE = 'Y'
        ORDER BY UPDATED_DATE DESC
        FETCH FIRST 1 ROW ONLY;
    
    o_success := 1;
    o_message := 'Statistics retrieved successfully';
    
EXCEPTION
    WHEN OTHERS THEN
        o_success := 0;
        o_message := 'Error: ' || SQLERRM;
        OPEN o_statistics FOR SELECT * FROM DUAL WHERE 1=0;
END SP_GET_VOTING_STATISTICS;
/

-- ============================================================================
-- 5. CREATE STORED PROCEDURE: SP_UPDATE_VOTING_STATISTICS
-- ============================================================================

CREATE OR REPLACE PROCEDURE WEBSITE.SP_UPDATE_VOTING_STATISTICS (
    p_total_voters IN NUMBER,
    p_male_voters IN NUMBER,
    p_female_voters IN NUMBER,
    p_other_voters IN NUMBER,
    p_casted_votes IN NUMBER,
    p_male_casted IN NUMBER,
    p_female_casted IN NUMBER,
    p_other_casted IN NUMBER,
    p_time_slot IN VARCHAR2,
    p_updated_by IN VARCHAR2,
    o_result OUT VARCHAR2
) AS
    v_count NUMBER;
    v_id NUMBER;
    v_old_id NUMBER;
BEGIN
    -- Validate input parameters
    IF p_total_voters <= 0 THEN
        o_result := 'ERROR: Total voters must be greater than 0';
        RETURN;
    END IF;
    
    IF p_casted_votes > p_total_voters THEN
        o_result := 'ERROR: Casted votes cannot exceed total voters';
        RETURN;
    END IF;
    
    IF p_male_casted > p_male_voters THEN
        o_result := 'ERROR: Male casted votes cannot exceed male voters';
        RETURN;
    END IF;
    
    IF p_female_casted > p_female_voters THEN
        o_result := 'ERROR: Female casted votes cannot exceed female voters';
        RETURN;
    END IF;
    
    IF p_other_casted > p_other_voters THEN
        o_result := 'ERROR: Other casted votes cannot exceed other voters';
        RETURN;
    END IF;
    
    -- Check if active record exists
    SELECT COUNT(*) INTO v_count
    FROM WEBSITE.VOTING_STATISTICS_2025
    WHERE IS_ACTIVE = 'Y';
    
    IF v_count > 0 THEN
        -- Get existing ID for update message
        SELECT ID INTO v_old_id
        FROM WEBSITE.VOTING_STATISTICS_2025
        WHERE IS_ACTIVE = 'Y'
        FETCH FIRST 1 ROW ONLY;
        
        -- Update existing active record
        UPDATE WEBSITE.VOTING_STATISTICS_2025
        SET TOTAL_VOTERS = p_total_voters,
            MALE_VOTERS = p_male_voters,
            FEMALE_VOTERS = p_female_voters,
            OTHER_VOTERS = p_other_voters,
            CASTED_VOTES = p_casted_votes,
            MALE_CASTED = p_male_casted,
            FEMALE_CASTED = p_female_casted,
            OTHER_CASTED = p_other_casted,
            TIME_SLOT = p_time_slot,
            UPDATED_DATE = SYSTIMESTAMP,
            UPDATED_BY = p_updated_by
        WHERE IS_ACTIVE = 'Y';
        
        o_result := 'SUCCESS: Updated record ID ' || v_old_id;
    ELSE
        -- Insert new record
        SELECT WEBSITE.SEQ_VOTING_STATS_2025.NEXTVAL INTO v_id FROM DUAL;
        
        INSERT INTO WEBSITE.VOTING_STATISTICS_2025 (
            ID, 
            TOTAL_VOTERS, 
            MALE_VOTERS, 
            FEMALE_VOTERS, 
            OTHER_VOTERS,
            CASTED_VOTES, 
            MALE_CASTED, 
            FEMALE_CASTED, 
            OTHER_CASTED,
            TIME_SLOT, 
            CREATED_BY, 
            UPDATED_BY, 
            IS_ACTIVE,
            CREATED_DATE,
            UPDATED_DATE
        ) VALUES (
            v_id, 
            p_total_voters, 
            p_male_voters, 
            p_female_voters, 
            p_other_voters,
            p_casted_votes, 
            p_male_casted, 
            p_female_casted, 
            p_other_casted,
            p_time_slot, 
            p_updated_by, 
            p_updated_by, 
            'Y',
            SYSTIMESTAMP,
            SYSTIMESTAMP
        );
        
        o_result := 'SUCCESS: Created new record with ID ' || v_id;
    END IF;
    
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        o_result := 'ERROR: ' || SQLERRM;
END SP_UPDATE_VOTING_STATISTICS;
/

-- ============================================================================
-- 6. GRANT PERMISSIONS (Run as WEBSITE or DBA)
-- ============================================================================

-- Grant execute permissions on procedures to WS user (if API uses WS user)
GRANT EXECUTE ON WEBSITE.SP_GET_VOTING_STATISTICS TO ws;
GRANT EXECUTE ON WEBSITE.SP_UPDATE_VOTING_STATISTICS TO ws;

-- Grant table access to WS user (if needed for direct queries)
GRANT SELECT, INSERT, UPDATE ON WEBSITE.VOTING_STATISTICS_2025 TO ws;

-- Grant sequence usage
GRANT SELECT ON WEBSITE.SEQ_VOTING_STATS_2025 TO ws;

-- ============================================================================
-- 7. VERIFY INSTALLATION
-- ============================================================================

-- Check table exists
SELECT table_name, status 
FROM user_tables 
WHERE table_name = 'VOTING_STATISTICS_2025';

-- Check sequence exists
SELECT sequence_name, last_number 
FROM user_sequences 
WHERE sequence_name = 'SEQ_VOTING_STATS_2025';

-- Check procedures exist
SELECT object_name, object_type, status 
FROM user_objects 
WHERE object_name IN ('SP_GET_VOTING_STATISTICS', 'SP_UPDATE_VOTING_STATISTICS')
ORDER BY object_name;

-- Check indexes exist
SELECT index_name, status 
FROM user_indexes 
WHERE table_name = 'VOTING_STATISTICS_2025';

-- ============================================================================
-- 8. TEST DATA (Optional - for testing)
-- ============================================================================

-- Insert sample test data
DECLARE
    v_result VARCHAR2(4000);
BEGIN
    WEBSITE.SP_UPDATE_VOTING_STATISTICS(
        p_total_voters => 454430,
        p_male_voters => 224483,
        p_female_voters => 229865,
        p_other_voters => 82,
        p_casted_votes => 78171,
        p_male_casted => 44040,
        p_female_casted => 34127,
        p_other_casted => 4,
        p_time_slot => '7:30 AM - 11:30 AM',
        p_updated_by => 'SYSTEM_TEST',
        o_result => v_result
    );
    
    DBMS_OUTPUT.PUT_LINE('Result: ' || v_result);
END;
/

-- Verify test data was inserted
SELECT 
    ID,
    TOTAL_VOTERS,
    CASTED_VOTES,
    TIME_SLOT,
    ROUND((CASTED_VOTES / TOTAL_VOTERS) * 100, 2) AS TURNOUT_PERCENT,
    IS_ACTIVE,
    UPDATED_DATE
FROM WEBSITE.VOTING_STATISTICS_2025
WHERE IS_ACTIVE = 'Y';

-- ============================================================================
-- 9. CLEANUP SCRIPT (Use with caution!)
-- ============================================================================

-- Uncomment to drop all objects (DANGEROUS - USE ONLY IN DEVELOPMENT)
/*
DROP PROCEDURE WEBSITE.SP_GET_VOTING_STATISTICS;
DROP PROCEDURE WEBSITE.SP_UPDATE_VOTING_STATISTICS;
DROP TABLE WEBSITE.VOTING_STATISTICS_2025 CASCADE CONSTRAINTS;
DROP SEQUENCE WEBSITE.SEQ_VOTING_STATS_2025;
*/

-- ============================================================================
-- END OF SCRIPT
-- ============================================================================

-- Summary of created objects:
-- 1. Table: WEBSITE.VOTING_STATISTICS_2025
-- 2. Sequence: WEBSITE.SEQ_VOTING_STATS_2025
-- 3. Procedure: WEBSITE.SP_GET_VOTING_STATISTICS
-- 4. Procedure: WEBSITE.SP_UPDATE_VOTING_STATISTICS
-- 5. Index: WEBSITE.IDX_VOTING_STATS_ACTIVE
-- 6. Index: WEBSITE.IDX_VOTING_STATS_DATE

PROMPT '============================================================'
PROMPT 'Voting Statistics 2025 - Database Setup Complete'
PROMPT '============================================================'
PROMPT 'Created Objects:'
PROMPT '  - Table: VOTING_STATISTICS_2025'
PROMPT '  - Sequence: SEQ_VOTING_STATS_2025'
PROMPT '  - Procedure: SP_GET_VOTING_STATISTICS'
PROMPT '  - Procedure: SP_UPDATE_VOTING_STATISTICS'
PROMPT '  - Indexes: 2 indexes created'
PROMPT ''
PROMPT 'Next Steps:'
PROMPT '  1. Verify all objects: SELECT * FROM user_objects WHERE...'
PROMPT '  2. Test procedures with sample data (see script above)'
PROMPT '  3. Grant permissions if using different user'
PROMPT '  4. Update API Web.config connection string'
PROMPT '  5. Test API endpoints with Postman'
PROMPT '============================================================'
