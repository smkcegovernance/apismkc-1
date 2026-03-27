using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;
using SmkcApi.Models.DepositManager;
using SmkcApi.Services.DepositManager;

namespace SmkcApi.Controllers.DepositManager
{
    /// <summary>
    /// Diagnostic controller to test FTP connectivity and permissions.
    /// WARNING: These endpoints are NOT authenticated - for diagnostic purposes only.
    /// Remove or secure this controller before production deployment.
    /// </summary>
    [RoutePrefix("api/ftp-diagnostic")]
    [AllowAnonymous] // No authentication required for diagnostics
    public class FtpDiagnosticController : ApiController
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _basePath;

        public FtpDiagnosticController()
        {
            _host = System.Configuration.ConfigurationManager.AppSettings["Ftp_Host"] ?? "192.168.40.47";
            var portSetting = System.Configuration.ConfigurationManager.AppSettings["Ftp_Port"];
            _port = !string.IsNullOrEmpty(portSetting) ? int.Parse(portSetting) : 21;
            _user = System.Configuration.ConfigurationManager.AppSettings["Ftp_User"] ?? string.Empty;
            _password = System.Configuration.ConfigurationManager.AppSettings["Ftp_Password"] ?? string.Empty;
            _basePath = System.Configuration.ConfigurationManager.AppSettings["Ftp_BasePath"] ?? "/BankConsents";
        }

        /// <summary>
        /// Show network and server information to diagnose routing issues
        /// GET /api/ftp-diagnostic/network-info
        /// </summary>
        [HttpGet]
        [Route("network-info")]
        public IHttpActionResult GetNetworkInfo()
        {
            try
            {
                var localIPs = new System.Collections.Generic.List<string>();
                try
                {
                    var hostAddresses = System.Net.Dns.GetHostAddresses(Environment.MachineName);
                    foreach (var ip in hostAddresses)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            localIPs.Add(ip.ToString());
                        }
                    }
                }
                catch
                {
                    localIPs.Add("Unable to resolve");
                }

                // Test ping to FTP server
                bool canPingFtp = false;
                string pingResult = "";
                try
                {
                    using (var ping = new System.Net.NetworkInformation.Ping())
                    {
                        var reply = ping.Send(_host, 2000);
                        canPingFtp = reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                        pingResult = string.Format("{0} ({1}ms)", reply.Status, reply.RoundtripTime);
                    }
                }
                catch (Exception ex)
                {
                    pingResult = "Error: " + ex.Message;
                }

                // Test TCP connection to FTP port
                bool canConnectFtp = false;
                string tcpResult = "";
                try
                {
                    using (var client = new System.Net.Sockets.TcpClient())
                    {
                        var asyncResult = client.BeginConnect(_host, _port, null, null);
                        var success = asyncResult.AsyncWaitHandle.WaitOne(3000); // 3 second timeout
                        if (success && client.Connected)
                        {
                            canConnectFtp = true;
                            tcpResult = "Connected successfully";
                            client.EndConnect(asyncResult);
                        }
                        else
                        {
                            tcpResult = "Connection timeout (3 seconds)";
                        }
                    }
                }
                catch (Exception ex)
                {
                    tcpResult = "Error: " + ex.Message;
                }

