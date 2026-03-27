# ?? Fix: Duplicate DepositManager Folder

## Problem Solved ?

**Issue**: Duplicate `DepositManager` folder was being created inside the shared folder.

**Root Cause**: The code was creating a full folder structure (`DepositManager/BankConsent/...`) starting from the shared folder, when the shared folder itself should be treated as the root location.

---

## What Changed

### Before (Creating Duplicates):
```
Your Shared Folder (1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp)
??? DepositManager ? DUPLICATE!
    ??? BankConsent
        ??? REQ0000000001
            ??? BANK001
                ??? file.pdf
```

### After (Clean Structure):
```
Your Shared Folder (1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp)
??? REQ0000000001 ? Direct structure
    ??? BANK001
        ??? file.pdf
```

---

## Technical Details

### Upload Logic Changed

**Old Logic**:
1. Create `DepositManager` folder
2. Create `BankConsent` subfolder
3. Create requirement folder
4. Create bank folder
5. Upload file

**New Logic** (when using shared folder):
1. Use shared folder as root (skip DepositManager/BankConsent)
2. Create requirement folder directly in shared folder
3. Create bank folder
4. Upload file

### Code Changes

**File**: `Services\DepositManager\GoogleDriveStorageService.cs`

**Method**: `UploadConsentDocument()`

```csharp
// NEW: Check if using shared folder
if (_useServiceAccount && !string.IsNullOrEmpty(_sharedFolderId))
{
    // Skip DepositManager/BankConsent creation
    currentFolderId = _sharedFolderId;
}
else
{
    // Traditional flow for OAuth users
    // Create DepositManager and BankConsent
}
```

**Method**: `DownloadConsentDocument()`
- Updated to match the new structure
- Skips looking for DepositManager/BankConsent when using shared folder

---

## ?? Testing

### Clean Up Old Structure

**Before testing**, you may want to delete the duplicate `DepositManager` folder:

1. Go to: https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
2. Find the `DepositManager` folder inside
3. Right-click ? **Delete** (or move to trash)

### Test Upload

**Restart Application**:
```
Restart IIS or Application Pool
```

**Test with Postman**:
```http
POST http://localhost:5000/api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "REQ0000000001",
  "bankId": "TEST001",
  "fileName": "test.pdf",
  "fileData": "JVBERi0xLjQKJeLjz9MK..."
}
```

**Expected Result**:
- File uploaded successfully
- Check Google Drive: https://drive.google.com/drive/folders/1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
- Structure: `REQ0000000001/TEST001/test.pdf`
- **No** duplicate `DepositManager` folder!

### Check Logs

**Location**: `C:\smkcapi_published\Logs\FtpLog_YYYYMMDD.txt`

**Look for**:
```
Using shared folder as root (skipping DepositManager/BankConsent creation)
Shared Folder ID: 1Jb--DaiXGdshrfVzrADqLLyRLEtezgLp
```

---

## ?? Comparison

| Aspect | Before | After |
|--------|--------|-------|
| Folder Depth | 5 levels | 3 levels |
| Duplicate Folders | Yes | No |
| Structure | SharedFolder/DepositManager/BankConsent/REQ/BANK | SharedFolder/REQ/BANK |
| Cleaner | ? No | ? Yes |

---

## ?? Migration Notes

### If You Have Existing Files

**Option 1: Move Existing Files** (Recommended)
1. Open Google Drive
2. Navigate to the duplicate `DepositManager/BankConsent` folder
3. Move all requirement folders to the shared folder root
4. Delete empty `DepositManager` folder

**Option 2: Keep Both Structures**
- Old files remain in `DepositManager/BankConsent/...`
- New files go directly to `REQ.../BANK.../`
- Download will fail for old files (they use old structure)

**Option 3: Re-upload All Files**
- Delete old folder structure
- Re-upload all files using the API
- New clean structure

---

## ? Benefits

1. **Cleaner Structure**: Fewer unnecessary folders
2. **No Duplicates**: Shared folder is treated as root
3. **Consistent**: Upload and download use same logic
4. **Efficient**: Less folder traversal
5. **Clear Intent**: Shared folder = storage root

---

## ?? If Issues Occur

### Files Not Uploading
**Check**: 
- Application restarted after code change
- Shared folder ID still in Web.config
- Folder still shared with service account

### Old Files Not Downloading
**Reason**: Old files are in `DepositManager/BankConsent/...` structure  
**Solution**: Move old files to root or re-upload them

### Still Creating Duplicate
**Check**:
- `GoogleDrive_UseServiceAccount = true` in Web.config
- `GoogleDrive_SharedFolderId` is not empty
- Application restarted

---

## ?? Summary

| Item | Status |
|------|--------|
| **Issue** | Duplicate DepositManager folder |
| **Cause** | Creating full structure in shared folder |
| **Fix** | Skip DepositManager/BankConsent when using shared folder |
| **Code Changed** | `GoogleDriveStorageService.cs` (Upload & Download) |
| **Build Status** | ? Successful |
| **Testing Required** | ? Yes - Test upload after restart |
| **Migration** | Optional - Move existing files to root |

---

## ?? Next Steps

1. ? Code fixed and built successfully
2. ? **Restart application**
3. ? **Clean up** old DepositManager folder (optional)
4. ? **Test upload** with Postman
5. ? **Verify** clean structure in Google Drive
6. ? **Test download** to confirm working

---

**Fixed**: January 2024  
**Files Changed**: `GoogleDriveStorageService.cs`  
**Build Status**: ? Success  
**Ready to Deploy**: ? Yes
