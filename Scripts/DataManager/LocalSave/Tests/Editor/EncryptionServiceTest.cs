namespace DataManager.LocalSave.Tests.Editor
{
    using DataManager.LocalSave.Encryption;
    using UnityEngine;

    /// <summary>
    /// Diagnostic tool to test encryption service functionality.
    /// Add this to a MonoBehaviour in your scene and check console logs.
    /// </summary>
    public class EncryptionServiceTest : MonoBehaviour
    {
        [SerializeField] private bool runTestOnStart = false;
        
        private void Start()
        {
            if (this.runTestOnStart)
            {
                this.RunEncryptionTest();
            }
        }

        [ContextMenu("Run Encryption Test")]
        public void RunEncryptionTest()
        {
            Debug.Log("=== Encryption Service Test Starting ===");

            var encryptionService = new AesEncryptionService();

            // Test 1: Basic round-trip
            var testData = "{ \"level\": 10, \"gold\": 1000, \"items\": [\"sword\", \"shield\"] }";
            Debug.Log($"Original: {testData}");

            var encrypted = encryptionService.Encrypt(testData);
            Debug.Log($"Encrypted (Base64): {encrypted.Substring(0, Mathf.Min(50, encrypted.Length))}...");
            Debug.Log($"Encrypted Length: {encrypted.Length} characters");

            var decrypted = encryptionService.Decrypt(encrypted);
            Debug.Log($"Decrypted: {decrypted}");

            if (testData == decrypted)
            {
                Debug.Log("<color=green>✓ Test 1 PASSED: Round-trip encryption/decryption</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Test 1 FAILED: Data mismatch!</color>");
            }

            // Test 2: Different IVs for same input
            var encrypted1 = encryptionService.Encrypt(testData);
            var encrypted2 = encryptionService.Encrypt(testData);

            if (encrypted1 != encrypted2)
            {
                Debug.Log("<color=green>✓ Test 2 PASSED: Different IVs generated</color>");
            }
            else
            {
                Debug.LogWarning("<color=yellow>⚠ Test 2 WARNING: Same encrypted output (IVs not random?)</color>");
            }

            // Test 3: Empty string handling
            var emptyEncrypted = encryptionService.Encrypt("");
            var emptyDecrypted = encryptionService.Decrypt(emptyEncrypted);

            if (emptyDecrypted == "")
            {
                Debug.Log("<color=green>✓ Test 3 PASSED: Empty string handling</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Test 3 FAILED: Empty string not handled correctly</color>");
            }

            // Test 4: Large data
            var largeData = new string('A', 10000) + " - Large JSON Data";
            var largeEncrypted = encryptionService.Encrypt(largeData);
            var largeDecrypted = encryptionService.Decrypt(largeEncrypted);

            if (largeData == largeDecrypted)
            {
                Debug.Log($"<color=green>✓ Test 4 PASSED: Large data ({largeData.Length} chars)</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Test 4 FAILED: Large data corruption</color>");
            }

            // Test 5: Tamper detection
            try
            {
                var tamperedData = encrypted.Substring(0, encrypted.Length - 1) + "X"; // Corrupt last character
                encryptionService.Decrypt(tamperedData);
                Debug.LogError("<color=red>✗ Test 5 FAILED: Tampered data not detected!</color>");
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                Debug.Log("<color=green>✓ Test 5 PASSED: Tampered data detected</color>");
            }

            Debug.Log("=== Encryption Service Test Complete ===");
        }
    }
}
