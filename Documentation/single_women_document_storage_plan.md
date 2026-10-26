# Single Women Registration - Document Storage Plan

## Purpose
This plan defines a compact, Oracle 12-safe way to store uploaded document records for the WCWC Single Women Registration module without using CLOB/BLOB or advanced licensed Oracle features.

## Architecture
- Actual file bytes are stored on the server filesystem
- Oracle stores metadata only
- This keeps DB size low and remains compatible with Oracle 12

## Oracle Tables
### 1) Main registration table
- `WCWC.TXN_WCWC_SINGLE_WOMEN_REG`

### 2) Scheme table
- `WCWC.TXN_WCWC_SWR_SCHEMES`

### 3) Document metadata table
- `WCWC.TXN_WCWC_SWR_DOCUMENTS`

## Recommended metadata columns
- `DOC_ID`
- `REG_ID`
- `DOC_CODE`
- `FILE_NAME`
- `ORIGINAL_NAME`
- `FILE_PATH`
- `FILE_SIZE`
- `MIME_TYPE`
- `IS_REQUIRED`
- `STATUS`
- `UPLOADED_AT`
- `HASH_VALUE`

## Storage folder pattern
`D:\SMKC\Uploads\WCWC\single-women\YYYY\MM\REG_ID\FILE_NAME`

## Why not BLOB/CLOB
- Large storage footprint in Oracle
- Harder to back up and restore
- Not necessary for this module
- Not needed for Oracle 12
- Avoids advanced Oracle feature requirements

## Required document codes
- `SINGLE_WOMAN_PROOF`
- `AADHAAR_COPY`
- other optional docs as needed

## Save logic
1. Validate input
2. Check registration exists
3. Save file to server folder
4. Save metadata row to Oracle
5. Return saved document metadata to frontend

## Download logic
1. Fetch metadata by `DOC_ID` or `REG_ID + DOC_CODE`
2. Read `FILE_PATH`
3. Check file exists
4. Return file stream to browser

## Delete logic
1. Delete file from server folder
2. Update status to `DELETED` in Oracle metadata table

## Security
- restrict file path to configured upload root
- validate extension
- validate file size
- confirm registration ownership
- never expose raw server path to the client

## Live deployment notes
- ensure Windows service account has write permissions
- ensure backup job includes upload folder
- monitor storage growth periodically
- keep file naming unique per upload

## Final recommendation
This is the correct production-safe pattern for the current Oracle 12 environment.
