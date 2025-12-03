using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using AudibleDownloader.Models;
using AudibleDownloader.Models.Audible;

namespace AudibleDownloader;

internal class AudibleRegister
{
    private readonly Config _config;

    private string _authCode;
    private string _codeVerifier;
    private string _codeChallenge;
    private string _domain;
    private string _serial;

    public AudibleRegister(Config config)
    {
        _config = config;
    }

    public async Task CreateAuthFileAsync()
    {
        string clientId = BuildClientId(_serial);
        RegistrationRequest request = BuildRegistrationRequest(clientId);

        string requestBody = JsonSerializer.Serialize(request, AudibleJsonContext.Default.RegistrationRequest);

        HttpClient httpClient = new HttpClient();
        string registrationUrl = $"https://api.amazon.{_domain}/auth/register";
        StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync(registrationUrl, content);
        string responseBody = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        RegistrationResponse registrationResponse = JsonSerializer.Deserialize(responseBody, AudibleJsonContext.Default.RegistrationResponse);
        AuthFile authFile = CreateAuthFile(registrationResponse, _domain);

        string authJson = JsonSerializer.Serialize(authFile, AudibleJsonContext.Default.AuthFile);

        File.WriteAllText(_config.AuthFilePath, authJson);
    }

    public async Task CreateAuthFileAsync(AuthResult authResult)
    {
        string clientId = BuildClientId(authResult.Serial);
        RegistrationRequest request = BuildRegistrationRequest(authResult, clientId);

        string requestBody = JsonSerializer.Serialize(request, AudibleJsonContext.Default.RegistrationRequest);

        HttpClient httpClient = new HttpClient();
        string registrationUrl = $"https://api.amazon.{authResult.Domain}/auth/register";
        StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync(registrationUrl, content);
        string responseBody = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        RegistrationResponse registrationResponse = JsonSerializer.Deserialize(responseBody, AudibleJsonContext.Default.RegistrationResponse);
        AuthFile authFile = CreateAuthFile(registrationResponse, authResult.Domain);

        string authJson = JsonSerializer.Serialize(authFile, AudibleJsonContext.Default.AuthFile);

        File.WriteAllText(_config.AuthFilePath, authJson);
    }

    public string GenerateAudibleSignInUrl(string countryCode = "us", string domain = "com", string marketPlaceId = "AF2M0KC94RCEA")
    {
        _domain = domain;

        byte[] randomBytes = new byte[32];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        _codeVerifier = Base64UrlEncode(randomBytes);

        _codeChallenge = CreateS256CodeChallenge(_codeVerifier);
        _serial = GenerateSerial();

        bool isValid = PkceVerifier.VerifyCodeChallenge(_codeVerifier, _codeChallenge);

        return BuildOAuthUrl(countryCode, _domain, marketPlaceId, _codeChallenge, _serial);
    }

