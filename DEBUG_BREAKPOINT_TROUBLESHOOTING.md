# ?? Debug Breakpoints Not Hitting - Troubleshooting Guide

## ? Issues Fixed

Your project has been **cleaned and rebuilt** with proper debug symbols (`.pdb` files).

---

## ?? Quick Fixes (Try These First)

### 1. **Restart Visual Studio Debugging**
   - **Stop** debugging (Shift + F5)
   - **Close** all browser windows opened by debugging
   - **Rebuild** solution (Ctrl + Shift + B)
   - **Start** debugging again (F5)

### 2. **Verify Debug Configuration**
   - Check toolbar dropdown shows **"Debug"** (not "Release")
   - Menu: `Build` > `Configuration Manager`
   - Ensure "Active solution configuration" = **Debug**

### 3. **Clear Browser Cache**
   If using Chrome/Edge:
   - Press `F12` (Developer Tools)
   - Right-click refresh button
   - Select "Empty Cache and Hard Reload"

### 4. **Delete .suo and .user Files**
   ```powershell
   # Run from solution directory
   Remove-Item -Path ".vs" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "*.suo" -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "*.user" -Force -ErrorAction SilentlyContinue
   ```

---

## ?? Specific to Your ConsentDocumentController

### Verify Routes Are Registered

Your controller uses attribute routing:
```csharp
[RoutePrefix("api/deposits/consent")]
public class ConsentDocumentController : ApiController
{
    [HttpGet]
    [Route("health")]
    [AllowAnonymous]
    public HttpResponseMessage HealthCheck() { }
}
```

**Test URL:**
```
GET http://localhost:<port>/api/deposits/consent/health
```

### Check Authentication Filters

Your controller has `[ShaAuthentication]` filter which might prevent requests from reaching the controller:

```csharp
[HttpGet]
[Route("download")]
[ShaAuthentication]  // ? This validates signature
[RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
public HttpResponseMessage DownloadConsentDocument(...)
```

**Solutions:**

1. **Test with Public Endpoint First:**
   - Set breakpoint in `HealthCheck()` method (has `[AllowAnonymous]`)
   - Call: `GET /api/deposits/consent/health`
   - This should hit without authentication

2. **Check Authentication Handler:**
   - Set breakpoint in `Security\ApiKeyAuthenticationHandler.cs`
   - Verify request is reaching authentication layer

3. **Temporarily Disable Authentication:**
   ```csharp
   // FOR TESTING ONLY - Remove after debugging
   [AllowAnonymous]
   [HttpGet]
   [Route("download")]
   public HttpResponseMessage DownloadConsentDocument(...)
   ```

---

## ??? Advanced Troubleshooting

### Issue 1: IIS Express Not Loading Latest Code

**Symptoms:**
- Breakpoints show hollow circles
- Changes not reflected in API responses

**Solution:**
```powershell
# Stop IIS Express
Stop-Process -Name "iisexpress" -Force -ErrorAction SilentlyContinue

# Clear ASP.NET temporary files
Remove-Item -Path "$env:TEMP\Temporary ASP.NET Files\*" -Recurse -Force -ErrorAction SilentlyContinue

# Rebuild
MSBuild apismkc.sln /t:Rebuild /p:Configuration=Debug
```

### Issue 2: Wrong Process Attached

**Verify correct process:**
1. Menu: `Debug` > `Attach to Process` (Ctrl + Alt + P)
2. Find `iisexpress.exe` with your project name
3. Verify "Attached" column shows "Yes"

### Issue 3: Dependency Injection Issues

Your controller uses constructor injection:
```csharp
private readonly IFtpStorageService _ftpStorage;

public ConsentDocumentController(IFtpStorageService ftpStorage) 
{ 
    _ftpStorage = ftpStorage ?? throw new ArgumentNullException(nameof(ftpStorage));
}
```

**Check:**
1. Set breakpoint in **constructor**
2. Verify `SimpleDependencyResolver` is registering `IFtpStorageService`
3. Check `App_Start\SimpleDependencyResolver.cs`

### Issue 4: Route Not Registered

**Verify in Visual Studio:**
1. Set breakpoint in `Global.asax.cs` > `Application_Start()`
2. Verify `GlobalConfiguration.Configure(WebApiConfig.Register)` is called
3. Set breakpoint in `App_Start\WebApiConfig.cs` > `Register()` method
4. Verify `config.MapHttpAttributeRoutes()` is called

---

## ?? Testing Checklist

Use this checklist to verify debugging works:

