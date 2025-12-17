using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AAXClean;
using AudibleDownloader.Enums;
using AudibleDownloader.Models;
using AudibleDownloader.Models.Audible;
using GaldrJson;

namespace AudibleDownloader;

internal class AudibleClient : IDisposable
{
    #region Fields

    private readonly HttpClient _httpClient;
    private AuthFile _auth;
    private readonly string _apiUrl;
    private RSA _rsaKey;
    private readonly AuthManager _authManager;
    private readonly IGaldrJsonSerializer _galdrJson;

    private ConcurrentDictionary<string, DownloadBookProgressResult> _downloadProgress = new();

    #endregion

    #region Constructor(s)

    public AudibleClient(AuthManager authManager, IGaldrJsonSerializer galdrJson)
    {
        _authManager = authManager;
        _galdrJson = galdrJson;

        AuthFile auth = _authManager.GetAuthFile();

        if (auth == null)
        {
            throw new Exception("Failed to load auth file");
        }

        _auth = auth;
        _apiUrl = $"https://api.audible.{GetDomainFromCountryCode(_auth.LocaleCode)}";

        HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Charset", "utf-8");

        _rsaKey = LoadRsaPrivateKey(_auth.DevicePrivateKey);
    }

    #endregion

    #region Public Methods

    public async Task<string> GetLibraryAsync(int numResults = 1000)
    {
        await EnsureValidAccessTokenAsync();

        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["num_results"] = numResults.ToString(),
            ["response_groups"] = "product_desc,product_attrs,media,relationships,is_playable,series",
            ["sort_by"] = "-PurchaseDate"
        };

