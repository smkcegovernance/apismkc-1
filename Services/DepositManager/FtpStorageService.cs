using System;
using System.IO;
using System.Net;
using System.Text;
using SmkcApi.Models.DepositManager;

namespace SmkcApi.Services.DepositManager
{
    /// <summary>
    /// Simple FTP storage service for uploading consent documents to an internal LAN FTP server.
    /// This uses classic FTP over LAN; the FTP server is not exposed to the internet.
    /// </summary>
    public interface IFtpStorageService
    {
        /// <summary>
        /// Upload a consent document and return the stored path/key.
        /// </summary>
        /// <param name="requirementId">Requirement identifier (e.g., REQ0000000002)</param>
        /// <param name="bankId">Bank identifier</param>
        /// <param name="document">Consent document DTO (file name, data, size)</param>
        /// <returns>Relative storage path on FTP (for logging/DB reference)</returns>
        string UploadConsentDocument(string requirementId, string bankId, ConsentDocumentDto document);

        /// <summary>
        /// Download a consent document from FTP by file name.
        /// </summary>
        /// <param name="consentFileName">The consent file name returned from DB (e.g., "{guid}_{originalName}.pdf")</param>
        /// <param name="requirementId">Requirement identifier</param>
        /// <param name="bankId">Bank identifier</param>
        /// <returns>Base64-encoded file content, or null if not found</returns>
        string DownloadConsentDocument(string consentFileName, string requirementId, string bankId);
    }

    public class FtpStorageService : IFtpStorageService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _basePath;

        public FtpStorageService()
        {
            _host = System.Configuration.ConfigurationManager.AppSettings["Ftp_Host"] ?? "192.168.40.47";
            var portSetting = System.Configuration.ConfigurationManager.AppSettings["Ftp_Port"];
            _port = !string.IsNullOrEmpty(portSetting) ? int.Parse(portSetting) : 21;
            _user = System.Configuration.ConfigurationManager.AppSettings["Ftp_User"] ?? string.Empty;
            _password = System.Configuration.ConfigurationManager.AppSettings["Ftp_Password"] ?? string.Empty;
            _basePath = System.Configuration.ConfigurationManager.AppSettings["Ftp_BasePath"] ?? "/BankConsents";
            
            // Initialize FTP logger
            FtpLogger.Initialize();
        }