### ? Step 1: Test Application Startup
- [ ] Breakpoint in `Global.asax.cs` > `Application_Start()` hits
- [ ] Breakpoint in `WebApiConfig.Register()` hits

### ? Step 2: Test Public Endpoint
- [ ] Breakpoint in `ConsentDocumentController` constructor hits
- [ ] Breakpoint in `HealthCheck()` method hits
- [ ] Response: `200 OK` with JSON

### ? Step 3: Test Authentication Handler
- [ ] Breakpoint in `ApiKeyAuthenticationHandler.SendAsync()` hits
- [ ] Authentication validates correctly

### ? Step 4: Test Protected Endpoint
- [ ] Breakpoint in `DownloadConsentDocument()` hits
- [ ] Parameters received correctly

---

## ?? Enable Module/Handler Tracing

Add to `Web.config` for detailed IIS Express logs:

```xml
<system.webServer>
  <tracing>
    <traceFailedRequests>
      <add path="*">
        <traceAreas>
          <add provider="ASP" verbosity="Verbose" />
          <add provider="ASPNET" areas="Infrastructure,Module,Page,AppServices" verbosity="Verbose" />
          <add provider="WWW Server" areas="Authentication,Security,Filter,StaticFile,CGI,Compression,Cache,RequestNotifications,Module,FastCGI,WebSocket,Rewrite" verbosity="Verbose" />
        </traceAreas>
        <failureDefinitions statusCodes="200-999" />
      </add>
    </traceFailedRequests>
  </tracing>
</system.webServer>
```

**View logs:**
```
%USERPROFILE%\Documents\IISExpress\TraceLogFiles\
```

---

## ?? Test Commands

### Test Health Endpoint (No Auth Required)
```powershell
Invoke-RestMethod -Uri "http://localhost:PORT/api/deposits/consent/health" -Method Get
```

### Test with Postman
1. Import collection from `Postman\` folder
2. Set environment variables
3. Send request to `/api/deposits/consent/health`
4. Check Visual Studio for breakpoint hit

---

## ?? Common Causes & Solutions Summary

| Symptom | Cause | Solution |
|---------|-------|----------|
| Hollow breakpoint circles | PDB files missing or mismatched | Clean + Rebuild |
| Breakpoint never hit | Wrong configuration (Release mode) | Switch to Debug mode |
| Breakpoint skipped | Code optimized | Verify `<Optimize>false</Optimize>` in .csproj |
| Request doesn't reach controller | Authentication filter blocks | Test with `[AllowAnonymous]` endpoint |
| Request doesn't reach controller | Route not registered | Check attribute routing setup |
| Constructor breakpoint not hit | DI container not creating instance | Check `SimpleDependencyResolver` |
| Code changes not reflected | Old assemblies cached | Stop IIS, delete bin/obj, rebuild |

---

## ?? Next Steps

1. **Restart Visual Studio** (File > Exit, then reopen)
2. **Press F5** to start debugging
3. **Test Health Endpoint:**
   ```
   GET http://localhost:<port>/api/deposits/consent/health
   ```
4. **Set breakpoint** in `HealthCheck()` method
5. **Verify breakpoint hits** when request is made

---

## ?? Still Not Working?

If breakpoints still don't hit after trying all solutions:

### Collect Information:
1. Visual Studio version: `Help` > `About Microsoft Visual Studio`
2. Configuration: Debug or Release?
3. Target endpoint: Which URL are you calling?
4. Error messages: Check Output window (View > Output)
5. Browser console: Any JavaScript errors?

### Enable Diagnostic Logging:
```xml
<!-- Add to Web.config -->
<system.diagnostics>
  <trace autoflush="true">
    <listeners>
      <add name="textListener" 
           type="System.Diagnostics.TextWriterTraceListener" 
           initializeData="C:\Logs\smkcapi_debug.log" />
    </listeners>
  </trace>
</system.diagnostics>
```

### Check Output Window:
- View > Output (Ctrl + Alt + O)
- Show output from: "Debug"
- Look for exceptions or routing errors

---

## ? Quick Reference

| Action | Shortcut |
|--------|----------|
| Start Debugging | F5 |
| Stop Debugging | Shift + F5 |
| Toggle Breakpoint | F9 |
| Step Over | F10 |
| Step Into | F11 |
| Rebuild Solution | Ctrl + Shift + B |
| Output Window | Ctrl + Alt + O |
| Attach to Process | Ctrl + Alt + P |

---

**Last Updated:** After clean build with debug symbols
**Status:** ? Project compiled successfully with .pdb files
**Next Action:** Restart Visual Studio and test with health endpoint
