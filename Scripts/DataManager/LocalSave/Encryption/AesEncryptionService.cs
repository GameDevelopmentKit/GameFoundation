namespace DataManager.LocalSave.Encryption
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// AES-256 encryption service implementation using device-unique key derivation.
    /// Provides secure encryption/decryption with integrity verification (HMAC).
    /// Salt prefix is configurable via DataManagerConfig to avoid game-specific hardcoding.
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        private const int KeySize = 256;          // AES-256
        private const int BlockSize = 128;        // AES block size
        private const int Iterations = 10000;     // PBKDF2 iterations
        private const int SaltSize = 16;          // 128-bit salt
        private const int IvSize = 16;            // 128-bit IV
        private const int HmacSize = 32;          // 256-bit HMAC

        private const string DefaultSaltPrefix = "SaltGame";

        private readonly byte[] encryptionKey;
        private readonly byte[] hmacKey;

        public AesEncryptionService() : this(DefaultSaltPrefix) { }

        public AesEncryptionService(string saltPrefix)
        {
            if (string.IsNullOrEmpty(saltPrefix))
            {
                saltPrefix = DefaultSaltPrefix;
            }

            // Derive encryption keys from device-unique identifier
            var deviceId = SystemInfo.deviceUniqueIdentifier;

            // Use different salts for encryption and HMAC keys
            var encryptionSalt = Encoding.UTF8.GetBytes($"{saltPrefix}_Encrypt_V1");
            var hmacSalt = Encoding.UTF8.GetBytes($"{saltPrefix}_HMAC_V1");

            // Derive 256-bit keys using PBKDF2
            using (var deriveBytes = new Rfc2898DeriveBytes(deviceId, encryptionSalt, Iterations))
            {
                this.encryptionKey = deriveBytes.GetBytes(KeySize / 8);
            }

            using (var deriveBytes = new Rfc2898DeriveBytes(deviceId, hmacSalt, Iterations))
            {
                this.hmacKey = deriveBytes.GetBytes(HmacSize);
            }
        }

        /// <summary>
        /// Encrypts plain text using AES-256-CBC with HMAC-SHA256 for integrity.
        /// Format: [IV(16)][CipherText(N)][HMAC(32)]
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = this.encryptionKey;
                    aes.GenerateIV(); // Random IV for each encryption

                    byte[] encrypted;

                    // Encrypt the plain text
                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        encrypted = msEncrypt.ToArray();
                    }

                    // Combine IV + CipherText
                    var ivAndCipherText = new byte[IvSize + encrypted.Length];
                    Array.Copy(aes.IV, 0, ivAndCipherText, 0, IvSize);
                    Array.Copy(encrypted, 0, ivAndCipherText, IvSize, encrypted.Length);

                    // Compute HMAC for integrity verification
                    byte[] hmac;
                    using (var hmacSha256 = new HMACSHA256(this.hmacKey))
                    {
                        hmac = hmacSha256.ComputeHash(ivAndCipherText);
                    }

                    // Final format: IV + CipherText + HMAC
                    var result = new byte[ivAndCipherText.Length + hmac.Length];
                    Array.Copy(ivAndCipherText, 0, result, 0, ivAndCipherText.Length);
                    Array.Copy(hmac, 0, result, ivAndCipherText.Length, hmac.Length);

                    return Convert.ToBase64String(result);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AesEncryptionService] Encryption failed: {ex.Message}");
                throw new CryptographicException("Failed to encrypt data", ex);
            }
        }

        /// <summary>
        /// Decrypts cipher text and verifies integrity using HMAC.
        /// </summary>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return string.Empty;
            }

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                // Validate minimum size: IV + at least 1 block + HMAC
                if (fullCipher.Length < IvSize + BlockSize / 8 + HmacSize)
                {
                    throw new CryptographicException("Invalid cipher text: too short");
                }

                // Extract components
                var ivAndCipherTextLength = fullCipher.Length - HmacSize;
                var ivAndCipherText = new byte[ivAndCipherTextLength];
                var receivedHmac = new byte[HmacSize];

                Array.Copy(fullCipher, 0, ivAndCipherText, 0, ivAndCipherTextLength);
                Array.Copy(fullCipher, ivAndCipherTextLength, receivedHmac, 0, HmacSize);

                // Verify HMAC (integrity check)
                byte[] computedHmac;
                using (var hmacSha256 = new HMACSHA256(this.hmacKey))
                {
                    computedHmac = hmacSha256.ComputeHash(ivAndCipherText);
                }

                if (!ConstantTimeEquals(receivedHmac, computedHmac))
                {
                    throw new CryptographicException("HMAC verification failed: data may be corrupted or tampered");
                }

                // Extract IV and cipher text
                var iv = new byte[IvSize];
                var cipherTextBytes = new byte[ivAndCipherTextLength - IvSize];

                Array.Copy(ivAndCipherText, 0, iv, 0, IvSize);
                Array.Copy(ivAndCipherText, IvSize, cipherTextBytes, 0, cipherTextBytes.Length);

                // Decrypt
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = this.encryptionKey;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipherTextBytes))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException)
            {
                // Re-throw cryptographic exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AesEncryptionService] Decryption failed: {ex.Message}");
                throw new CryptographicException("Failed to decrypt data", ex);
            }
        }

        /// <summary>
        /// Constant-time byte array comparison to prevent timing attacks.
        /// </summary>
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            var result = 0;
            for (var i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
