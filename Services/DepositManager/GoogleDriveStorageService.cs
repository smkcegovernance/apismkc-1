using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SmkcApi.Models.DepositManager;

namespace SmkcApi.Services.DepositManager
{
    /// <summary>
    /// Google Drive storage implementation for .NET Framework 4.5 using direct REST API calls.
    /// This avoids framework constraints from newer Google SDK packages.
    /// </summary>
    public class GoogleDriveStorageService : IFtpStorageService
    {
        private const string DriveScope = "https://www.googleapis.com/auth/drive";
        private const string FolderMimeType = "application/vnd.google-apps.folder";
        private const string TokenEndpointFallback = "https://oauth2.googleapis.com/token";

        private readonly string _credentialsPath;
        private readonly string _applicationName;
        private readonly string _sharedFolderId;
        private readonly string _delegatedUserEmail;
        private readonly string _oauthClientId;
        private readonly string _oauthClientSecret;
        private readonly string _oauthRefreshToken;
        private readonly bool _useUserOAuth;
        private readonly ServiceAccountCredentialFile _credential;

        private static readonly object TokenLock = new object();
        private static string _accessToken;
        private static DateTime _accessTokenExpiresUtc = DateTime.MinValue;

        public GoogleDriveStorageService()
        {
            FtpLogger.Initialize();

            _credentialsPath = System.Configuration.ConfigurationManager.AppSettings["GoogleDrive_CredentialsPath"] ?? "GoogleDrive\\service-account-credentials.json";
            _applicationName = System.Configuration.ConfigurationManager.AppSettings["GoogleDrive_ApplicationName"] ?? "SMKC Deposit Manager API";
            _sharedFolderId = System.Configuration.ConfigurationManager.AppSettings["GoogleDrive_SharedFolderId"] ?? string.Empty;
            _delegatedUserEmail = System.Configuration.ConfigurationManager.AppSettings["GoogleDrive_DelegatedUserEmail"] ?? string.Empty;
            _oauthClientId = System.Configuration.ConfigurationManager.AppSettings["GoogleDrive_OAuthClientId"] ?? string.Empty;
            _oauthClientSecret = System.Configuration.ConfigurationManager.AppSettings["GoogleDrive_OAuthClientSecret"] ?? string.Empty;
            _oauthRefreshToken = System.Configuration.ConfigurationManager.AppSettings["GoogleDrive_OAuthRefreshToken"] ?? string.Empty;
            _useUserOAuth = !string.IsNullOrWhiteSpace(_oauthClientId) &&
                            !string.IsNullOrWhiteSpace(_oauthClientSecret) &&
                            !string.IsNullOrWhiteSpace(_oauthRefreshToken);

            if (!_useUserOAuth)
            {
                var resolvedCredentialsPath = ResolveCredentialsPath(_credentialsPath);
                if (!File.Exists(resolvedCredentialsPath))
                {
                    throw new FileNotFoundException("Google Drive credentials file not found", resolvedCredentialsPath);
                }

                var json = File.ReadAllText(resolvedCredentialsPath);
                _credential = JsonConvert.DeserializeObject<ServiceAccountCredentialFile>(json);

                ValidateCredential(_credential, resolvedCredentialsPath);

                FtpLogger.LogInfo("Credentials: " + resolvedCredentialsPath);
            }

            if (string.IsNullOrWhiteSpace(_sharedFolderId))
            {
                throw new InvalidOperationException("GoogleDrive_SharedFolderId appSetting is required for service-account uploads.");
            }

            FtpLogger.LogInfo("=== Google Drive Storage Service Initialized ===");
            FtpLogger.LogInfo("Application: " + _applicationName);
            FtpLogger.LogInfo("Shared Folder ID: " + _sharedFolderId);
            FtpLogger.LogInfo("Delegated User: " + (string.IsNullOrWhiteSpace(_delegatedUserEmail) ? "(none)" : _delegatedUserEmail));
            FtpLogger.LogInfo("Auth Mode: " + (_useUserOAuth ? "OAuth Refresh Token (User)" : "Service Account"));
        }

