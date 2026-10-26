using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using SmkcApi.Models.DepositManager;
using SmkcApi.Services.DepositManager;

namespace SmkcApi.Services
{
    public interface IWcwcDocumentStorageService
    {
        string UploadDocument(string registrationNumber, string documentCode, ConsentDocumentDto document);
        string DownloadDocument(string fileName, string registrationNumber, string documentCode);
        string ResolveDocumentFileName(string registrationNumber, string documentCode);
    }

    public class WcwcDocumentStorageService : IWcwcDocumentStorageService
    {
        private readonly string _storageType;
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _ftpBasePathDisability;
        private readonly string _ftpBasePathSingleWomen;
        private readonly string _networkRootPathDisability;
        private readonly string _networkRootPathSingleWomen;

        public WcwcDocumentStorageService()
        {
            _storageType = System.Configuration.ConfigurationManager.AppSettings["Wcwc_Storage_Type"]
                ?? System.Configuration.ConfigurationManager.AppSettings["Storage_Type"]
                ?? "ftp";
            _host = System.Configuration.ConfigurationManager.AppSettings["Ftp_Host"] ?? "192.168.40.47";
            _port = ParseInt(System.Configuration.ConfigurationManager.AppSettings["Ftp_Port"], 21);
            _user = System.Configuration.ConfigurationManager.AppSettings["Ftp_User"] ?? string.Empty;
            _password = System.Configuration.ConfigurationManager.AppSettings["Ftp_Password"] ?? string.Empty;
            _ftpBasePathDisability = (System.Configuration.ConfigurationManager.AppSettings["Wcwc_Disability_Ftp_BasePath"]
                ?? System.Configuration.ConfigurationManager.AppSettings["Wcwc_Ftp_BasePath"]
                ?? "/DisabilityRegistrations").TrimEnd('/');
            _ftpBasePathSingleWomen = (System.Configuration.ConfigurationManager.AppSettings["Wcwc_Swr_Ftp_BasePath"]
                ?? "/SingleWomenRegistrations").TrimEnd('/');
            _networkRootPathDisability = ResolveNetworkRootPathForConfig(
                "Wcwc_Disability_Network_LocalPath",
                "Wcwc_Disability_Network_Share",
                System.Configuration.ConfigurationManager.AppSettings["Wcwc_Network_LocalPath"],
                System.Configuration.ConfigurationManager.AppSettings["Wcwc_Network_Share"],
                "c$\\inetpub\\ftproot\\DisabilityRegistrations");
            _networkRootPathSingleWomen = ResolveNetworkRootPathForConfig(
                "Wcwc_Swr_Network_LocalPath",
                "Wcwc_Swr_Network_Share",
                null,
                null,
                "c$\\inetpub\\ftproot\\SingleWomenRegistrations");

            FtpLogger.Initialize();
            LogEffectiveConfiguration();
        }

        public string UploadDocument(string registrationNumber, string documentCode, ConsentDocumentDto document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            if (string.IsNullOrWhiteSpace(registrationNumber) || string.IsNullOrWhiteSpace(documentCode))
            {
                throw new ArgumentException("registrationNumber and documentCode are required");
            }

            var safeFileName = GenerateStoredFileName(documentCode, Path.GetFileName(document.FileName));
            var fileBytes = DecodeBase64(document.FileData);

            if (IsNetworkStorage())
            {
                UploadToNetwork(registrationNumber, documentCode, safeFileName, fileBytes);
            }
            else
            {
                UploadToFtp(registrationNumber, documentCode, safeFileName, fileBytes);
            }

            return safeFileName;
        }

        public string DownloadDocument(string fileName, string registrationNumber, string documentCode)
        {
            if (IsNetworkStorage())
            {
                return DownloadFromNetwork(fileName, registrationNumber, documentCode);
            }

            return DownloadFromFtp(fileName, registrationNumber, documentCode);
        }

        public string ResolveDocumentFileName(string registrationNumber, string documentCode)
        {
            if (string.IsNullOrWhiteSpace(registrationNumber) || string.IsNullOrWhiteSpace(documentCode))
            {
                return null;
            }

            return IsNetworkStorage()
                ? ResolveDocumentFileNameFromNetwork(registrationNumber, documentCode)
                : ResolveDocumentFileNameFromFtp(registrationNumber, documentCode);
        }

