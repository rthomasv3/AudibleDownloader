using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AudibleDownloader.Models;
using GitCredentialManager;

namespace AudibleDownloader;

internal class AuthManager
{
    #region Fields

    private static readonly string _storeName = "AudibleDownloader";
    private static readonly string _serviceName = "AuthManager://Credentials";
    private static readonly string _userName = "AudibleUser";

    private readonly Config _config;
    private readonly ICredentialStore _credentialStore;

    private byte[] _encryptionKey;

    #endregion

    #region Constructor

    public AuthManager(Config config)
    {
        _config = config;

        _credentialStore = CredentialManager.Create(_storeName);
        ICredential credential = _credentialStore.Get(_serviceName, _userName);

        if (credential != null)
        {
            _encryptionKey = Convert.FromBase64String(credential.Password);
        }
        else
        {
            _encryptionKey = GenerateKey();
            string keyBase64 = Convert.ToBase64String(_encryptionKey);
            _credentialStore.AddOrUpdate(_serviceName, _userName, keyBase64);
        }
    }

    #endregion

    #region Public Methods

    public void SaveAuthFile(AuthFile auth)
    {
        string authJson = JsonSerializer.Serialize(auth, AudibleJsonContext.Default.AuthFile);
        byte[] plaintext = Encoding.UTF8.GetBytes(authJson);
        byte[] encrypted = Encrypt(plaintext, _encryptionKey);
        File.WriteAllBytes(_config.AuthFilePath, encrypted);
    }

    public AuthFile GetAuthFile()
    {
        if (!File.Exists(_config.AuthFilePath))
            return null;

        byte[] encryptedData = File.ReadAllBytes(_config.AuthFilePath);
        byte[] plaintext = Decrypt(encryptedData, _encryptionKey);
        string authJson = Encoding.UTF8.GetString(plaintext);
        return JsonSerializer.Deserialize(authJson, AudibleJsonContext.Default.AuthFile);
    }

    #endregion

    #region Private Methods

    private static byte[] GenerateKey()
    {
        byte[] key = new byte[32]; // 256 bits for AES-256
        RandomNumberGenerator.Fill(key);
        return key;
    }

    private static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        // Generate a random 12-byte nonce (96 bits is standard for GCM)
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        // Tag for authentication
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        byte[] ciphertext = new byte[plaintext.Length];

        using (var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // Format: [nonce (12 bytes)][tag (16 bytes)][ciphertext]
        byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return result;
    }

    private static byte[] Decrypt(byte[] encryptedData, byte[] key)
    {
        // Extract nonce, tag, and ciphertext
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        byte[] ciphertext = new byte[encryptedData.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(encryptedData, nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(encryptedData, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

        byte[] plaintext = new byte[ciphertext.Length];

        using (var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return plaintext;
    }

    #endregion
}