        public string UploadConsentDocument(string requirementId, string bankId, ConsentDocumentDto document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (string.IsNullOrWhiteSpace(document.FileName)) throw new ArgumentException("FileName is required", "document");
            if (string.IsNullOrWhiteSpace(document.FileData)) throw new ArgumentException("FileData is required", "document");

            FtpLogger.LogInfo("============================================================");
            FtpLogger.LogInfo(string.Format("FTP Upload: REQ={0}, Bank={1}, File={2}", requirementId, bankId, document.FileName));
            FtpLogger.LogInfo(string.Format("FTP Server: ftp://{0}:{1}", _host, _port));
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
            var storedFileName = GenerateStoredFileName(safeFileName);
            
            // Build paths - directly working with base path
            var basePath = _basePath.TrimEnd('/');
            var requirementPath = basePath + "/" + requirementId;
            var bankPath = requirementPath + "/" + bankId;
            var filePath = bankPath + "/" + storedFileName;

            FtpLogger.LogInfo("");
            FtpLogger.LogInfo("Target Paths:");
            FtpLogger.LogInfo("  Base:        " + basePath);
            FtpLogger.LogInfo("  Requirement: " + requirementPath);
            FtpLogger.LogInfo("  Bank:        " + bankPath);
            FtpLogger.LogInfo("  File:        " + filePath);
            FtpLogger.LogInfo("");

            try
            {
                // Create requirement folder directly
                FtpLogger.LogInfo("Creating requirement folder: " + requirementPath);
                CreateFtpDirectory(requirementPath);
                FtpLogger.LogInfo("? Requirement folder ready");

                // Create bank folder directly
                FtpLogger.LogInfo("Creating bank folder: " + bankPath);
                CreateFtpDirectory(bankPath);
                FtpLogger.LogInfo("? Bank folder ready");

                // Upload file directly
                FtpLogger.LogInfo("Uploading file: " + storedFileName);
                UploadFtpFile(filePath, fileBytes);
                FtpLogger.LogInfo("? File uploaded successfully");

                FtpLogger.LogInfo("============================================================");
                FtpLogger.LogInfo("Upload completed successfully!");
                FtpLogger.LogInfo("============================================================");
                FtpLogger.LogInfo("");

                return storedFileName;
            }
            catch (Exception ex)
            {
                FtpLogger.LogError("Upload failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Create FTP directory - simple and direct
        /// </summary>
        private void CreateFtpDirectory(string path)
        {
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
            
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential(_user, _password);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 10000;

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    FtpLogger.LogInfo(string.Format("  Created: {0} - {1}", path, response.StatusDescription.Trim()));
                }
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                if (ftpResponse != null)
                {
                    // If directory already exists, that's fine
                    if (ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable &&
                        ftpResponse.StatusDescription != null &&
                        (ftpResponse.StatusDescription.Contains("exist") || 
                         ftpResponse.StatusDescription.Contains("Exist")))
                    {
                        FtpLogger.LogInfo(string.Format("  Already exists: {0}", path));
                        return;
                    }
                    
                    FtpLogger.LogError(string.Format("  Failed to create {0}: Status={1}, Message={2}", 
                        path, ftpResponse.StatusCode, ftpResponse.StatusDescription));
                    throw;
                }
                
                FtpLogger.LogError(string.Format("  Connection error for {0}: {1}", path, ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Upload file to FTP - simple and direct
        /// </summary>
        private void UploadFtpFile(string path, byte[] fileBytes)
        {
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
            
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_user, _password);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 30000;
                request.ReadWriteTimeout = 30000;

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileBytes, 0, fileBytes.Length);
                }

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    FtpLogger.LogInfo(string.Format("  Uploaded {0} bytes - {1}", fileBytes.Length, response.StatusDescription.Trim()));
                }
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                if (ftpResponse != null)
                {
                    // If file already exists, delete and retry
                    if (ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable &&
                        ftpResponse.StatusDescription != null &&
                        (ftpResponse.StatusDescription.Contains("exist") || 
                         ftpResponse.StatusDescription.Contains("Exist")))
                    {
                        FtpLogger.LogInfo(string.Format("  File exists, deleting and retrying: {0}", path));
                        
                        try
                        {
                            DeleteFtpFile(path);
                            FtpLogger.LogInfo("  Deleted existing file");
                            
                            // Retry upload
                            var retryRequest = (FtpWebRequest)WebRequest.Create(uri);
                            retryRequest.Method = WebRequestMethods.Ftp.UploadFile;
                            retryRequest.Credentials = new NetworkCredential(_user, _password);
                            retryRequest.UseBinary = true;
                            retryRequest.UsePassive = true;
                            retryRequest.KeepAlive = false;
                            retryRequest.Timeout = 30000;
                            retryRequest.ReadWriteTimeout = 30000;

                            using (var requestStream = retryRequest.GetRequestStream())
                            {
                                requestStream.Write(fileBytes, 0, fileBytes.Length);
                            }

                            using (var response = (FtpWebResponse)retryRequest.GetResponse())
                            {
                                FtpLogger.LogInfo(string.Format("  Uploaded {0} bytes (retry) - {1}", 
                                    fileBytes.Length, response.StatusDescription.Trim()));
                            }
                            return;
                        }
                        catch (Exception retryEx)
                        {
                            FtpLogger.LogError("  Failed to delete and retry upload", retryEx);
                            throw;
                        }
                    }
                    
                    FtpLogger.LogError(string.Format("  Failed to upload {0}: Status={1}, Message={2}", 
                        path, ftpResponse.StatusCode, ftpResponse.StatusDescription));
                    throw;
                }
                
                FtpLogger.LogError(string.Format("  Connection error uploading {0}: {1}", path, ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Delete file from FTP
        /// </summary>
        private void DeleteFtpFile(string path)
        {
            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, path));
            
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(_user, _password);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = 10000;

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                // File deleted
            }
        }

        private static string GenerateStoredFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            return string.Format("{0}_{1}{2}", Guid.NewGuid().ToString("N"), baseName, extension);
        }

        public string DownloadConsentDocument(string consentFileName, string requirementId, string bankId)
        {
            if (string.IsNullOrWhiteSpace(consentFileName))
            {
                FtpLogger.LogWarning("DownloadConsentDocument called with empty file name");
                return null;
            }

            // Build FTP path: /BankConsents/{requirementId}/{bankId}/{consentFileName}
            var basePath = _basePath.TrimEnd('/');
            var ftpPath = string.Format("{0}/{1}/{2}/{3}", basePath, requirementId, bankId, consentFileName);

            var uri = new Uri(string.Format("ftp://{0}:{1}{2}", _host, _port, ftpPath));

            FtpLogger.LogInfo(string.Format("Downloading file: {0}", ftpPath));

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(_user, _password);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 30000;
                request.ReadWriteTimeout = 30000;

                using (var response = (FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var memoryStream = new MemoryStream())
                {
                    responseStream.CopyTo(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    FtpLogger.LogInfo(string.Format("Downloaded {0} bytes", fileBytes.Length));

                    return Convert.ToBase64String(fileBytes);
                }
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                if (ftpResponse != null && ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    FtpLogger.LogWarning(string.Format("File not found: {0}", ftpPath));
                    return null;
                }

                FtpLogger.LogError(string.Format("Download failed: {0}", ftpPath), ex);
                throw;
            }
        }
    }
}
