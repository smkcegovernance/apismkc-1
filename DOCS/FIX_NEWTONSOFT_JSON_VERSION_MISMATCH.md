# Newtonsoft.Json Version Mismatch Fix

## Problem

Google Drive API initialization is failing with the error:

```
System.InvalidOperationException: Error deserializing JSON credential data.
  Inner Exception: TypeInitializationException: The type initializer for 'Google.Apis.Json.NewtonsoftJsonSerializer' threw an exception.
  Inner Exception: FileLoadException: Could not load file or assembly 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference.
```

## Root Cause

**Assembly Version Mismatch:**
- **Google.Apis** libraries require **Newtonsoft.Json 13.0.0.0**
- **Project (.csproj)** references **Newtonsoft.Json 6.0.4** (Version 6.0.0.0)
- **packages.config** has **Newtonsoft.Json 13.0.4** installed
- **Web.config** binding redirect was pointing to **6.0.0.0** (now fixed)

The project file (SmkcApi.csproj) contains an outdated reference that takes precedence over the packages.config, causing the wrong DLL to be copied to the bin folder.

## Current State

### ? Already Fixed:
- **Web.config** - Binding redirect updated to `13.0.0.0`
- **packages.config** - Has correct version `13.0.4`
- **packages folder** - Has Newtonsoft.Json 13.0.4

### ? Still Needs Fixing:
- **SmkcApi.csproj** - Still references version `6.0.4`
- **bin folder** - Has wrong DLL version `6.0.0.0` (locked by debugger)

## Solution

### Prerequisites
**?? IMPORTANT: Stop debugging first (Shift+F5)**

The bin folder files are locked by the debugger, so changes won't take effect until you stop debugging.

---

### Option 1: Automated Fix (Recommended)

Run the PowerShell script we created:

```powershell
# In Visual Studio Package Manager Console or PowerShell Terminal
.\FIX_NEWTONSOFT_JSON.ps1
```

This script will:
1. ? Backup your .csproj file
2. ? Update the Newtonsoft.Json reference to version 13.0.4
3. ? Clean bin and obj directories
4. ? Remove old package folder
5. ? Verify all settings

Then:
```
Build ? Rebuild Solution
```

---

### Option 2: Manual Fix

#### Step 1: Update Visual Studio Project

**Method A: Using NuGet Package Manager**
```
Tools ? NuGet Package Manager ? Manage NuGet Packages for Solution
? Installed ? Newtonsoft.Json ? Update (or Reinstall)
```

**Method B: Using Package Manager Console**
```powershell
Update-Package Newtonsoft.Json -Reinstall
```

**Method C: Manual .csproj Edit**

1. Unload the project (Right-click project ? Unload Project)
2. Edit SmkcApi.csproj (Right-click ? Edit SmkcApi.csproj)
3. Find:
```xml
<Reference Include="Newtonsoft.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL">
  <HintPath>packages\Newtonsoft.Json.6.0.4\lib\net45\Newtonsoft.Json.dll</HintPath>
</Reference>
```

4. Replace with:
```xml
<Reference Include="Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL">
  <HintPath>packages\Newtonsoft.Json.13.0.4\lib\net45\Newtonsoft.Json.dll</HintPath>
</Reference>
```

5. Save and reload the project

#### Step 2: Clean Build

```
Build ? Clean Solution
Build ? Rebuild Solution
```

Or via PowerShell:
```powershell
Remove-Item "bin" -Recurse -Force
Remove-Item "obj" -Recurse -Force
```

#### Step 3: Remove Old Package Folder

```powershell
Remove-Item "packages\Newtonsoft.Json.6.0.4" -Recurse -Force
```

---

## Verification

### 1. Check Project Reference
```powershell
Get-Content "SmkcApi.csproj" | Select-String -Pattern "Newtonsoft.Json" -Context 0,2
```

**Expected output:**
```xml
<Reference Include="Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL">
  <HintPath>packages\Newtonsoft.Json.13.0.4\lib\net45\Newtonsoft.Json.dll</HintPath>
</Reference>
```

### 2. Check Web.config Binding Redirect
```powershell
Get-Content "Web.config" | Select-String -Pattern "Newtonsoft.Json" -Context 0,2
```

**Expected output:**
```xml
<assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
<bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
```

### 3. Check DLL Version in bin Folder
```powershell
[System.Reflection.Assembly]::LoadFile("$PWD\bin\Newtonsoft.Json.dll").GetName().Version
```

**Expected output:**
```
Major  Minor  Build  Revision
-----  -----  -----  --------
13     0      0      0
```

### 4. Check Package Folders
```powershell
Get-ChildItem "packages\Newtonsoft.Json*" -Directory | Select-Object Name
```

**Expected output:**
```
Name
----
Newtonsoft.Json.13.0.4
```

(Should NOT show `Newtonsoft.Json.6.0.4`)

---

## Testing

After fixing and rebuilding:

### 1. Start Debugging (F5)

### 2. Check Application Logs
```powershell
Get-Content "Logs\FtpLog_*.txt" -Tail 50
```

**Look for:**
```
=== Google Drive Storage Service Initialized ===
? Credentials file found
? Service Account authentication successful (silent)
? Google Drive service initialized successfully
```

### 3. Test Health Endpoint
```http
GET http://localhost:57031/api/deposits/consent/googledrive/health
```

**Expected response:**
```json
{
  "success": true,
  "message": "GoogleDriveConsentController is accessible",
  "storageType": "Google Drive"
}
```