        private bool IsNetworkStorage()
        {
            return _storageType.Equals("network", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private string ResolveNetworkRootPathForConfig(
            string localPathKey,
            string shareKey,
            string legacyLocalOverride,
            string legacyShare,
            string defaultShare)
        {
            var localOverride = System.Configuration.ConfigurationManager.AppSettings[localPathKey];
            if (string.IsNullOrWhiteSpace(localOverride))
            {
                localOverride = legacyLocalOverride;
            }

            if (!string.IsNullOrWhiteSpace(localOverride))
            {
                return localOverride;
            }

            var configuredShare = System.Configuration.ConfigurationManager.AppSettings[shareKey];
            if (string.IsNullOrWhiteSpace(configuredShare))
            {
                configuredShare = legacyShare;
            }

            if (string.IsNullOrWhiteSpace(configuredShare))
            {
                configuredShare = defaultShare;
            }

            var server = System.Configuration.ConfigurationManager.AppSettings["Network_Server"] ?? "192.168.40.47";
            return BuildUncPath(server, configuredShare);
        }

        private static bool IsSingleWomenDocument(string documentCode)
        {
            return !string.IsNullOrWhiteSpace(documentCode)
                && documentCode.Trim().StartsWith("SWR_", StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveFtpBasePath(string documentCode)
        {
            return IsSingleWomenDocument(documentCode) ? _ftpBasePathSingleWomen : _ftpBasePathDisability;
        }

        private string ResolveNetworkRootPath(string documentCode)
        {
            return IsSingleWomenDocument(documentCode) ? _networkRootPathSingleWomen : _networkRootPathDisability;
        }

        private void LogEffectiveConfiguration()
        {
            FtpLogger.LogInfo("=== WCWC Document Storage Configuration ===");
            FtpLogger.LogInfo("Storage_Type: " + _storageType);
            FtpLogger.LogInfo(string.Format("FTP Server: ftp://{0}:{1}", _host, _port));
            FtpLogger.LogInfo("FTP Disability Base Path: " + _ftpBasePathDisability);
            FtpLogger.LogInfo("FTP Single Women Base Path: " + _ftpBasePathSingleWomen);
            FtpLogger.LogInfo("Network Disability Root Path: " + _networkRootPathDisability);
            FtpLogger.LogInfo("Network Single Women Root Path: " + _networkRootPathSingleWomen);
            FtpLogger.LogInfo("==========================================");
        }

        private static string BuildUncPath(string server, string shareValue)
        {
            string shareName;
            string subPath = string.Empty;

            if (shareValue.Contains("\\"))
            {
                var parts = shareValue.Split(new[] { '\\' }, 2);
                shareName = parts[0];
                subPath = parts.Length > 1 ? parts[1] : string.Empty;
            }
            else
            {
                shareName = shareValue;
            }

            var unc = string.Format(@"\\{0}\{1}", server, shareName);
            if (!string.IsNullOrWhiteSpace(subPath))
            {
                unc = Path.Combine(unc, subPath);
            }

            return unc;
        }

        private static byte[] DecodeBase64(string fileData)
        {
            var base64Data = fileData ?? string.Empty;
            if (base64Data.Contains(","))
            {
                var commaIndex = base64Data.IndexOf(',');
                if (base64Data.Substring(0, commaIndex).Contains("base64"))
                {
                    base64Data = base64Data.Substring(commaIndex + 1);
                }
            }

            return Convert.FromBase64String(base64Data);
        }

        private static string GenerateStoredFileName(string documentCode, string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);

            if (IsSingleWomenDocument(documentCode))
            {
                // Keep SWR names compact to avoid overflowing OTHER_INFO snapshot limits.
                return string.Format("{0}{1}", Guid.NewGuid().ToString("N").Substring(0, 12), extension);
            }

            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            return string.Format("{0}_{1}{2}", Guid.NewGuid().ToString("N"), baseName, extension);
        }

        private void UploadToFtp(string registrationNumber, string documentCode, string fileName, byte[] fileBytes)
        {
            var basePath = ResolveFtpBasePath(documentCode).TrimEnd('/');
            var registrationPath = basePath + "/" + registrationNumber;
            var documentPath = registrationPath + "/" + documentCode;
            var filePath = documentPath + "/" + fileName;

            CreateFtpDirectory(registrationPath);
            CreateFtpDirectory(documentPath);
            UploadFtpFile(filePath, fileBytes);
        }

        private string DownloadFromFtp(string fileName, string registrationNumber, string documentCode)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = ResolveDocumentFileNameFromFtp(registrationNumber, documentCode);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return null;
                }
            }

            var ftpPath = string.Format("{0}/{1}/{2}/{3}", ResolveFtpBasePath(documentCode).TrimEnd('/'), registrationNumber, documentCode, fileName);
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, ftpPath));

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new System.Net.NetworkCredential(_user, _password);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 30000;