        public string UploadConsentDocument(string requirementId, string bankId, ConsentDocumentDto document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (string.IsNullOrWhiteSpace(requirementId)) throw new ArgumentException("requirementId is required", "requirementId");
            if (string.IsNullOrWhiteSpace(bankId)) throw new ArgumentException("bankId is required", "bankId");
            if (string.IsNullOrWhiteSpace(document.FileName)) throw new ArgumentException("FileName is required", "document");
            if (string.IsNullOrWhiteSpace(document.FileData)) throw new ArgumentException("FileData is required", "document");

            FtpLogger.LogInfo("============================================================");
            FtpLogger.LogInfo(string.Format("Google Drive Upload: REQ={0}, Bank={1}, File={2}", requirementId, bankId, document.FileName));

            var fileBytes = DecodeBase64(document.FileData);
            var safeFileName = Path.GetFileName(document.FileName);

            var token = GetAccessToken();
            var requirementFolderId = EnsureFolderExists(requirementId, _sharedFolderId, token);
            var bankFolderId = EnsureFolderExists(bankId, requirementFolderId, token);

            var uploaded = UploadFileMultipart(safeFileName, document.ContentType ?? "application/pdf", fileBytes, bankFolderId, token);

            FtpLogger.LogInfo(string.Format("Upload completed: FileId={0}, Name={1}", uploaded.Id, uploaded.Name));
            FtpLogger.LogInfo("============================================================");
            return uploaded.Name;
        }

        public string DownloadConsentDocument(string consentFileName, string requirementId, string bankId)
        {
            if (string.IsNullOrWhiteSpace(consentFileName) || string.IsNullOrWhiteSpace(requirementId) || string.IsNullOrWhiteSpace(bankId))
            {
                FtpLogger.LogWarning("DownloadConsentDocument called with missing parameters");
                return null;
            }

            FtpLogger.LogInfo("============================================================");
            FtpLogger.LogInfo(string.Format("Google Drive Download: REQ={0}, Bank={1}, File={2}", requirementId, bankId, consentFileName));

            var token = GetAccessToken();
            var requirementFolder = FindFolder(requirementId, _sharedFolderId, token);
            if (requirementFolder == null)
            {
                FtpLogger.LogWarning("Requirement folder not found on Google Drive: " + requirementId);
                return null;
            }

            var bankFolder = FindFolder(bankId, requirementFolder.Id, token);
            if (bankFolder == null)
            {
                FtpLogger.LogWarning("Bank folder not found on Google Drive: " + bankId);
                return null;
            }

            var file = FindFile(consentFileName, bankFolder.Id, token);
            if (file == null)
            {
                FtpLogger.LogWarning("File not found on Google Drive: " + consentFileName);
                return null;
            }

            var bytes = DownloadFile(file.Id, token);
            var base64 = Convert.ToBase64String(bytes);

            FtpLogger.LogInfo(string.Format("Download completed: FileId={0}, Bytes={1}", file.Id, bytes.Length));
            FtpLogger.LogInfo("============================================================");
            return base64;
        }

        private static void ValidateCredential(ServiceAccountCredentialFile credential, string path)
        {
            if (credential == null)
            {
                throw new InvalidOperationException("Unable to parse Google service-account credentials JSON: " + path);
            }

            if (string.IsNullOrWhiteSpace(credential.ClientEmail) ||
                string.IsNullOrWhiteSpace(credential.PrivateKey) ||
                string.IsNullOrWhiteSpace(credential.TokenUri))
            {
                throw new InvalidOperationException("Service-account credentials are incomplete. Required: client_email, private_key, token_uri.");
            }
        }

