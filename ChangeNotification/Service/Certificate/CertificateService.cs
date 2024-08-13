using System.Security.Cryptography.X509Certificates;

namespace ChangeNotification.Service.Certificate;

public class CertificateService(
    ILogger<CertificateService> logger)
{
    private readonly ILogger<CertificateService> _logger = logger ??
        throw new ArgumentException(nameof(logger));

    private byte[]? _publicKeyBytes;

    private byte[]? _privateKeyBytes;

    public async Task<X509Certificate2> GetEncryptionCertificate()
    {
        if (_publicKeyBytes == null)
        {
            await LoadCertificates();
        }

        var certificate = new X509Certificate2(_publicKeyBytes ??
                                               throw new Exception("Could not load encryption certificate"));
        return certificate;
    }

    public async Task<X509Certificate2> GetDecryptionCertificate()
    {
        if (_privateKeyBytes == null)
        {
            await LoadCertificates();
        }

        return new X509Certificate2(_privateKeyBytes ??
            throw new Exception("Could not load decryption certificate"));
    }

    /// <summary>
    /// Gets the public and private keys from Azure Key Vault and caches the raw values.
    /// </summary>
    private async Task LoadCertificates()
    {
        try
        {
            X509Certificate2 cert = new X509Certificate2("certificate.pfx",string.Empty, keyStorageFlags:X509KeyStorageFlags.Exportable);

            _publicKeyBytes = cert.RawData;

            if (cert.HasPrivateKey)
            {
                var privateKey = cert.GetRSAPrivateKey();
               _privateKeyBytes = privateKey.ExportRSAPrivateKey();
            }
            else
            {
                throw new Exception("Unable to extract the private key.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

    }
}