                using (var response = (FtpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return Convert.ToBase64String(memory.ToArray());
                }
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                if (ftpResponse != null && ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return null;
                }

                throw;
            }
        }

        private void CreateFtpDirectory(string path)
        {
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new System.Net.NetworkCredential(_user, _password);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 10000;

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                }
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                if (ftpResponse != null && ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return;
                }

                throw;
            }
        }

        private void UploadFtpFile(string path, byte[] fileBytes)
        {
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new System.Net.NetworkCredential(_user, _password);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = 30000;

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileBytes, 0, fileBytes.Length);
            }

            using (var response = (FtpWebResponse)request.GetResponse())
            {
            }
        }

        private void UploadToNetwork(string registrationNumber, string documentCode, string fileName, byte[] fileBytes)
        {
            var rootPath = ResolveNetworkRootPath(documentCode);
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new InvalidOperationException("Network root path is not configured for document code: " + documentCode);
            }

            var registrationDir = Path.Combine(rootPath, registrationNumber);
            var documentDir = Path.Combine(registrationDir, documentCode);
            var filePath = Path.Combine(documentDir, fileName);

            NetworkCredential credential = null;

            try
            {
                if (!string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_password))
                {
                    credential = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["Network_Server"] ?? "192.168.40.47", _user, _password);
                    credential.Connect();
                }

                if (!Directory.Exists(documentDir))
                {
                    Directory.CreateDirectory(documentDir);
                }

                File.WriteAllBytes(filePath, fileBytes);
            }
            finally
            {
                if (credential != null)
                {
                    credential.Disconnect();
                }
            }
        }

        private string DownloadFromNetwork(string fileName, string registrationNumber, string documentCode)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = ResolveDocumentFileNameFromNetwork(registrationNumber, documentCode);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return null;
                }
            }

            var rootPath = ResolveNetworkRootPath(documentCode);
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return null;
            }

            var filePath = Path.Combine(rootPath, registrationNumber, documentCode, fileName);
            NetworkCredential credential = null;

            try
            {
                if (!string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_password))
                {
                    credential = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["Network_Server"] ?? "192.168.40.47", _user, _password);
                    credential.Connect();
                }

                if (!File.Exists(filePath))
                {
                    return null;
                }

                return Convert.ToBase64String(File.ReadAllBytes(filePath));
            }
            finally
            {
                if (credential != null)
                {
                    credential.Disconnect();
                }
            }
        }

        private string ResolveDocumentFileNameFromNetwork(string registrationNumber, string documentCode)
        {
            var rootPath = ResolveNetworkRootPath(documentCode);
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return null;
            }

            var documentDir = Path.Combine(rootPath, registrationNumber, documentCode);
            if (!Directory.Exists(documentDir))
            {
                return null;
            }

            var files = Directory.GetFiles(documentDir);
            if (files == null || files.Length == 0)
            {
                return null;
            }

            string bestPath = null;
            DateTime bestWrite = DateTime.MinValue;
            foreach (var path in files)
            {
                DateTime writeTime;
                try
                {
                    writeTime = File.GetLastWriteTimeUtc(path);
                }
                catch
                {
                    continue;
                }

                if (bestPath == null || writeTime > bestWrite)
                {
                    bestPath = path;
                    bestWrite = writeTime;
                }
            }

            return bestPath == null ? null : Path.GetFileName(bestPath);
        }

        private string ResolveDocumentFileNameFromFtp(string registrationNumber, string documentCode)
        {
            var folderPath = string.Format("{0}/{1}/{2}", ResolveFtpBasePath(documentCode).TrimEnd('/'), registrationNumber, documentCode);
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, folderPath));

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new System.Net.NetworkCredential(_user, _password);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 15000;

                using (var response = (FtpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        var candidate = line.Trim();
                        if (candidate == "." || candidate == "..")
                        {
                            continue;
                        }

                        return candidate;
                    }
                }
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                if (ftpResponse != null && ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return null;
                }

                throw;
            }

            return null;
        }

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
            }

            public bool Connect()
            {
                try
                {
                    var netResource = new NetResource
                    {
                        Scope = ResourceScope.GlobalNetwork,
                        ResourceType = ResourceType.Disk,
                        DisplayType = ResourceDisplayType.Share,
                        RemoteName = string.Format(@"\\{0}\IPC$", _server)
                    };

                    var userName = string.Format(@"{0}\{1}", _server, _username);
                    var result = WNetUseConnection(IntPtr.Zero, netResource, _password, userName, 0, null, null, null);
                    _isConnected = result == 0;
                    return _isConnected;
                }
                catch
                {
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
                    }
                    catch
                    {
                    }

                    _isConnected = false;
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
                Reserved = 8
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