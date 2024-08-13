using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using ChangeNotification.Extensions;
using ChangeNotification.Service.Queue;
using Microsoft.Graph;
using System.Text.RegularExpressions;
using ChangeNotification.Service;
using Shared.Settings;

namespace ChangeNotification.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ListenController(
        SubscriptionStore subscriptionStore,
        GraphServiceClient graphServiceClient,
        ServiceBusSender serviceBusSender,
        ILogger<ListenController> logger,
        IOptions<AdSettings> adSettings) : ControllerBase
    {
        public GraphServiceClient GraphServiceClient { get; } = graphServiceClient;

        [HttpPost("listen")]
        [AllowAnonymous]
        public async Task<IActionResult> Listen([FromQuery] string? validationToken = null)
        {
            logger.LogDebug($"Listen message:{DateTime.Now}");
            try
            {
                if (!string.IsNullOrEmpty(validationToken))
                {
                    return Ok(validationToken);
                }

                using var bodyStreamReader = new StreamReader(Request.Body);
                var content = await bodyStreamReader.ReadToEndAsync();
                var notifications = JsonConvert.DeserializeObject<ChangeNotificationCollection>(content);

                if (notifications == null || notifications.Value == null) return Accepted();

                var areTokensValid = await notifications.AreTokensValid(adSettings.Value);
                if (!areTokensValid) return Unauthorized();

                if (notifications.Value == null)
                {
                    return Accepted();
                }

                foreach (var notification in notifications.Value.Where(n => n.EncryptedContent == null))
                {
                    var subscription = subscriptionStore.GetSubscriptionRecord(notification.SubscriptionId.ToString() ?? string.Empty);
                    // If this isn't a subscription we know about, or if client state doesn't match,
                    // ignore it
                    if (subscription != null && subscription.ClientState == notification.ClientState)
                    {
                        var messageId = GetMessageId(notification.Resource);
                        await serviceBusSender.SendMessageAsync(messageId);
                    }
                }

                return Accepted();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error listening notification: {ex.Message}", ex.ToString());
                return BadRequest();
            }
        }
        private static string GetMessageId(string resource)
        {
            Regex regex = new Regex(@"Users\/(.*)/Messages\/(.*)");
            var match = regex.Match(resource);
            if (match.Success)
            {
                return match.Groups[2].Value;
            }

            return default;
        }
    }
}
