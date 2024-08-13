using ChangeNotification.Service.Certificate;
using Microsoft.Graph.Models;

namespace ChangeNotification.Extensions
{
    public static class ChangeNotificationExtension
    {
        public static async Task<Message> DecryptAsync(this Microsoft.Graph.Models.ChangeNotification notification,
            CertificateService certificateService)
        {
            var message = await notification.EncryptedContent!.DecryptAsync<Message>(
                async (_, _) =>
                {
                    var cert = await certificateService.GetDecryptionCertificate();
                    return cert;
                });

            return message;
        }
    }
}
