using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using SmkcApi.Models.DepositManager;

namespace SmkcApi.Services.DepositManager
{
    /// <summary>
    /// Network-based storage service for uploading consent documents to a network share.
    /// Uses UNC path (\\server\share) for direct file system access.
    /// Supports credential-based authentication for accessing network shares.
    /// </summary>
    public class NetworkStorageService : IFtpStorageService
    {
        private readonly string _networkPath;
        private readonly string _basePath;
        private readonly string _username;
        private readonly string _password;
        private readonly string _server;

        public NetworkStorageService()
        {
            // Check if local path is configured (API running on same server as storage)
            var localPath = System.Configuration.ConfigurationManager.AppSettings["Network_LocalPath"];
            
            if (!string.IsNullOrEmpty(localPath))
            {
                // Use local file system path directly
                _networkPath = localPath;
                FtpLogger.Initialize();
                FtpLogger.LogInfo("=== Network Storage Service Initialized (Local Path) ===");
                FtpLogger.LogInfo("Local Path: " + _networkPath);
            }
            else
            {
                // Use network share (UNC path)
                _server = System.Configuration.ConfigurationManager.AppSettings["Network_Server"] ?? "192.168.40.47";
                var networkShare = System.Configuration.ConfigurationManager.AppSettings["Network_Share"] ?? "BankConsents";
                _basePath = System.Configuration.ConfigurationManager.AppSettings["Network_BasePath"] ?? "BankConsents";

                // Get credentials from FTP settings (reuse same credentials)
                _username = System.Configuration.ConfigurationManager.AppSettings["Ftp_User"] ?? string.Empty;
                _password = System.Configuration.ConfigurationManager.AppSettings["Ftp_Password"] ?? string.Empty;

                // Build UNC path: Handle both regular shares and admin shares with subdirectories
                // Examples:
                //   networkShare = "BankConsents" -> \\192.168.40.47\BankConsents
                //   networkShare = "c$\inetpub\ftproot\BankConsents" -> \\192.168.40.47\c$\inetpub\ftproot\BankConsents
                // Fix: Separate the share name from the subdirectory path
                string shareName;
                string subPath = string.Empty;
                
                // Check if networkShare contains a backslash (indicating subdirectory)
                if (networkShare.Contains("\\"))
                {
                    var parts = networkShare.Split(new[] { '\\' }, 2);
                    shareName = parts[0]; // e.g., "c$"
                    subPath = parts.Length > 1 ? parts[1] : string.Empty; // e.g., "inetpub\ftproot\BankConsents"
                }
                else
                {
                    shareName = networkShare; // e.g., "BankConsents"
                }

                // Build UNC path properly
                _networkPath = string.Format(@"\\{0}\{1}", _server, shareName);
                
                // Append subdirectory path if exists
                if (!string.IsNullOrEmpty(subPath))
                {
                    _networkPath = Path.Combine(_networkPath, subPath);
                }

                FtpLogger.Initialize();
                FtpLogger.LogInfo("=== Network Storage Service Initialized (Network Share) ===");
                FtpLogger.LogInfo("Server: " + _server);
                FtpLogger.LogInfo("Share Name: " + shareName);
                FtpLogger.LogInfo("Sub Path: " + (string.IsNullOrEmpty(subPath) ? "(none)" : subPath));
                FtpLogger.LogInfo("Final Network Path: " + _networkPath);
                FtpLogger.LogInfo(string.Format("Using credentials: {0}\\{1}", _server, _username));
            }
        }