        string response = await GetAsync("1.0/library", parameters);
        return response;
    }

    public async Task<List<LibraryItem>> GetAllLibraryItemsAsync(int pageSize = 1000)
    {
        List<LibraryItem> allItems = new List<LibraryItem>();

        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            await EnsureValidAccessTokenAsync();

            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                ["num_results"] = pageSize.ToString(),
                ["page"] = currentPage.ToString(),
                ["response_groups"] = "product_desc,product_attrs,media,relationships,is_playable,series",
                ["sort_by"] = "Title"
                // [-Author, -Length, -Narrator, -PurchaseDate, -Title, Author, Length, Narrator, PurchaseDate, Title]
            };

            string responseJson = await GetAsync("1.0/library", parameters);
            LibraryResponse response = _galdrJson.Deserialize<LibraryResponse>(responseJson);

            if (response?.Items == null || response.Items.Length == 0)
            {
                hasMorePages = false;
            }
            else
            {
                allItems.AddRange(response.Items);

                if (response.Items.Length < pageSize)
                {
                    hasMorePages = false;
                }
                else
                {
                    currentPage++;
                }
            }
        }

        return allItems;
    }

    public async Task<LibraryItem> GetLibraryItemDetailsAsync(string asin)
    {
        await EnsureValidAccessTokenAsync();

        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["response_groups"] = "product_desc,product_attrs,media,relationships"
        };

        string responseJson = await GetAsync($"1.0/library/{asin}", parameters);
        LibraryItemResponse response = _galdrJson.Deserialize<LibraryItemResponse>(responseJson);

        return response?.Item;
    }

    public async Task<string> GetActivationBytesAsync(bool forceRefresh = false)
    {
        await EnsureValidAccessTokenAsync();

        // Return cached value if available and not forcing refresh
        if (!forceRefresh && !string.IsNullOrEmpty(_auth.ActivationBytes))
        {
            return _auth.ActivationBytes;
        }

        // Fetch activation blob
        byte[] activationBlob = await FetchActivationBlobAsync();

        // Extract activation bytes from blob
        string activationBytes = ExtractActivationBytes(activationBlob);

        // Save to auth file
        _auth.ActivationBytes = activationBytes;
        SaveAuthFile();

        return activationBytes;
    }

    public async Task<int> DownloadBookAsync(LibraryResult libraryEntry, string saveFileDirectory, string codec = "LC_128_44100_stereo")
    {
        UpdateProgress(libraryEntry.Asin, null, "Starting download...", DownloadStatus.NotStarted);

        await EnsureValidAccessTokenAsync();

        if (string.IsNullOrEmpty(_auth.AdpToken))
        {
            throw new Exception("No adp token present. Can't get download link.");
        }

        List<byte[]> parts = new();

        if (libraryEntry.Relationships != null && libraryEntry.Relationships.Count > 0)
        {
            for (int i = 0; i < libraryEntry.Relationships.Count; i++)
            {
                await EnsureValidAccessTokenAsync();

                LibraryRelationship relationship = libraryEntry.Relationships[i];
                string downloadUrl = await GetDownloadLinkAsync(relationship.Asin, codec);

                UpdateProgress(libraryEntry.Asin, null, $"Downloading part {i + 1} of {libraryEntry.Relationships.Count}", DownloadStatus.Downloading);

                byte[] fileBytes = await DownloadFileFromUrlAsync(libraryEntry.Asin, downloadUrl, i, libraryEntry.Relationships.Count);
                parts.Add(fileBytes);
            }
        }
        else
        {
            string downloadUrl = await GetDownloadLinkAsync(libraryEntry.Asin, codec);
            UpdateProgress(libraryEntry.Asin, null, $"Downloading...", DownloadStatus.Downloading);
            byte[] fileBytes = await DownloadFileFromUrlAsync(libraryEntry.Asin, downloadUrl);
            parts.Add(fileBytes);
        }

        UpdateProgress(libraryEntry.Asin, 1.0, "Decrypting...", DownloadStatus.Decrypting);

        if (!Directory.Exists(saveFileDirectory))
        {
            Directory.CreateDirectory(saveFileDirectory);
        }

        await GetActivationBytesAsync();

        for (int i = 0; i < parts.Count; i++)
        {
            DecryptedBookResult decryptedBook = await DecryptBook(parts[i]);

            File.WriteAllBytes(Path.Combine(saveFileDirectory, decryptedBook.SafeFileName), decryptedBook.FileData);
        }

        UpdateProgress(libraryEntry.Asin, 1.0, "Done", DownloadStatus.Completed);

        return parts.Count;
    }

    public DownloadBookProgressResult GetDownloadProgress(string asin)
    {
        _downloadProgress.TryGetValue(asin, out DownloadBookProgressResult value);
        return value;
    }

    public void ClearDownloadProgress(string asin)
    {
        _downloadProgress.TryRemove(asin, out _);
    }

    public async Task<DecryptedBookResult> DecryptBook(byte[] encryptedBook)
    {
        using MemoryStream encryptedStream = new MemoryStream(encryptedBook);
        AaxFile aaxFile = new(encryptedStream);
        aaxFile.SetDecryptionKey(_auth.ActivationBytes);
        using MemoryStream decryptedStream = new MemoryStream();
        await aaxFile.ConvertToMp4aAsync(decryptedStream);
        byte[] decryptedFileData = decryptedStream.ToArray();

        string author = GetAuthor(aaxFile);
        string title = RemoveInvalidNameChars(aaxFile.AppleTags.Title);
        string fileName = $"{RemoveInvalidNameChars(aaxFile.AppleTags.Title)}.m4a";

        return new DecryptedBookResult()
        {
            SafeAuthorName = author,
            SafeFileName = fileName,
            SafeTitle = title,
            FileData = decryptedFileData
        };
    }

    public async Task<DeregisterResponse> DeregisterAsync(bool deregisterAll = false)
    {
        await EnsureValidAccessTokenAsync();

        string domain = GetDomainFromCountryCode(_auth.LocaleCode);
        string targetDomain = _auth.WithUsername ? "audible" : "amazon";
        string url = $"https://api.{targetDomain}.{domain}/auth/deregister";

        DeregisterRequest requestBody = new DeregisterRequest
        {
            DeregisterAllExistingAccounts = deregisterAll
        };

        string jsonBody = _galdrJson.Serialize<DeregisterRequest>(requestBody);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        request.Headers.Add("Authorization", $"Bearer {_auth.AccessToken}");

        HttpResponseMessage response = await _httpClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        DeregisterResponse deregisterResponse = _galdrJson.Deserialize<DeregisterResponse>(responseBody);

        return deregisterResponse;
    }

    public string RemoveInvalidNameChars(string filename)
    {
        return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }

    public AuthFile GetAuthInfo()
    {
        return _auth;
    }

    public void Dispose()
    {
        _rsaKey?.Dispose();
        _httpClient?.Dispose();
    }

    #endregion

    #region Private Methods

    private async Task<string> GetDownloadLinkAsync(string asin, string codec)
    {
        string contentUrl = "https://cde-ta-g7g.amazon.com/FionaCDEServiceEngine/FSDownloadContent";

        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["type"] = "AUDI",
            ["currentTransportMethod"] = "WIFI",
            ["key"] = asin,
            ["codec"] = codec
        };

        string queryString = string.Join("&",
            parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        string fullUrl = $"{contentUrl}?{queryString}";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
        SignRequest(request, Array.Empty<byte>());

        HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        string test = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != System.Net.HttpStatusCode.Redirect &&
            response.StatusCode != System.Net.HttpStatusCode.MovedPermanently &&
            response.StatusCode != System.Net.HttpStatusCode.Found)
        {
            throw new Exception($"Expected redirect but got status code: {response.StatusCode}");
        }

        string location = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(location))
        {
            throw new Exception("No Location header found in redirect response");
        }

        // Replace domain with locale-specific domain
        string domain = GetDomainFromCountryCode(_auth.LocaleCode);
        string adjustedLink = location.Replace("cds.audible.com", $"cds.audible.{domain}");

        return adjustedLink;
    }

    private async Task<byte[]> DownloadFileFromUrlAsync(string url)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
        return fileBytes;
    }

    private async Task<byte[]> DownloadFileFromUrlAsync(string asin, string url, int partNumber = 0, int totalParts = 1)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? contentLength = response.Content.Headers.ContentLength;

        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
        {
            long totalBytesRead = 0;
            byte[] buffer = new byte[131072]; // 128 KB

            using (MemoryStream ms = new())
            {
                while (true)
                {
                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break; // End of stream
                    }

                    await ms.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    // Only report progress if we know the total size
                    if (contentLength.HasValue)
                    {
                        double currentPartProgress = (double)totalBytesRead / contentLength.Value;
                        //_downloadProgress[asin] = ((double)partNumber + currentPartProgress) / (double)totalParts;
                        UpdateProgress(asin, ((double)partNumber + currentPartProgress) / (double)totalParts, downloadStatus: DownloadStatus.Downloading);
                    }
                }

                //_downloadProgress[asin] = (partNumber + 1.0) / (double)totalParts;
                UpdateProgress(asin, (partNumber + 1.0) / (double)totalParts, downloadStatus: DownloadStatus.Downloading);

                return ms.ToArray();
            }
        }
    }

    private async Task<byte[]> FetchActivationBlobAsync()
    {
        string domain = GetDomainFromCountryCode(_auth.LocaleCode);
        string url = $"https://www.audible.{domain}/license/token?player_manuf=Audible,iPhone&action=register&player_model=iPhone";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        SignRequest(request, Array.Empty<byte>());

        HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        byte[] content = await response.Content.ReadAsByteArrayAsync();
        return content;
    }

    private string ExtractActivationBytes(byte[] activationBlob)
    {
        // Check for error conditions
        string blobText = Encoding.UTF8.GetString(activationBlob);
        if (blobText.Contains("BAD_LOGIN") || blobText.Contains("Whoops") || !blobText.Contains("group_id"))
        {
            throw new Exception("Activation failed. Invalid activation blob received.");
        }

        // Extract last 0x238 bytes (568 bytes)
        int extractLength = 0x238;
        if (activationBlob.Length < extractLength)
        {
            throw new Exception("Activation blob is too short.");
        }

        byte[] relevantData = activationBlob[^extractLength..];

        // Strip newline characters (process in chunks of 71 bytes: 70 data + 1 newline)
        List<byte> processedBytes = new List<byte>();
        for (int i = 0; i < 8; i++)
        {
            int offset = i * 71;
            int length = Math.Min(70, relevantData.Length - offset);
            if (length > 0)
            {
                byte[] chunk = new byte[length];
                Array.Copy(relevantData, offset, chunk, 0, length);
                processedBytes.AddRange(chunk);
            }
        }

        byte[] cleanedData = processedBytes.ToArray();

        // Extract first 4 bytes and convert to hex
        if (cleanedData.Length < 4)
        {
            throw new Exception("Cleaned activation data is too short.");
        }

        uint activationValue = BitConverter.ToUInt32(cleanedData, 0);
        string activationHex = activationValue.ToString("x");

        // Pad with zeros if necessary to reach 8 characters
        if (activationHex.Length < 8)
        {
            activationHex = activationHex.PadLeft(8, '0');
        }

        return activationHex;
    }

    private async Task EnsureValidAccessTokenAsync()
    {
        bool isExpired = IsAccessTokenExpired();

        if (isExpired)
        {
            await RefreshAccessTokenAsync();
        }
    }

    private bool IsAccessTokenExpired()
    {
        DateTimeOffset expirationTime = DateTimeOffset.FromUnixTimeSeconds((long)_auth.Expires);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        bool isExpired = expirationTime <= now;
        return isExpired;
    }

    private async Task RefreshAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_auth.RefreshToken))
        {
            throw new Exception("No refresh token available. Cannot refresh access token.");
        }

        string domain = GetDomainFromCountryCode(_auth.LocaleCode);
        string targetDomain = _auth.WithUsername ? "audible" : "amazon";
        string url = $"https://api.{targetDomain}.{domain}/auth/token";

        Dictionary<string, string> formData = new Dictionary<string, string>
        {
            ["app_name"] = "Audible",
            ["app_version"] = "3.56.2",
            ["source_token"] = _auth.RefreshToken,
            ["requested_token_type"] = "access_token",
            ["source_token_type"] = "refresh_token"
        };

        FormUrlEncodedContent content = new FormUrlEncodedContent(formData);

        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        RefreshTokenResponse refreshResponse = _galdrJson.Deserialize<RefreshTokenResponse>(responseBody);

        if (refreshResponse == null)
        {
            throw new Exception("Failed to parse refresh token response");
        }

        double newExpires = DateTimeOffset.UtcNow.AddSeconds(refreshResponse.ExpiresIn).ToUnixTimeSeconds();

        _auth.AccessToken = refreshResponse.AccessToken;
        _auth.Expires = newExpires;

        SaveAuthFile();
    }

    private void SaveAuthFile()
    {
        _authManager.SaveAuthFile(_auth);
    }

    private async Task<string> GetAsync(string path, Dictionary<string, string> parameters = null)
    {
        string url = BuildApiUrl(path);

        if (parameters != null && parameters.Count > 0)
        {
            string queryString = string.Join("&",
                parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            url += "?" + queryString;
        }

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        SignRequest(request, Array.Empty<byte>());

        HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }


    private void SignRequest(HttpRequestMessage request, byte[] body)
    {
        string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string path = request.RequestUri.PathAndQuery;
        string method = request.Method.ToString();
        string bodyString = Encoding.UTF8.GetString(body);

        string data = $"{method}\n{path}\n{date}\n{bodyString}\n{_auth.AdpToken}";
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        byte[] signature = _rsaKey.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string signatureBase64 = Convert.ToBase64String(signature);

        request.Headers.Add("x-adp-token", _auth.AdpToken);
        request.Headers.Add("x-adp-alg", "SHA256withRSA:1.0");
        request.Headers.Add("x-adp-signature", $"{signatureBase64}:{date}");
    }

    private string BuildApiUrl(string path)
    {
        string cleanPath = path.StartsWith("/") ? path.Substring(1) : path;

        if (!cleanPath.StartsWith("1.0") && !cleanPath.StartsWith("0.0"))
        {
            cleanPath = "1.0/" + cleanPath;
        }

        string fullPath = _apiUrl + "/" + cleanPath;
        return fullPath;
    }

    private RSA LoadRsaPrivateKey(string privateKeyPem)
    {
        RSA rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa;
    }

    private string GetDomainFromCountryCode(string countryCode)
    {
        Dictionary<string, string> countryCodeToDomain = new Dictionary<string, string>
        {
            ["us"] = "com",
            ["uk"] = "co.uk",
            ["de"] = "de",
            ["fr"] = "fr",
            ["ca"] = "ca",
            ["it"] = "it",
            ["au"] = "com.au",
            ["in"] = "in",
            ["jp"] = "co.jp",
            ["es"] = "es",
            ["br"] = "com.br"
        };

        string domain = "com";
        if (countryCodeToDomain.ContainsKey(countryCode))
        {
            domain = countryCodeToDomain[countryCode];
        }

        return domain;
    }

    private string GetAuthor(AaxFile book)
    {
        string author = book.AppleTags.Artist;
        if (String.IsNullOrWhiteSpace(author))
        {
            author = book.AppleTags.FirstAuthor;
        }
        return author;
    }

    private void UpdateProgress(string asin, double? progress = null, string message = null, DownloadStatus downloadStatus = DownloadStatus.NotStarted)
    {
        _downloadProgress.AddOrUpdate(
            asin,
            new DownloadBookProgressResult
            {
                Progress = progress ?? 0.0001,
                Message = message ?? string.Empty,
                Status = (int)downloadStatus
            },
            (key, existing) =>
            {
                if (progress.HasValue)
                    existing.Progress = progress.Value;

                if (message != null)
                    existing.Message = message;

                existing.Status = (int)downloadStatus;

                return existing;
            }
        );
    }

    #endregion
}
