namespace DataManager.LocalSave.Encryption
{
    /// <summary>
    /// Interface for encryption and decryption services to secure local data.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts plain text data into cipher text.
        /// </summary>
        /// <param name="plainText">The plain text string to encrypt</param>
        /// <returns>Base64-encoded encrypted cipher text</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts cipher text back into plain text.
        /// </summary>
        /// <param name="cipherText">Base64-encoded encrypted cipher text</param>
        /// <returns>Decrypted plain text string</returns>
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// Thrown when decryption fails (corrupted data or tampered)
        /// </exception>
        string Decrypt(string cipherText);
    }
}
