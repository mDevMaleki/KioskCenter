using KioskCenter.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KioskCenter.Helper
{
    public class LicenseValidator
    {
        private readonly string _publicKey = """
-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEA1VUhSiowddvSnHtNmL4jOBvnWhf/d231oY/vw9OXAdxu+wYKvsWw
USBYRK7KyfWg6LDhHhzgHKXHxRDtffZQgtrjNrvW4ougKW3qnQye0CylEryBY7R7
fwPmhXqJbrKU3tUgFmAzryRih/wSy9rNmsDOLUkKVzxNg/Kme7oXnUwbwAaxpdwB
T+FamE7eSw046tCFKGc9Vnu0pN9Q/u1XjGMxi6Un8mbt495AxrRzroTRe7qelhTK
u0NrxJOQ/hBnLF+3gPdY/Ac/EueWzFialaKrlILCYBRuSqmA2fyNE3Swi2pPWDuu
wkKfT/pnNG+uul5n42VJ9ynTwbwUwBAprQIDAQAB
-----END RSA PUBLIC KEY-----
""";

        public bool Validate(SignedLicense envelope, string currentHardwareHash)
        {
            // 1️⃣ Verify RSA signature
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_publicKey);

            var data = Encoding.UTF8.GetBytes(envelope.PayloadJson);
            var signature = Convert.FromBase64String(envelope.SignatureBase64);

            var valid = rsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            if (!valid)
                return false;

            // 2️⃣ Deserialize payload
            var payload = JsonSerializer.Deserialize<LicensePayload>(envelope.PayloadJson);

            if (payload == null)
                return false;

            // 3️⃣ Check expiration
            if (payload.ExpiresAtUtc < DateTime.UtcNow)
                return false;

            // 4️⃣ Check hardware binding
            if (payload.HardwareHash != currentHardwareHash)
                return false;

            return true;
        }
    }
}
