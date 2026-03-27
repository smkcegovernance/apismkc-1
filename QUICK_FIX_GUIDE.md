# ?? Debug Breakpoints Not Hitting - QUICK FIX GUIDE

## ? Your Project Status: ALL CHECKS PASSED ?

Your project is **properly configured for debugging**:
- ? Debug symbols (.pdb) generated
- ? Web.config has debug=true
- ? Project configured for full debugging
- ? Routes properly configured
- ? Controller exists and accessible

---

## ?? MOST COMMON CAUSES (Try These First!)

### 1. Using "Start Without Debugging" (Ctrl+F5) ?
**WRONG:** Ctrl+F5 (runs without debugger attached)  
**CORRECT:** Press **F5** (starts with debugger)

### 2. Configuration Set to "Release" ?
**Check toolbar dropdown:**
- Should show: `Debug` ?
- Not: `Release` ?

**How to change:**
1. Click dropdown next to "Start" button
2. Select "Debug"
3. Press F5 again

### 3. Visual Studio Not Fully Started ?
**Symptoms:**
- Breakpoint is hollow/empty circle
- Says "The breakpoint will not currently be hit"

**Solution:**
1. Wait 10-15 seconds after pressing F5
2. Once browser opens, try again
3. Check bottom status bar shows "Debugging"

### 4. Old Code Cached in IIS Express ?
**Solution - Quick Fix:**
```powershell
# Stop and clean
Stop-Process -Name "iisexpress" -Force -ErrorAction SilentlyContinue
Remove-Item "bin\*" -Recurse -Force
Remove-Item "obj\*" -Recurse -Force

# Rebuild
MSBuild apismkc.sln /t:Rebuild /p:Configuration=Debug

# Restart Visual Studio, press F5
```

**Solution - Use Script:**
```powershell
.\Fix-Debug-Issues.ps1
```

---

## ?? STEP-BY-STEP TESTING PROCEDURE

### Step 1: Set Breakpoint in Health Endpoint
```csharp
[HttpGet]
[Route("health")]
[AllowAnonymous]  // ? No authentication required
public HttpResponseMessage HealthCheck()
{
    // ? SET BREAKPOINT HERE (Click left margin, red circle appears)
    return Request.CreateResponse(HttpStatusCode.OK, new
    {
        success = true,
        message = "ConsentDocumentController is accessible",
        // ...
    });
}
```

**Why this endpoint?**
- ? No authentication required
- ? Simple GET request
- ? Easy to test in browser

### Step 2: Start Debugging
1. Press **F5** (not Ctrl+F5!)
2. Wait for browser to open
3. Browser may show empty page - **that's OK**

### Step 3: Call the Endpoint
**Option A: Use Browser**
```
http://localhost:57031/api/deposits/consent/health
```

**Option B: Use PowerShell**
```powershell
.\Test-API-Endpoint.ps1
```

**Option C: Use Postman**
```
GET http://localhost:57031/api/deposits/consent/health
No headers needed
```

### Step 4: Verify Breakpoint Hits
? Visual Studio should pause execution  
? Yellow arrow appears on breakpoint line  
? Variables window shows values  
? You can step through code (F10)

---

## ?? IF BREAKPOINT STILL DOESN'T HIT

### Check 1: Verify Debugger is Attached
**Bottom status bar should show:**
```
Ready  |  Debugging  |  ? Stop
```

**If it shows "Running" instead:**
- You used Ctrl+F5 (wrong!)
- Stop (Shift+F5)
- Press F5 to start with debugger

### Check 2: Verify Breakpoint is Valid
**Breakpoint should be:**
- ? Solid red circle ?
- ? Hollow circle ? (means PDB mismatch)

**If hollow:**
```powershell
.\Fix-Debug-Issues.ps1
```

### Check 3: Verify Request Reaches API
**Check Output window** (Ctrl+Alt+O):
```
Show output from: Debug
```

**Look for:**
- ? Application started messages
- ? HTTP request logs
- ? Exceptions or errors

### Check 4: Authentication Not Blocking
Your controller has **two** types of endpoints:

**Public (works without auth):**
```csharp
[HttpGet]
[Route("health")]
[AllowAnonymous]  // ? Test this first!
```

**Protected (needs auth):**
```csharp
[HttpGet]
[Route("download")]
[ShaAuthentication]  // ? Requires API key
[RateLimit(...)]
```

**Always test public endpoint first!**

### Check 5: Correct URL
Your API runs at: **http://localhost:57031/**

**Health endpoint full URL:**
```
http://localhost:57031/api/deposits/consent/health
```