        private static string ResolveCredentialsPath(string configuredPath)
        {
            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, configuredPath));
        }

        private static byte[] DecodeBase64(string data)
        {
            var base64 = data;
            var comma = base64.IndexOf(',');
            if (comma >= 0 && base64.Substring(0, comma).IndexOf("base64", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                base64 = base64.Substring(comma + 1);
            }
            return Convert.FromBase64String(base64);
        }

        private string GetAccessToken()
        {
            lock (TokenLock)
            {
                if (!string.IsNullOrWhiteSpace(_accessToken) && DateTime.UtcNow < _accessTokenExpiresUtc)
                {
                    return _accessToken;
                }

                if (_useUserOAuth)
                {
                    var userTokenResponse = RequestAccessTokenWithRefreshToken();
                    if (string.IsNullOrWhiteSpace(userTokenResponse.AccessToken))
                    {
                        throw new InvalidOperationException("Google OAuth refresh-token flow did not return access_token.");
                    }

                    _accessToken = userTokenResponse.AccessToken;
                    var userExpiresIn = userTokenResponse.ExpiresIn <= 0 ? 3600 : userTokenResponse.ExpiresIn;
                    _accessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(30, userExpiresIn - 60));

                    FtpLogger.LogInfo(string.Format("Google OAuth user token acquired. Expires UTC: {0:yyyy-MM-dd HH:mm:ss}", _accessTokenExpiresUtc));
                    return _accessToken;
                }

                var issuedAt = DateTime.UtcNow;
                var expiresAt = issuedAt.AddMinutes(55);

                var header = new { alg = "RS256", typ = "JWT" };
                var payload = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "iss", _credential.ClientEmail },
                    { "scope", DriveScope },
                    { "aud", string.IsNullOrWhiteSpace(_credential.TokenUri) ? TokenEndpointFallback : _credential.TokenUri },
                    { "iat", ToUnixTime(issuedAt) },
                    { "exp", ToUnixTime(expiresAt) }
                };

                if (!string.IsNullOrWhiteSpace(_delegatedUserEmail))
                {
                    payload["sub"] = _delegatedUserEmail;
                }

                var payloadJson = JsonConvert.SerializeObject(payload);
                var unsignedToken = Base64UrlEncode(JsonConvert.SerializeObject(header)) + "." + Base64UrlEncode(payloadJson);
                var signature = SignJwt(unsignedToken, _credential.PrivateKey);
                var assertion = unsignedToken + "." + Base64UrlEncode(signature);

                var tokenResponse = RequestAccessToken(assertion);
                if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                {
                    throw new InvalidOperationException("Google OAuth token response did not include access_token.");
                }

                _accessToken = tokenResponse.AccessToken;
                var expiresIn = tokenResponse.ExpiresIn <= 0 ? 3600 : tokenResponse.ExpiresIn;
                _accessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(30, expiresIn - 60));

                FtpLogger.LogInfo(string.Format("Google OAuth token acquired. Expires UTC: {0:yyyy-MM-dd HH:mm:ss}", _accessTokenExpiresUtc));
                return _accessToken;
            }
        }

        private TokenResponse RequestAccessTokenWithRefreshToken()
        {
            var form = "grant_type=" + Uri.EscapeDataString("refresh_token") +
                       "&client_id=" + Uri.EscapeDataString(_oauthClientId) +
                       "&client_secret=" + Uri.EscapeDataString(_oauthClientSecret) +
                       "&refresh_token=" + Uri.EscapeDataString(_oauthRefreshToken);
            var data = Encoding.UTF8.GetBytes(form);

            var request = (HttpWebRequest)WebRequest.Create(TokenEndpointFallback);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "application/json";

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream ?? Stream.Null))
                {
                    var json = reader.ReadToEnd();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                    if (tokenResponse != null && !string.IsNullOrWhiteSpace(tokenResponse.Error))
                    {
                        throw new InvalidOperationException("Google OAuth refresh-token error: " + tokenResponse.Error + " - " + tokenResponse.ErrorDescription);
                    }
                    return tokenResponse;
                }
            }
            catch (WebException ex)
            {
                var details = ReadWebException(ex);
                throw new InvalidOperationException("Failed to obtain Google OAuth user access token: " + details, ex);
            }
        }

        private static long ToUnixTime(DateTime utc)
        {
            return (long)(utc - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private TokenResponse RequestAccessToken(string assertion)
        {
            var tokenUri = string.IsNullOrWhiteSpace(_credential.TokenUri) ? TokenEndpointFallback : _credential.TokenUri;
            var form = "grant_type=" + Uri.EscapeDataString("urn:ietf:params:oauth:grant-type:jwt-bearer") +
                       "&assertion=" + Uri.EscapeDataString(assertion);
            var data = Encoding.UTF8.GetBytes(form);

            var request = (HttpWebRequest)WebRequest.Create(tokenUri);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "application/json";

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream ?? Stream.Null))
                {
                    var json = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<TokenResponse>(json);
                }
            }
            catch (WebException ex)
            {
                var details = ReadWebException(ex);
                throw new InvalidOperationException("Failed to obtain Google OAuth access token: " + details, ex);
            }
        }

        private static string ReadWebException(WebException ex)
        {
            try
            {
                if (ex.Response == null) return ex.Message;
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    var body = reader.ReadToEnd();
                    return string.IsNullOrWhiteSpace(body) ? ex.Message : body;
                }
            }
            catch
            {
                return ex.Message;
            }
        }

        private string NormalizeDriveError(string operation, string rawDetails)
        {
            if (string.IsNullOrWhiteSpace(rawDetails))
            {
                return "Google Drive " + operation + " failed.";
            }

            if (rawDetails.IndexOf("storageQuotaExceeded", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (!string.IsNullOrWhiteSpace(_delegatedUserEmail))
                {
                    return "Google Drive " + operation + " failed: storage quota exceeded for delegated user context. " +
                           "Verify GoogleDrive_DelegatedUserEmail has enough Drive storage and that domain-wide delegation is enabled for the service account.";
                }

                if (_useUserOAuth)
                {
                    return "Google Drive " + operation + " failed: user storage quota exceeded. " +
                           "Verify the Google account linked to GoogleDrive_OAuthRefreshToken has sufficient available Drive storage.";
                }

                return "Google Drive " + operation + " failed: service-account storage quota exceeded. " +
                       "If this target is in My Drive, set GoogleDrive_DelegatedUserEmail and enable domain-wide delegation, " +
                       "or use a real Shared Drive folder for GoogleDrive_SharedFolderId and grant Content Manager access.";
            }

            return "Google Drive " + operation + " failed: " + rawDetails;
        }

        private static string EscapeQueryLiteral(string value)
        {
            return value.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        private DriveFileResult FindFolder(string name, string parentId, string token)
        {
            return FindDriveItem(name, parentId, token, true);
        }

        private DriveFileResult FindFile(string name, string parentId, string token)
        {
            return FindDriveItem(name, parentId, token, false);
        }

        private DriveFileResult FindDriveItem(string name, string parentId, string token, bool folder)
        {
            var escapedName = EscapeQueryLiteral(name);
            var mimeFilter = folder
                ? "mimeType='application/vnd.google-apps.folder'"
                : "mimeType!='application/vnd.google-apps.folder'";

            var query = string.Format("name='{0}' and '{1}' in parents and trashed=false and {2}", escapedName, parentId, mimeFilter);
            var url = "https://www.googleapis.com/drive/v3/files?q=" + Uri.EscapeDataString(query) +
                      "&fields=files(id,name)&supportsAllDrives=true&includeItemsFromAllDrives=true&pageSize=1";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    var json = reader.ReadToEnd();
                    var parsed = JsonConvert.DeserializeObject<DriveFileListResponse>(json);
                    return parsed != null && parsed.Files != null ? parsed.Files.FirstOrDefault() : null;
                }
            }
            catch (WebException ex)
            {
                var details = ReadWebException(ex);
                throw new InvalidOperationException(NormalizeDriveError("search", details), ex);
            }
        }

        private string EnsureFolderExists(string folderName, string parentId, string token)
        {
            var existing = FindFolder(folderName, parentId, token);
            if (existing != null)
            {
                return existing.Id;
            }

            var payload = new
            {
                name = folderName,
                mimeType = FolderMimeType,
                parents = new[] { parentId }
            };

            var json = JsonConvert.SerializeObject(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            var url = "https://www.googleapis.com/drive/v3/files?supportsAllDrives=true&fields=id,name";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json; charset=UTF-8";
            request.Accept = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream ?? Stream.Null))
                {
                    var responseJson = reader.ReadToEnd();
                    var created = JsonConvert.DeserializeObject<DriveFileResult>(responseJson);
                    if (created == null || string.IsNullOrWhiteSpace(created.Id))
                    {
                        throw new InvalidOperationException("Drive folder creation returned an empty id.");
                    }
                    return created.Id;
                }
            }
            catch (WebException ex)
            {
                var details = ReadWebException(ex);
                throw new InvalidOperationException(NormalizeDriveError("folder creation", details), ex);
            }
        }

        private DriveFileResult UploadFileMultipart(string fileName, string contentType, byte[] fileBytes, string parentId, string token)
        {
            var boundary = "================" + DateTime.UtcNow.Ticks;
            var metadata = JsonConvert.SerializeObject(new
            {
                name = fileName,
                parents = new[] { parentId }
            });

            var header = "--" + boundary + "\r\n" +
                         "Content-Type: application/json; charset=UTF-8\r\n\r\n" +
                         metadata + "\r\n" +
                         "--" + boundary + "\r\n" +
                         "Content-Type: " + (string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType) + "\r\n\r\n";

            var footer = "\r\n--" + boundary + "--\r\n";
            var headerBytes = Encoding.UTF8.GetBytes(header);
            var footerBytes = Encoding.UTF8.GetBytes(footer);

            var body = new byte[headerBytes.Length + fileBytes.Length + footerBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, body, 0, headerBytes.Length);
            Buffer.BlockCopy(fileBytes, 0, body, headerBytes.Length, fileBytes.Length);
            Buffer.BlockCopy(footerBytes, 0, body, headerBytes.Length + fileBytes.Length, footerBytes.Length);

            var url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&supportsAllDrives=true&fields=id,name";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "multipart/related; boundary=" + boundary;
            request.Accept = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
            request.ContentLength = body.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream ?? Stream.Null))
                {
                    var json = reader.ReadToEnd();
                    var uploaded = JsonConvert.DeserializeObject<DriveFileResult>(json);
                    if (uploaded == null || string.IsNullOrWhiteSpace(uploaded.Id))
                    {
                        throw new InvalidOperationException("Google Drive upload response did not include file id.");
                    }
                    return uploaded;
                }
            }
            catch (WebException ex)
            {
                var details = ReadWebException(ex);
                throw new InvalidOperationException(NormalizeDriveError("file upload", details), ex);
            }
        }

        private byte[] DownloadFile(string fileId, string token)
        {
            var url = "https://www.googleapis.com/drive/v3/files/" + Uri.EscapeDataString(fileId) + "?alt=media&supportsAllDrives=true";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var ms = new MemoryStream())
                {
                    if (responseStream != null)
                    {
                        responseStream.CopyTo(ms);
                    }
                    return ms.ToArray();
                }
            }
            catch (WebException ex)
            {
                var details = ReadWebException(ex);
                throw new InvalidOperationException(NormalizeDriveError("file download", details), ex);
            }
        }

        private static byte[] SignJwt(string unsignedToken, string pemPrivateKey)
        {
            var privateKeyBytes = DecodePem(pemPrivateKey);
            var rsaParameters = DecodePkcs8PrivateKey(privateKeyBytes);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParameters);
                return rsa.SignData(Encoding.UTF8.GetBytes(unsignedToken), CryptoConfig.MapNameToOID("SHA256"));
            }
        }

        private static string Base64UrlEncode(string plain)
        {
            return Base64UrlEncode(Encoding.UTF8.GetBytes(plain));
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] DecodePem(string pem)
        {
            var lines = pem
                .Replace("-----BEGIN PRIVATE KEY-----", string.Empty)
                .Replace("-----END PRIVATE KEY-----", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();

            return Convert.FromBase64String(lines);
        }

        private static RSAParameters DecodePkcs8PrivateKey(byte[] pkcs8)
        {
            using (var mem = new MemoryStream(pkcs8))
            using (var reader = new BinaryReader(mem))
            {
                ReadAsn1Sequence(reader);
                ReadAsn1Integer(reader); // version
                ReadAsn1Sequence(reader); // algorithm identifier sequence
                ReadAsn1Oid(reader); // rsaEncryption oid
                if (PeekTag(reader) == 0x05) ReadAsn1Null(reader); // optional NULL
                var privateKeyOctetString = ReadAsn1OctetString(reader);
                return DecodePkcs1PrivateKey(privateKeyOctetString);
            }
        }

        private static RSAParameters DecodePkcs1PrivateKey(byte[] pkcs1)
        {
            using (var mem = new MemoryStream(pkcs1))
            using (var reader = new BinaryReader(mem))
            {
                ReadAsn1Sequence(reader);
                ReadAsn1Integer(reader); // version

                var rsa = new RSAParameters
                {
                    Modulus = ReadAsn1Integer(reader),
                    Exponent = ReadAsn1Integer(reader),
                    D = ReadAsn1Integer(reader),
                    P = ReadAsn1Integer(reader),
                    Q = ReadAsn1Integer(reader),
                    DP = ReadAsn1Integer(reader),
                    DQ = ReadAsn1Integer(reader),
                    InverseQ = ReadAsn1Integer(reader)
                };

                return rsa;
            }
        }

        private static int PeekTag(BinaryReader reader)
        {
            var value = reader.ReadByte();
            reader.BaseStream.Position -= 1;
            return value;
        }

        private static void ReadAsn1Sequence(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag != 0x30) throw new InvalidOperationException("Invalid ASN.1: expected SEQUENCE");
            ReadAsn1Length(reader);
        }

        private static byte[] ReadAsn1Integer(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag != 0x02) throw new InvalidOperationException("Invalid ASN.1: expected INTEGER");
            var length = ReadAsn1Length(reader);
            var value = reader.ReadBytes(length);

            // Unsigned integer normalization
            if (value.Length > 1 && value[0] == 0x00)
            {
                var trimmed = new byte[value.Length - 1];
                Buffer.BlockCopy(value, 1, trimmed, 0, trimmed.Length);
                value = trimmed;
            }

            return value;
        }

        private static void ReadAsn1Oid(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag != 0x06) throw new InvalidOperationException("Invalid ASN.1: expected OID");
            var length = ReadAsn1Length(reader);
            reader.ReadBytes(length);
        }

        private static void ReadAsn1Null(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag != 0x05) throw new InvalidOperationException("Invalid ASN.1: expected NULL");
            var length = ReadAsn1Length(reader);
            if (length > 0)
            {
                reader.ReadBytes(length);
            }
        }

        private static byte[] ReadAsn1OctetString(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag != 0x04) throw new InvalidOperationException("Invalid ASN.1: expected OCTET STRING");
            var length = ReadAsn1Length(reader);
            return reader.ReadBytes(length);
        }

        private static int ReadAsn1Length(BinaryReader reader)
        {
            var length = reader.ReadByte();
            if ((length & 0x80) == 0)
            {
                return length;
            }

            var byteCount = length & 0x7F;
            if (byteCount <= 0 || byteCount > 4)
            {
                throw new InvalidOperationException("Invalid ASN.1 length encoding.");
            }

            var result = 0;
            for (var i = 0; i < byteCount; i++)
            {
                result = (result << 8) + reader.ReadByte();
            }
            return result;
        }

        private class ServiceAccountCredentialFile
        {
            [JsonProperty("client_email")]
            public string ClientEmail { get; set; }

            [JsonProperty("private_key")]
            public string PrivateKey { get; set; }

            [JsonProperty("token_uri")]
            public string TokenUri { get; set; }
        }

        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("error_description")]
            public string ErrorDescription { get; set; }
        }

        private class DriveFileListResponse
        {
            [JsonProperty("files")]
            public DriveFileResult[] Files { get; set; }
        }

        private class DriveFileResult
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}