        public string UploadConsentDocument(string requirementId, string bankId, ConsentDocumentDto document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (string.IsNullOrWhiteSpace(document.FileName)) throw new ArgumentException("FileName is required", "document");
            if (string.IsNullOrWhiteSpace(document.FileData)) throw new ArgumentException("FileData is required", "document");

            FtpLogger.LogInfo("============================================================");
            FtpLogger.LogInfo(string.Format("Network Upload: REQ={0}, Bank={1}, File={2}", requirementId, bankId, document.FileName));
            FtpLogger.LogInfo(string.Format("Network Path: {0}", _networkPath));
            FtpLogger.LogInfo("============================================================");

            // Decode base64 file data
            string base64Data = document.FileData;
            if (base64Data.Contains(","))
            {
                var commaIndex = base64Data.IndexOf(',');
                if (base64Data.Substring(0, commaIndex).Contains("base64"))
                {
                    base64Data = base64Data.Substring(commaIndex + 1);
                }
            }

            byte[] fileBytes;
            try
            {
                fileBytes = Convert.FromBase64String(base64Data);
                FtpLogger.LogInfo(string.Format("File decoded: {0} bytes", fileBytes.Length));
            }
            catch (Exception ex)
            {
                FtpLogger.LogError("Failed to decode file data", ex);
                throw new InvalidOperationException("Invalid base64 data for consent document", ex);
            }

            var safeFileName = Path.GetFileName(document.FileName);
            
            // Build directory structure
            var requirementDir = Path.Combine(_networkPath, requirementId);
            var bankDir = Path.Combine(requirementDir, bankId);
            var filePath = Path.Combine(bankDir, safeFileName);

            FtpLogger.LogInfo("");
            FtpLogger.LogInfo("Target Paths:");
            FtpLogger.LogInfo("  Network Share:  " + _networkPath);
            FtpLogger.LogInfo("  Requirement:    " + requirementDir);
            FtpLogger.LogInfo("  Bank:           " + bankDir);
            FtpLogger.LogInfo("  File:           " + filePath);
            FtpLogger.LogInfo("");

            NetworkCredential credential = null;

            try
            {
                // Connect with credentials if provided
                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                {
                    FtpLogger.LogInfo("Connecting to network share with credentials...");
                    credential = new NetworkCredential(_server, _username, _password);
                    var connected = credential.Connect();
                    
                    if (connected)
                    {
                        FtpLogger.LogInfo("? Connected to network share successfully");
                    }
                    else
                    {
                        FtpLogger.LogWarning("Could not establish credential connection, attempting without credentials...");
                    }
                }

                // Step 1: Verify network path is accessible
                FtpLogger.LogInfo("Step 1: Verifying network path access...");
                if (!Directory.Exists(_networkPath))
                {
                    var errorMsg = string.Format("Network path not accessible: {0}. Please ensure the share exists and is accessible.", _networkPath);
                    FtpLogger.LogError(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }
                FtpLogger.LogInfo("? Network path is accessible");

                // Step 2: Create requirement directory
                FtpLogger.LogInfo("Step 2: Creating requirement directory...");
                if (!Directory.Exists(requirementDir))
                {
                    Directory.CreateDirectory(requirementDir);
                    FtpLogger.LogInfo(string.Format("? Created: {0}", requirementDir));
                }
                else
                {
                    FtpLogger.LogInfo(string.Format("? Already exists: {0}", requirementDir));
                }

                // Step 3: Create bank directory
                FtpLogger.LogInfo("Step 3: Creating bank directory...");
                if (!Directory.Exists(bankDir))
                {
                    Directory.CreateDirectory(bankDir);
                    FtpLogger.LogInfo(string.Format("? Created: {0}", bankDir));
                }
                else
                {
                    FtpLogger.LogInfo(string.Format("? Already exists: {0}", bankDir));
                }

                // Step 4: Write file
                FtpLogger.LogInfo("Step 4: Writing file...");
                if (File.Exists(filePath))
                {
                    FtpLogger.LogInfo(string.Format("File exists, will overwrite: {0}", safeFileName));
                }

                File.WriteAllBytes(filePath, fileBytes);
                FtpLogger.LogInfo(string.Format("? File written successfully: {0} bytes", fileBytes.Length));

                // Step 5: Verify file was written
                FtpLogger.LogInfo("Step 5: Verifying file...");
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    FtpLogger.LogInfo(string.Format("? File verified: {0} bytes, Created: {1}", 
                        fileInfo.Length, fileInfo.CreationTime));
                }
                else
                {
                    FtpLogger.LogError("? File verification failed - file not found after write!");
                    throw new InvalidOperationException("File was not created successfully");
                }

                FtpLogger.LogInfo("============================================================");
                FtpLogger.LogInfo("Upload completed successfully!");
                FtpLogger.LogInfo("============================================================");
                FtpLogger.LogInfo("");

                return safeFileName;
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMsg = string.Format("Access denied to network path. Please check permissions for user '{0}' on {1}", 
                    _username ?? "current user", _networkPath);
                FtpLogger.LogError(errorMsg, ex);
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (IOException ex)
            {
                var errorMsg = string.Format("I/O error accessing network path: {0}", ex.Message);
                FtpLogger.LogError(errorMsg, ex);
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                FtpLogger.LogError("Upload failed", ex);
                throw;
            }
            finally
            {
                // Disconnect credential if it was connected
                if (credential != null)
                {
                    try
                    {
                        credential.Disconnect();
                        FtpLogger.LogInfo("Disconnected from network share");
                    }
                    catch { }
                }
            }
        }

        public string DownloadConsentDocument(string consentFileName, string requirementId, string bankId)
        {
            if (string.IsNullOrWhiteSpace(consentFileName))
            {
                FtpLogger.LogWarning("DownloadConsentDocument called with empty file name");
                return null;
            }

            FtpLogger.LogInfo("============================================================");
            FtpLogger.LogInfo("DOWNLOAD CONSENT DOCUMENT - Starting");
            FtpLogger.LogInfo("============================================================");
            FtpLogger.LogInfo(string.Format("Request Parameters:"));
            FtpLogger.LogInfo(string.Format("  - Requirement ID: {0}", requirementId ?? "(null)"));
            FtpLogger.LogInfo(string.Format("  - Bank ID: {0}", bankId ?? "(null)"));
            FtpLogger.LogInfo(string.Format("  - File Name: {0}", consentFileName ?? "(null)"));
            FtpLogger.LogInfo("");
            FtpLogger.LogInfo(string.Format("Server: {0}", _server));
            FtpLogger.LogInfo(string.Format("Username: {0}", _username));
            FtpLogger.LogInfo(string.Format("Network Path: {0}", _networkPath));
            FtpLogger.LogInfo("");

            // Build file paths
            var requirementDir = Path.Combine(_networkPath, requirementId);
            var bankDir = Path.Combine(requirementDir, bankId);
            var filePath = Path.Combine(bankDir, consentFileName);

            FtpLogger.LogInfo("Constructed File Paths:");
            FtpLogger.LogInfo(string.Format("  - Network Base: {0}", _networkPath));
            FtpLogger.LogInfo(string.Format("  - Requirement Dir: {0}", requirementDir));
            FtpLogger.LogInfo(string.Format("  - Bank Dir: {0}", bankDir));
            FtpLogger.LogInfo(string.Format("  - Full File Path: {0}", filePath));
            FtpLogger.LogInfo("");

            NetworkCredential credential = null;

            try
            {
                // Connect with credentials if provided
                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                {
                    FtpLogger.LogInfo("Connecting to network share with credentials...");
                    credential = new NetworkCredential(_server, _username, _password);
                    var connected = credential.Connect();
                    
                    if (connected)
                    {
                        FtpLogger.LogInfo("? Connected to network share successfully");
                    }
                    else
                    {
                        FtpLogger.LogWarning("Could not establish credential connection, attempting without credentials...");
                    }
                }
                else
                {
                    FtpLogger.LogInfo("No credentials configured - attempting access with current user context");
                }

                FtpLogger.LogInfo("");
                FtpLogger.LogInfo("Checking file existence...");
                FtpLogger.LogInfo(string.Format("Looking for file at: {0}", filePath));

                if (!File.Exists(filePath))
                {
                    FtpLogger.LogError(string.Format("? File NOT FOUND at path: {0}", filePath));
                    FtpLogger.LogInfo("");
                    FtpLogger.LogInfo("Troubleshooting Information:");
                    
                    // Check if network path exists
                    try
                    {
                        if (Directory.Exists(_networkPath))
                        {
                            FtpLogger.LogInfo(string.Format("  ? Network base path EXISTS: {0}", _networkPath));
                        }
                        else
                        {
                            FtpLogger.LogError(string.Format("  ? Network base path DOES NOT EXIST: {0}", _networkPath));
                        }
                    }
                    catch (Exception ex)
                    {
                        FtpLogger.LogError(string.Format("  ? Cannot access network base path: {0}", ex.Message));
                    }

                    // Check if requirement directory exists
                    try
                    {
                        if (Directory.Exists(requirementDir))
                        {
                            FtpLogger.LogInfo(string.Format("  ? Requirement directory EXISTS: {0}", requirementDir));
                        }
                        else
                        {
                            FtpLogger.LogError(string.Format("  ? Requirement directory DOES NOT EXIST: {0}", requirementDir));
                        }
                    }
                    catch (Exception ex)
                    {
                        FtpLogger.LogError(string.Format("  ? Cannot access requirement directory: {0}", ex.Message));
                    }

                    // Check if bank directory exists
                    try
                    {
                        if (Directory.Exists(bankDir))
                        {
                            FtpLogger.LogInfo(string.Format("  ? Bank directory EXISTS: {0}", bankDir));
                            
                            // List files in bank directory
                            try
                            {
                                var files = Directory.GetFiles(bankDir);
                                if (files.Length > 0)
                                {
                                    FtpLogger.LogInfo(string.Format("  Files in bank directory ({0} files):", files.Length));
                                    foreach (var file in files)
                                    {
                                        FtpLogger.LogInfo(string.Format("    - {0}", Path.GetFileName(file)));
                                    }
                                }
                                else
                                {
                                    FtpLogger.LogWarning("  Bank directory is EMPTY (no files found)");
                                }
                            }
                            catch (Exception ex)
                            {
                                FtpLogger.LogError(string.Format("  Cannot list files in bank directory: {0}", ex.Message));
                            }
                        }
                        else
                        {
                            FtpLogger.LogError(string.Format("  ? Bank directory DOES NOT EXIST: {0}", bankDir));
                        }
                    }
                    catch (Exception ex)
                    {
                        FtpLogger.LogError(string.Format("  ? Cannot access bank directory: {0}", ex.Message));
                    }

                    FtpLogger.LogInfo("============================================================");
                    return null;
                }

                FtpLogger.LogInfo(string.Format("? File EXISTS at: {0}", filePath));
                FtpLogger.LogInfo("");
                FtpLogger.LogInfo("Reading file contents...");

                var fileBytes = File.ReadAllBytes(filePath);
                FtpLogger.LogInfo(string.Format("? File read successfully: {0} bytes", fileBytes.Length));

                var base64Content = Convert.ToBase64String(fileBytes);
                FtpLogger.LogInfo(string.Format("? File converted to base64: {0} characters", base64Content.Length));

                FtpLogger.LogInfo("============================================================");
                FtpLogger.LogInfo("DOWNLOAD COMPLETED SUCCESSFULLY");
                FtpLogger.LogInfo("============================================================");
                FtpLogger.LogInfo("");

                return base64Content;
            }
            catch (UnauthorizedAccessException ex)
            {
                FtpLogger.LogError("============================================================");
                FtpLogger.LogError("ACCESS DENIED ERROR");
                FtpLogger.LogError("============================================================");
                FtpLogger.LogError(string.Format("File Path: {0}", filePath));
                FtpLogger.LogError(string.Format("Error: {0}", ex.Message));
                FtpLogger.LogError(string.Format("Possible Causes:"));
                FtpLogger.LogError("  1. Insufficient NTFS permissions on the file/folder");
                FtpLogger.LogError("  2. Network share permissions deny access");
                FtpLogger.LogError("  3. Administrative share (C$) blocked by UAC");
                FtpLogger.LogError("  4. Credentials in Web.config are incorrect");
                FtpLogger.LogError(string.Format("Current User Context: {0}", Environment.UserName));
                FtpLogger.LogError("============================================================");
                throw;
            }
            catch (IOException ex)
            {
                FtpLogger.LogError("============================================================");
                FtpLogger.LogError("I/O ERROR");
                FtpLogger.LogError("============================================================");
                FtpLogger.LogError(string.Format("File Path: {0}", filePath));
                FtpLogger.LogError(string.Format("Error: {0}", ex.Message));
                FtpLogger.LogError(string.Format("Possible Causes:"));
                FtpLogger.LogError("  1. Network connection interrupted");
                FtpLogger.LogError("  2. File is locked by another process");
                FtpLogger.LogError("  3. Disk I/O error on remote server");
                FtpLogger.LogError("============================================================");
                throw;
            }
            catch (Exception ex)
            {
                FtpLogger.LogError("============================================================");
                FtpLogger.LogError("UNEXPECTED ERROR");
                FtpLogger.LogError("============================================================");
                FtpLogger.LogError(string.Format("File Path: {0}", filePath));
                FtpLogger.LogError(string.Format("Error Type: {0}", ex.GetType().Name));
                FtpLogger.LogError(string.Format("Error Message: {0}", ex.Message));
                FtpLogger.LogError(string.Format("Stack Trace: {0}", ex.StackTrace));
                FtpLogger.LogError("============================================================");
                throw;
            }
            finally
            {
                if (credential != null)
                {
                    try
                    {
                        credential.Disconnect();
                        FtpLogger.LogInfo("Disconnected from network share");
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Helper class to manage network credentials for accessing shares
        /// </summary>
        private class NetworkCredential
        {
            private readonly string _server;
            private readonly string _username;
            private readonly string _password;
            private bool _isConnected;

            public NetworkCredential(string server, string username, string password)
            {
                _server = server;
                _username = username;
                _password = password;
                _isConnected = false;
            }

            public bool Connect()
            {
                try
                {
                    // Use WNetUseConnection for explicit credential authentication
                    var netResource = new NetResource
                    {
                        Scope = ResourceScope.GlobalNetwork,
                        ResourceType = ResourceType.Disk,
                        DisplayType = ResourceDisplayType.Share,
                        RemoteName = string.Format(@"\\{0}\IPC$", _server)
                    };

                    var userName = string.Format(@"{0}\{1}", _server, _username);
                    
                    int result = WNetUseConnection(
                        IntPtr.Zero,
                        netResource,
                        _password,
                        userName,
                        0,
                        null,
                        null,
                        null);

                    if (result == 0)
                    {
                        _isConnected = true;
                        return true;
                    }
                    else
                    {
                        FtpLogger.LogWarning(string.Format("WNetUseConnection failed with error code: {0}", result));
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    FtpLogger.LogWarning(string.Format("Failed to connect with credentials: {0}", ex.Message));
                    return false;
                }
            }

            public void Disconnect()
            {
                if (_isConnected)
                {
                    try
                    {
                        WNetCancelConnection2(string.Format(@"\\{0}\IPC$", _server), 0, true);
                        _isConnected = false;
                    }
                    catch { }
                }
            }

            [DllImport("mpr.dll")]
            private static extern int WNetUseConnection(
                IntPtr hwndOwner,
                NetResource lpNetResource,
                string lpPassword,
                string lpUserID,
                int dwFlags,
                string lpAccessName,
                string lpBufferSize,
                string lpResult);

            [DllImport("mpr.dll")]
            private static extern int WNetCancelConnection2(
                string lpName,
                int dwFlags,
                bool fForce);

            [StructLayout(LayoutKind.Sequential)]
            private class NetResource
            {
                public ResourceScope Scope;
                public ResourceType ResourceType;
                public ResourceDisplayType DisplayType;
                public int Usage;
                public string LocalName;
                public string RemoteName;
                public string Comment;
                public string Provider;
            }

            private enum ResourceScope
            {
                Connected = 1,
                GlobalNetwork,
                Remembered,
                Recent,
                Context
            }

            private enum ResourceType
            {
                Any = 0,
                Disk = 1,
                Print = 2,
                Reserved = 8,
            }

            private enum ResourceDisplayType
            {
                Generic = 0x0,
                Domain = 0x01,
                Server = 0x02,
                Share = 0x03,
                File = 0x04,
                Group = 0x05,
                Network = 0x06,
                Root = 0x07,
                Shareadmin = 0x08,
                Directory = 0x09,
                Tree = 0x0a,
                Ndscontainer = 0x0b
            }
        }
    }
}
