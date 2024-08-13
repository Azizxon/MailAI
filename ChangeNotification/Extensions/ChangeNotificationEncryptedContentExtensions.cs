using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Graph.Models;
using Newtonsoft.Json;

namespace ChangeNotification.Extensions
{
    public static class ChangeNotificationEncryptedContentExtensions
    {
        public static async Task<T?> DecryptAsync<T>(
            this ChangeNotificationEncryptedContent encryptedContent,
            Func<string, string, Task<X509Certificate2>> certificateProvider,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                if (certificateProvider == null)
                    throw new ArgumentNullException(nameof(certificateProvider));

                var stringContent = await encryptedContent.DecryptAsync(certificateProvider, cancellationToken)
                    .ConfigureAwait(false);
                var jsonObject = JsonConvert.DeserializeObject<T>(stringContent);
                return jsonObject;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return null;
        }

        private static async Task<string> DecryptAsync(
            this ChangeNotificationEncryptedContent encryptedContent,
            Func<string, string, Task<X509Certificate2>> certificateProvider,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (certificateProvider == null)
                    throw new ArgumentNullException(nameof(certificateProvider));

                _ = encryptedContent.EncryptionCertificateId ??
                    throw new Exception("Certificate ID missing in encrypted content");
                _ = encryptedContent.EncryptionCertificateThumbprint ??
                    throw new Exception("Certificate thumbprint missing in encrypted content");
                _ = encryptedContent.DataKey ??
                    throw new Exception("Data key missing in encrypted content");
                _ = encryptedContent.Data ??
                    throw new Exception("Data missing in encrypted content");

                using var certificate = await certificateProvider(encryptedContent.EncryptionCertificateId,
                    encryptedContent.EncryptionCertificateThumbprint).ConfigureAwait(false);
                using var rsaPrivateKey = certificate.GetRSAPrivateKey() ??
                                          throw new Exception("Could not get RSA private key from certificate");
                var decryptedSymmetricKey = rsaPrivateKey.Decrypt(Convert.FromBase64String(encryptedContent.DataKey),
                    RSAEncryptionPadding.OaepSHA1);
                using var hashAlg = new HMACSHA256(decryptedSymmetricKey);
                var expectedSignatureValue =
                    Convert.ToBase64String(hashAlg.ComputeHash(Convert.FromBase64String(encryptedContent.Data)));
                if (!string.Equals(encryptedContent.DataSignature, expectedSignatureValue))
                {
                    throw new InvalidDataException("Signature does not match");
                }

                var contentBytes = await AesDecryptAsync(Convert.FromBase64String(encryptedContent.Data), decryptedSymmetricKey, cancellationToken);
                return Encoding.UTF8.GetString(contentBytes);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return default;
        }

        private static async Task<byte[]> AesDecryptAsync(
            byte[] dataToDecrypt,
            byte[] key,
            CancellationToken cancellationToken = default)
        {
            try
            {
#pragma warning disable SYSLIB0021
                using var cryptoServiceProvider = new AesCryptoServiceProvider();
                cryptoServiceProvider.Mode = CipherMode.CBC;
                cryptoServiceProvider.Padding = PaddingMode.PKCS7;
                cryptoServiceProvider.Key = key;
#pragma warning restore SYSLIB0021
                var numArray = new byte[16]; //16 is the IV size for the decryption provider required by specification
                Array.Copy(key, numArray, numArray.Length);
                cryptoServiceProvider.IV = numArray;
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, cryptoServiceProvider.CreateDecryptor(),
                    CryptoStreamMode.Write);
                await cryptoStream.WriteAsync(dataToDecrypt, 0, dataToDecrypt.Length, cancellationToken);
                await cryptoStream.FlushFinalBlockAsync(cancellationToken);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unexpected error occurred while trying to decrypt the input", ex);
            }
        }
    }
}