    public string ExtractAuthorizationCode(string url)
    {
        Uri uri = new(url);
        NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
        _authCode = query["openid.oa2.authorization_code"];

        if (string.IsNullOrEmpty(_authCode))
        {
            throw new Exception("Authorization code not found in URL");
        }

        return _authCode;
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string CreateS256CodeChallenge(string verifier)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string GenerateSerial()
    {
        return Guid.NewGuid().ToString("N").ToUpper();
    }

    private static string BuildOAuthUrl(string countryCode, string domain, string marketPlaceId,
        string codeChallenge, string serial)
    {
        string baseUrl = $"https://www.amazon.{domain}";
        string clientId = AudibleRegister.BuildClientId(serial);

        Dictionary<string, string> parameters = new()
        {
            { "openid.oa2.response_type", "code" },
            { "openid.oa2.code_challenge_method", "S256" },
            { "openid.oa2.code_challenge", codeChallenge },
            { "openid.return_to", $"https://www.amazon.{domain}/ap/maplanding" },
            { "openid.assoc_handle", $"amzn_audible_ios_{countryCode}" },
            { "openid.identity", "http://specs.openid.net/auth/2.0/identifier_select" },
            { "pageId", "amzn_audible_ios" },
            { "accountStatusPolicy", "P1" },
            { "openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select" },
            { "openid.mode", "checkid_setup" },
            { "openid.ns.oa2", "http://www.amazon.com/ap/ext/oauth/2" },
            { "openid.oa2.client_id", $"device:{clientId}" },
            { "openid.ns.pape", "http://specs.openid.net/extensions/pape/1.0" },
            { "marketPlaceId", marketPlaceId },
            { "openid.oa2.scope", "device_auth_access" },
            { "forceMobileLayout", "true" },
            { "openid.ns", "http://specs.openid.net/auth/2.0" },
            { "openid.pape.max_auth_age", "0" }
        };

        string queryString = string.Join("&", parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{baseUrl}/ap/signin?{queryString}";
    }

    private static string BuildClientId(string serial)
    {
        string clientIdString = serial + "#A2CZJZGLK2JJVM";
        byte[] bytes = Encoding.UTF8.GetBytes(clientIdString);
        StringBuilder hex = new StringBuilder(bytes.Length * 2);

        foreach (byte b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    private static RegistrationRequest BuildRegistrationRequest(AuthResult authResult, string clientId)
    {
        RegistrationRequest request = new RegistrationRequest
        {
            RequestedTokenType = new[] { "bearer", "mac_dms", "website_cookies", "store_authentication_cookie" },
            Cookies = new CookiesRequest
            {
                WebsiteCookies = new object[0],
                Domain = $".amazon.{authResult.Domain}"
            },
            RegistrationData = new RegistrationData
            {
                Domain = "Device",
                AppVersion = "3.56.2",
                DeviceSerial = authResult.Serial,
                DeviceType = "A2CZJZGLK2JJVM",
                DeviceName = "%FIRST_NAME%%FIRST_NAME_POSSESSIVE_STRING%%DUPE_STRATEGY_1ST%Audible for iPhone",
                OsVersion = "15.0.0",
                SoftwareVersion = "35602678",
                DeviceModel = "iPhone",
                AppName = "Audible"
            },
            AuthData = new AuthData
            {
                ClientId = clientId,
                AuthorizationCode = authResult.AuthorizationCode,
                CodeVerifier = authResult.CodeVerifier,
                CodeAlgorithm = "SHA-256",
                ClientDomain = "DeviceLegacy"
            },
            RequestedExtensions = new[] { "device_info", "customer_info" }
        };

        return request;
    }

    private RegistrationRequest BuildRegistrationRequest(string clientId)
    {
        RegistrationRequest request = new RegistrationRequest
        {
            RequestedTokenType = new[] { "bearer", "mac_dms", "website_cookies", "store_authentication_cookie" },
            Cookies = new CookiesRequest
            {
                WebsiteCookies = new object[0],
                Domain = $".amazon.{_domain}"
            },
            RegistrationData = new RegistrationData
            {
                Domain = "Device",
                AppVersion = "3.56.2",
                DeviceSerial = _serial,
                DeviceType = "A2CZJZGLK2JJVM",
                DeviceName = "%FIRST_NAME%%FIRST_NAME_POSSESSIVE_STRING%%DUPE_STRATEGY_1ST%Audible for iPhone",
                OsVersion = "15.0.0",
                SoftwareVersion = "35602678",
                DeviceModel = "iPhone",
                AppName = "Audible"
            },
            AuthData = new AuthData
            {
                ClientId = clientId,
                AuthorizationCode = _authCode,
                CodeVerifier =  _codeVerifier,
                CodeAlgorithm = "SHA-256",
                ClientDomain = "DeviceLegacy"
            },
            RequestedExtensions = new[] { "device_info", "customer_info" }
        };

        return request;
    }

    private static AuthFile CreateAuthFile(RegistrationResponse registrationResponse, string domain)
    {
        SuccessResponse successResponse = registrationResponse.Response.Success;
        Tokens tokens = successResponse.Tokens;
        Extensions extensions = successResponse.Extensions;

        int expiresIn = int.Parse(tokens.Bearer.ExpiresIn);
        double expiresTimestamp = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToUnixTimeSeconds();

        Dictionary<string, string> websiteCookies = new Dictionary<string, string>();
        if (tokens.WebsiteCookies != null)
        {
            foreach (WebsiteCookie cookie in tokens.WebsiteCookies)
            {
                string value = cookie.Value.Replace("\"", "");
                websiteCookies[cookie.Name] = value;
            }
        }

        AuthFile authFile = new AuthFile
        {
            WebsiteCookies = websiteCookies,
            AdpToken = tokens.MacDms.AdpToken,
            AccessToken = tokens.Bearer.AccessToken,
            RefreshToken = tokens.Bearer.RefreshToken,
            DevicePrivateKey = tokens.MacDms.DevicePrivateKey,
            StoreAuthenticationCookie = tokens.StoreAuthenticationCookie,
            DeviceInfo = extensions.DeviceInfo,
            CustomerInfo = extensions.CustomerInfo,
            Expires = expiresTimestamp,
            LocaleCode = GetCountryCodeFromDomain(domain),
            WithUsername = false,
            ActivationBytes = null
        };

        return authFile;
    }

    private static string GetCountryCodeFromDomain(string domain)
    {
        Dictionary<string, string> domainToCountryCode = new Dictionary<string, string>
        {
            ["com"] = "us",
            ["co.uk"] = "uk",
            ["de"] = "de",
            ["fr"] = "fr",
            ["ca"] = "ca",
            ["it"] = "it",
            ["com.au"] = "au",
            ["in"] = "in",
            ["co.jp"] = "jp",
            ["es"] = "es",
            ["com.br"] = "br"
        };

        string countryCode = "us";
        if (domainToCountryCode.ContainsKey(domain))
        {
            countryCode = domainToCountryCode[domain];
        }

        return countryCode;
    }
}