### 4. Test Upload Endpoint
```http
POST http://localhost:57031/api/deposits/consent/googledrive/upload
Content-Type: application/json

{
  "requirementId": "TEST001",
  "bankId": "TESTBANK",
  "fileName": "test.pdf",
  "fileData": "JVBERi0xLjQK..."
}
```

---

## Why This Happened

### The Problem Chain:
1. **Old Web.config binding redirect** ? Redirected all Newtonsoft.Json versions to 6.0.0.0
2. **Old .csproj reference** ? Copied Newtonsoft.Json 6.0.0.0 DLL to bin folder
3. **Google.Apis requirements** ? Expects Newtonsoft.Json 13.0.0.0
4. **Runtime mismatch** ? Application loads 6.0.0.0, Google.Apis asks for 13.0.0.0 ? **CRASH**

### The Fix Chain:
1. ? **Update Web.config** ? Redirect to 13.0.0.0
2. ? **Update .csproj reference** ? Point to 13.0.4 package
3. ? **Clean and rebuild** ? Copy correct DLL to bin folder
4. ? **Runtime match** ? Application loads 13.0.0.0, Google.Apis gets 13.0.0.0 ? **SUCCESS**

---

## Related Files

| File | Current Status | Expected Value |
|------|----------------|----------------|
| **packages.config** | ? Correct | `<package id="Newtonsoft.Json" version="13.0.4" ...>` |
| **Web.config** | ? Fixed | `<bindingRedirect ... newVersion="13.0.0.0" />` |
| **SmkcApi.csproj** | ? Needs Fix | `Version=13.0.0.0` and `HintPath>packages\Newtonsoft.Json.13.0.4\...` |
| **bin\Newtonsoft.Json.dll** | ? Wrong Version | Assembly version should be 13.0.0.0 |

---

## Common Issues

### Issue: "Could not load file or assembly 'Newtonsoft.Json, Version=13.0.0.0'"
**Cause:** Wrong DLL version in bin folder  
**Fix:** Clean and rebuild after updating .csproj reference

### Issue: "The type initializer for 'Google.Apis.Json.NewtonsoftJsonSerializer' threw an exception"
**Cause:** Version mismatch between what's loaded and what's expected  
**Fix:** Ensure Web.config binding redirect matches the DLL version

### Issue: "Access to path ... is denied" when trying to delete bin\Newtonsoft.Json.dll
**Cause:** Debugger is running and has the DLL locked  
**Fix:** Stop debugging first (Shift+F5)

### Issue: After fix, still getting version errors
**Cause:** Visual Studio cached the old DLL  
**Fix:** 
```
1. Stop debugging
2. Close Visual Studio
3. Delete bin and obj folders
4. Reopen Visual Studio
5. Rebuild solution
```

---

## Prevention for Future

When adding NuGet packages that depend on Newtonsoft.Json:

1. **Always use the latest compatible version** of Newtonsoft.Json
2. **Check binding redirects** in Web.config after adding packages
3. **Verify DLL versions** after build:
   ```powershell
   Get-ChildItem "bin\*.dll" | ForEach-Object { 
     try { 
       $version = [System.Reflection.AssemblyName]::GetAssemblyName($_.FullName).Version
       Write-Host "$($_.Name): $version"
     } catch {}
   }
   ```
4. **Use consistent versions** across all packages (avoid mixing old and new)

---

## Google.Apis Package Requirements

The Google.Apis packages we're using require:

| Package | Version | Requires Newtonsoft.Json |
|---------|---------|--------------------------|
| Google.Apis | 1.73.0 | ? 13.0.1 |
| Google.Apis.Auth | 1.73.0 | ? 13.0.1 |
| Google.Apis.Core | 1.73.0 | ? 13.0.1 |
| Google.Apis.Drive.v3 | 1.73.0.4045 | ? 13.0.1 |

**Our version:** 13.0.4 ? (meets requirement)

---

## Quick Command Reference

```powershell
# Check project reference
Get-Content "SmkcApi.csproj" | Select-String "Newtonsoft.Json" -Context 0,2

# Check packages.config
Get-Content "packages.config" | Select-String "Newtonsoft.Json"

# Check Web.config binding redirect
Get-Content "Web.config" | Select-String "Newtonsoft.Json" -Context 0,2

# Check DLL version in bin
[System.Reflection.Assembly]::LoadFile("$PWD\bin\Newtonsoft.Json.dll").GetName().Version

# Check package folders
Get-ChildItem "packages\Newtonsoft.Json*" -Directory

# Clean build folders (stop debugger first!)
Remove-Item "bin" -Recurse -Force
Remove-Item "obj" -Recurse -Force

# Remove old package
Remove-Item "packages\Newtonsoft.Json.6.0.4" -Recurse -Force

# Reinstall package (in Package Manager Console)
Update-Package Newtonsoft.Json -Reinstall
```

---

## Summary

**Problem:** Newtonsoft.Json version mismatch (6.0.0.0 loaded, 13.0.0.0 required)  
**Cause:** Outdated .csproj reference and Web.config binding redirect  
**Solution:** Update both to reference version 13.0.0.0, clean, and rebuild  
**Status:** Web.config ? Fixed | .csproj ? Needs manual fix after stopping debugger

---

**?? ACTION REQUIRED:**
1. **Stop debugging** (Shift+F5)
2. **Run:** `.\FIX_NEWTONSOFT_JSON.ps1`
3. **Rebuild** solution
4. **Start debugging** (F5)
5. **Test** Google Drive endpoints

---

**Expected Result:** Google Drive API initializes successfully without assembly version errors.