                var info = new
                {
                    ServerName = Environment.MachineName,
                    ServerLocalIPs = localIPs,
                    RequestUri = Request.RequestUri.ToString(),
                    RequestHost = Request.RequestUri.Host,
                    RequestMethod = Request.Method.ToString(),
                    ClientIP = GetClientIP(),
                    UserAgent = Request.Headers.UserAgent?.ToString() ?? "Unknown",
                    FtpConfiguration = new
                    {
                        Host = _host,
                        Port = _port,
                        User = _user,
                        BasePath = _basePath,
                        FtpUri = string.Format("ftp://{0}:{1}{2}", _host, _port, _basePath)
                    },
                    NetworkTests = new
                    {
                        CanPingFtpServer = canPingFtp,
                        PingResult = pingResult,
                        CanConnectToFtpPort = canConnectFtp,
                        TcpConnectionResult = tcpResult
                    },
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Network diagnostic information",
                    Data = info
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Error gathering network info: " + ex.Message,
                    Error = ex.ToString()
                });
            }
        }

        private string GetClientIP()
        {
            try
            {
                // Try various sources for client IP
                var httpContext = System.Web.HttpContext.Current;
                if (httpContext != null && httpContext.Request != null)
                {
                    // Check X-Forwarded-For header first (for proxies)
                    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"];
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        return forwardedFor.Split(',')[0].Trim();
                    }

                    // Check X-Real-IP header
                    var realIP = httpContext.Request.Headers["X-Real-IP"];
                    if (!string.IsNullOrEmpty(realIP))
                    {
                        return realIP.Trim();
                    }

                    // Fall back to UserHostAddress
                    var userHostAddress = httpContext.Request.UserHostAddress;
                    if (!string.IsNullOrEmpty(userHostAddress))
                    {
                        return userHostAddress;
                    }
                }
                
                return "Unknown";
            }
            catch
            {
                return "Error getting IP";
            }
        }

        /// <summary>
        /// Show FTP configuration (WARNING: Shows sensitive info - remove in production!)
        /// GET /api/ftp-diagnostic/config
        /// </summary>
        [HttpGet]
        [Route("config")]
        public IHttpActionResult ShowConfig()
        {
            var config = new
            {
                Host = _host,
                Port = _port,
                User = _user,
                PasswordLength = _password?.Length ?? 0,
                PasswordFirstChar = _password?.Length > 0 ? _password[0].ToString() : "N/A",
                PasswordLastChar = _password?.Length > 0 ? _password[_password.Length - 1].ToString() : "N/A",
                PasswordContainsDollar = _password?.Contains("$") ?? false,
                BasePath = _basePath,
                FullUri = string.Format("ftp://{0}:{1}{2}", _host, _port, _basePath)
            };

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "FTP Configuration (Sensitive - Remove in production!)",
                Data = config
            });
        }

        /// <summary>
        /// Test FTP connectivity and permissions
        /// GET /api/ftp-diagnostic/test
        /// </summary>
        [HttpGet]
        [Route("test")]
        public IHttpActionResult TestFtpConnection()
        {
            var results = new StringBuilder();
            results.AppendLine("=== FTP Diagnostic Test ===");
            results.AppendLine();
            results.AppendLine(string.Format("Server: ftp://{0}:{1}", _host, _port));
            results.AppendLine(string.Format("User: {0}", _user));
            results.AppendLine(string.Format("Password Length: {0} chars", _password?.Length ?? 0));
            results.AppendLine(string.Format("Base Path: {0}", _basePath));
            results.AppendLine();

            // Test 1: Check FTP server connectivity
            results.AppendLine("Test 1: Testing FTP server connectivity...");
            try
            {
                var testUri = new Uri(string.Format("ftp://{0}:{1}/", _host, _port));
                var request = (FtpWebRequest)WebRequest.Create(testUri);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(_user, _password);
                request.Timeout = 10000;
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    results.AppendLine(string.Format("? SUCCESS: Connected to FTP server. Status: {0}", response.StatusDescription));
                    
                    // List root directory contents
                    using (var stream = response.GetResponseStream())
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        var listing = reader.ReadToEnd();
                        results.AppendLine("  Root directory listing:");
                        foreach (var line in listing.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            results.AppendLine("    - " + line);
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                if (ftpResponse != null)
                {
                    results.AppendLine(string.Format("? FAILED: FTP Error - Status: {0} ({1}), Message: {2}", 
                        (int)ftpResponse.StatusCode, ftpResponse.StatusCode, ftpResponse.StatusDescription));
                    
                    if (ftpResponse.StatusCode == FtpStatusCode.NotLoggedIn)
                    {
                        results.AppendLine("  ? Authentication failed. Possible causes:");
                        results.AppendLine("    - Incorrect username or password");
                        results.AppendLine("    - FTP user account locked or disabled");
                        results.AppendLine("    - Password contains special characters not properly encoded in Web.config");
                    }
                }
                else
                {
                    results.AppendLine(string.Format("? FAILED: Connection Error - {0}", ex.Message));
                    results.AppendLine(string.Format("  WebException Status: {0}", ex.Status));
                    if (ex.InnerException != null)
                    {
                        results.AppendLine(string.Format("  Inner Exception: {0}", ex.InnerException.Message));
                    }
                    
                    if (ex.Status == WebExceptionStatus.TrustFailure)
                    {
                        results.AppendLine("  ? SSL Certificate validation failed");
                        results.AppendLine("    - Server may be using a self-signed certificate");
                        results.AppendLine("    - Set Ftp_AcceptAllCertificates=true in Web.config for internal servers");
                    }
                    else
                    {
                        results.AppendLine("  Possible causes:");
                        results.AppendLine("    - FTP server is not running on " + _host);
                        results.AppendLine("    - Network connectivity issue from IIS app pool");
                        results.AppendLine("    - Firewall blocking connection");
                        results.AppendLine("    - IIS app pool identity lacks network permissions");
                    }
                }
                
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = results.ToString()
                });
            }
            catch (Exception ex)
            {
                results.AppendLine(string.Format("? FAILED: Unexpected Error - {0}", ex.Message));
                results.AppendLine(string.Format("  Exception Type: {0}", ex.GetType().Name));
                if (ex.InnerException != null)
                {
                    results.AppendLine(string.Format("  Inner Exception: {0}", ex.InnerException.Message));
                }
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = results.ToString()
                });
            }

            results.AppendLine();

            // Test 2: Check if base directory exists
            results.AppendLine("Test 2: Checking if base directory exists...");
            bool baseExists = CheckDirectoryExists(_basePath, results);
            if (baseExists)
            {
                results.AppendLine(string.Format("? SUCCESS: Base directory {0} exists", _basePath));
            }
            else
            {
                results.AppendLine(string.Format("? WARNING: Base directory {0} does not exist", _basePath));
                
                // Test 3: Try to create base directory
                results.AppendLine();
                results.AppendLine("Test 3: Attempting to create base directory...");
                try
                {
                    CreateDirectory(_basePath);
                    results.AppendLine(string.Format("? SUCCESS: Created base directory {0}", _basePath));
                }
                catch (Exception ex)
                {
                    results.AppendLine(string.Format("? FAILED: Could not create base directory - {0}", ex.Message));
                    results.AppendLine("  ACTION REQUIRED:");
                    results.AppendLine(string.Format("  1. Manually create directory {0} on FTP server {1}", _basePath, _host));
                    results.AppendLine(string.Format("  2. Grant write permissions to FTP user '{0}'", _user));
                    
                    return Ok(new ApiResponse
                    {
                        Success = false,
                        Message = results.ToString()
                    });
                }
            }

            results.AppendLine();

            // Test 4: Try to create a test subdirectory
            var testDir = _basePath + "/TEST_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            results.AppendLine("Test 4: Testing write permissions (creating test directory)...");
            try
            {
                CreateDirectory(testDir);
                results.AppendLine(string.Format("? SUCCESS: Created test directory {0}", testDir));
                
                // Test 5: Clean up test directory
                results.AppendLine();
                results.AppendLine("Test 5: Cleaning up test directory...");
                try
                {
                    DeleteDirectory(testDir);
                    results.AppendLine(string.Format("? SUCCESS: Deleted test directory {0}", testDir));
                }
                catch (Exception ex)
                {
                    results.AppendLine(string.Format("? WARNING: Could not delete test directory - {0}", ex.Message));
                    results.AppendLine(string.Format("  Please manually delete {0} from FTP server", testDir));
                }
            }
            catch (Exception ex)
            {
                results.AppendLine(string.Format("? FAILED: Write permission test failed - {0}", ex.Message));
                results.AppendLine("  ACTION REQUIRED:");
                results.AppendLine(string.Format("  Grant write/modify permissions to FTP user '{0}' on directory {1}", _user, _basePath));
                
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = results.ToString()
                });
            }

            results.AppendLine();
            results.AppendLine("=== All Tests Passed! ===");
            results.AppendLine("FTP server is properly configured and ready to use.");

            return Ok(new ApiResponse
            {
                Success = true,
                Message = results.ToString()
            });
        }

        private bool CheckDirectoryExists(string path, StringBuilder log)
        {
            try
            {
                var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(_user, _password);
                request.Timeout = 5000;
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (WebException ex)
            {
                var resp = ex.Response as FtpWebResponse;
                if (resp != null)
                {
                    log.AppendLine(string.Format("  FTP Status: {0} ({1}) - {2}", 
                        (int)resp.StatusCode, resp.StatusCode, resp.StatusDescription));
                    
                    if (resp.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        return false; // 550 - Directory doesn't exist
                    }
                }
                else
                {
                    log.AppendLine(string.Format("  Connection error: {0}, Status: {1}", ex.Message, ex.Status));
                }
                throw; // Re-throw for connection errors
            }
        }

        private void CreateDirectory(string path)
        {
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(_user, _password);
            request.Timeout = 10000;
            request.UsePassive = true;

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                // Success
            }
        }

        private void DeleteDirectory(string path)
        {
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.RemoveDirectory;
            request.Credentials = new NetworkCredential(_user, _password);
            request.Timeout = 10000;
            request.UsePassive = true;

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                // Success
            }
        }

        /// <summary>
        /// Get FTP log file information
        /// GET /api/ftp-diagnostic/log-info
        /// </summary>
        [HttpGet]
        [Route("log-info")]
        public IHttpActionResult GetLogInfo()
        {
            try
            {
                FtpLogger.Initialize();
                
                var logFilePath = FtpLogger.GetLogFilePath();
                var logFileExists = File.Exists(logFilePath);
                var logFileSize = logFileExists ? new FileInfo(logFilePath).Length : 0;
                
                var info = new
                {
                    LogFilePath = logFilePath,
                    LogDirectory = Path.GetDirectoryName(logFilePath),
                    LogFileName = Path.GetFileName(logFilePath),
                    LogFileExists = logFileExists,
                    LogFileSizeBytes = logFileSize,
                    LogFileSizeKB = logFileSize / 1024.0,
                    IsInitialized = FtpLogger.IsInitialized,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "FTP Log file information",
                    Data = info
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Error getting log info: " + ex.Message,
                    Error = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Get recent FTP log entries
        /// GET /api/ftp-diagnostic/recent-logs?lines=50
        /// </summary>
        [HttpGet]
        [Route("recent-logs")]
        public IHttpActionResult GetRecentLogs(int lines = 50)
        {
            try
            {
                FtpLogger.Initialize();
                
                var logFilePath = FtpLogger.GetLogFilePath();
                
                if (!File.Exists(logFilePath))
                {
                    return Ok(new ApiResponse
                    {
                        Success = false,
                        Message = "Log file does not exist yet: " + logFilePath
                    });
                }

                // Read last N lines from log file
                var allLines = File.ReadAllLines(logFilePath);
                var recentLines = allLines.Length <= lines 
                    ? allLines 
                    : allLines.Skip(allLines.Length - lines).ToArray();

                var logContent = string.Join("\n", recentLines);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = string.Format("Last {0} lines from FTP log", recentLines.Length),
                    Data = new
                    {
                        LogFilePath = logFilePath,
                        TotalLines = allLines.Length,
                        DisplayedLines = recentLines.Length,
                        LogContent = logContent
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Error reading log file: " + ex.Message,
                    Error = ex.ToString()
                });
            }
        }
    }
}
