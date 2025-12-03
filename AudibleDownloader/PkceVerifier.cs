using System;
using System.Security.Cryptography;
using System.Text;

public static class PkceVerifier
{
    // This simulates what Amazon does when it receives our OAuth request
    public static bool VerifyCodeChallenge(string codeVerifier, string codeChallenge)
    {
        // Amazon receives the code_challenge in the OAuth URL
        // Then when we send the code_verifier in registration, Amazon should:
        // 1. Hash the code_verifier with SHA256
        // 2. Base64URL encode it
        // 3. Compare it to the code_challenge we sent earlier

        byte[] verifierBytes = Encoding.UTF8.GetBytes(codeVerifier);

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(verifierBytes);
            string computedChallenge = Base64UrlEncode(hash);

            return computedChallenge == codeChallenge;
        }
    }

    // Alternative version - what if Amazon expects the verifier as base64url bytes?
    public static bool VerifyCodeChallengeFromBase64(string codeVerifierBase64, string codeChallenge)
    {
        // Decode the base64url string back to bytes
        byte[] verifierBytes = Base64UrlDecode(codeVerifierBase64);

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(verifierBytes);
            string computedChallenge = Base64UrlEncode(hash);

            return computedChallenge == codeChallenge;
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        string base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if needed
        int padding = 4 - (base64.Length % 4);
        if (padding < 4)
        {
            base64 = base64 + new string('=', padding);
        }

        return Convert.FromBase64String(base64);
    }

    // Test method to verify our implementation
    public static void TestPkceFlow()
    {
        // Generate verifier bytes (this is what Python does)
        byte[] verifierBytes = new byte[32];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(verifierBytes);
        }

        // Encode to base64url (this is what we send to Amazon in registration)
        string codeVerifier = Base64UrlEncode(verifierBytes);

        // Hash and encode to create challenge (this is what we send in OAuth URL)
        string codeChallenge = Base64UrlEncode(SHA256.HashData(verifierBytes));

        Console.WriteLine($"Verifier bytes: {BitConverter.ToString(verifierBytes)}");
        Console.WriteLine($"Code Verifier (base64url): {codeVerifier}");
        Console.WriteLine($"Code Challenge (base64url): {codeChallenge}");

        // Test verification (simulating what Amazon does)
        Console.WriteLine("\n--- Testing verification method 1 (verifier as UTF-8 string) ---");
        bool isValid1 = VerifyCodeChallenge(codeVerifier, codeChallenge);
        Console.WriteLine($"Valid: {isValid1}");

        Console.WriteLine("\n--- Testing verification method 2 (verifier as base64url bytes) ---");
        bool isValid2 = VerifyCodeChallengeFromBase64(codeVerifier, codeChallenge);
        Console.WriteLine($"Valid: {isValid2}");
    }
}
