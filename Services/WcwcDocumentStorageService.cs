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
    }

    public class WcwcDocumentStorageService : IWcwcDocumentStorageService
    {
        private readonly string _storageType;
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _ftpBasePath;
        private readonly string _networkRootPath;

        public WcwcDocumentStorageService()
        {
            _storageType = System.Configuration.ConfigurationManager.AppSettings["Storage_Type"] ?? "ftp";
            _host = System.Configuration.ConfigurationManager.AppSettings["Ftp_Host"] ?? "192.168.40.47";
            _port = ParseInt(System.Configuration.ConfigurationManager.AppSettings["Ftp_Port"], 21);
            _user = System.Configuration.ConfigurationManager.AppSettings["Ftp_User"] ?? string.Empty;
            _password = System.Configuration.ConfigurationManager.AppSettings["Ftp_Password"] ?? string.Empty;
            _ftpBasePath = System.Configuration.ConfigurationManager.AppSettings["Wcwc_Ftp_BasePath"] ?? "/DisabilityRegistrations";
            _networkRootPath = ResolveNetworkRootPath();

            FtpLogger.Initialize();
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

            var safeFileName = GenerateStoredFileName(Path.GetFileName(document.FileName));
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

        private bool IsNetworkStorage()
        {
            return _storageType.Equals("network", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private string ResolveNetworkRootPath()
        {
            var localOverride = System.Configuration.ConfigurationManager.AppSettings["Wcwc_Network_LocalPath"];
            if (!string.IsNullOrWhiteSpace(localOverride))
            {
                return localOverride;
            }

            var configuredShare = System.Configuration.ConfigurationManager.AppSettings["Wcwc_Network_Share"];
            if (string.IsNullOrWhiteSpace(configuredShare))
            {
                configuredShare = "c$\\inetpub\\ftproot\\DisabilityRegistrations";
            }

            var server = System.Configuration.ConfigurationManager.AppSettings["Network_Server"] ?? "192.168.40.47";
            return BuildUncPath(server, configuredShare);
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

        private static string GenerateStoredFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            return string.Format("{0}_{1}{2}", Guid.NewGuid().ToString("N"), baseName, extension);
        }

        private void UploadToFtp(string registrationNumber, string documentCode, string fileName, byte[] fileBytes)
        {
            var basePath = _ftpBasePath.TrimEnd('/');
            var registrationPath = basePath + "/" + registrationNumber;
            var documentPath = registrationPath + "/" + documentCode;
            var filePath = documentPath + "/" + fileName;

            CreateFtpDirectory(registrationPath);
            CreateFtpDirectory(documentPath);
            UploadFtpFile(filePath, fileBytes);
        }

        private string DownloadFromFtp(string fileName, string registrationNumber, string documentCode)
        {
            var ftpPath = string.Format("{0}/{1}/{2}/{3}", _ftpBasePath.TrimEnd('/'), registrationNumber, documentCode, fileName);
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
            var registrationDir = Path.Combine(_networkRootPath, registrationNumber);
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
            var filePath = Path.Combine(_networkRootPath, registrationNumber, documentCode, fileName);
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