**Common mistakes:**
- ? Wrong port number
- ? Missing /api/ prefix
- ? HTTPS instead of HTTP
- ? Trailing slash or extra characters

---

## ??? AUTOMATED FIX SCRIPTS

### Script 1: Verify Everything
```powershell
.\Verify-Debug-Config.ps1
```
**Shows:** 8 configuration checks with pass/fail status

### Script 2: Fix Issues
```powershell
.\Fix-Debug-Issues.ps1
```
**Does:**
1. Stops IIS Express
2. Clears cache
3. Deletes bin/obj
4. Rebuilds with debug symbols

### Script 3: Test API
```powershell
.\Test-API-Endpoint.ps1
```
**Tests:** Health endpoint and shows response

---

## ?? STILL NOT WORKING?

### Detailed Troubleshooting

**Read comprehensive guide:**
```
DEBUG_BREAKPOINT_TROUBLESHOOTING.md
```

### Checklist for Support
If you need help, collect this information:

```powershell
# 1. Verify configuration
.\Verify-Debug-Config.ps1 > debug-report.txt

# 2. Check build
MSBuild apismkc.sln /t:Build /p:Configuration=Debug /v:detailed > build-log.txt

# 3. Get errors
Get-Content "Web.config" | Select-String "debug" >> debug-report.txt
Get-Content "SmkcApi.csproj" | Select-String "Debug|Optimize" >> debug-report.txt
```

### Common Questions

**Q: I pressed F5 but nothing happens?**
A: Check Visual Studio is set as startup project:
   - Right-click SmkcApi project
   - "Set as Startup Project"
   - Try F5 again

**Q: Browser opens but shows error page?**
A: That's OK! The breakpoint should still hit. Check Visual Studio.

**Q: Getting 404 Not Found?**
A: 
- Verify URL: `http://localhost:57031/api/deposits/consent/health`
- Check WebApiConfig.cs has `config.MapHttpAttributeRoutes()`
- Restart debugging (Shift+F5, then F5)

**Q: Getting 401 Unauthorized?**
A: 
- Use `/health` endpoint (has `[AllowAnonymous]`)
- Don't test `/download` without auth headers

**Q: Breakpoint has red circle but never hits?**
A:
1. Set breakpoint in `Global.asax.cs` > `Application_Start()`
2. If that hits ? API is starting
3. If health endpoint doesn't hit ? routing issue
4. Check Output window for exceptions

---

## ? SUCCESS INDICATORS

### When Everything Works:

1. **Press F5**
   - Visual Studio compiles
   - Browser opens (may show blank page)
   - Status bar: "Debugging"

2. **Call health endpoint**
   - Visual Studio comes to foreground
   - Execution pauses at breakpoint
   - Yellow arrow on breakpoint line

3. **You can now:**
   - Hover over variables to see values
   - Press F10 to step over
   - Press F11 to step into
   - Press F5 to continue
   - Press Shift+F5 to stop

---

## ?? Quick Reference

| Action | Shortcut | Purpose |
|--------|----------|---------|
| Start Debugging | **F5** | Attach debugger and run |
| Start Without Debugging | Ctrl+F5 | Run only (no breakpoints) |
| Stop Debugging | Shift+F5 | Stop and detach |
| Toggle Breakpoint | **F9** | Add/remove breakpoint |
| Step Over | F10 | Execute current line |
| Step Into | F11 | Go into method |
| Continue | F5 | Resume until next breakpoint |
| Output Window | Ctrl+Alt+O | View logs |

---

## ?? BOTTOM LINE

### Your project is ready for debugging ?

**If breakpoints don't hit, it's probably:**
1. Using Ctrl+F5 instead of F5 (80% of cases)
2. Configuration set to Release (15% of cases)
3. Need to restart Visual Studio (5% of cases)

**Try this sequence:**
1. Close Visual Studio
2. Run: `.\Fix-Debug-Issues.ps1`
3. Open Visual Studio
4. Press **F5** (not Ctrl+F5)
5. Call: `http://localhost:57031/api/deposits/consent/health`

**Should work! ??**

---

**Files Created:**
- ? DEBUG_BREAKPOINT_TROUBLESHOOTING.md (detailed guide)
- ? Fix-Debug-Issues.ps1 (automated fix)
- ? Test-API-Endpoint.ps1 (test script)
- ? Verify-Debug-Config.ps1 (check configuration)
- ? QUICK_FIX_GUIDE.md (this file)

**Project URL:** http://localhost:57031/  
**Test Endpoint:** http://localhost:57031/api/deposits/consent/health  
**Status:** ? Ready for debugging
