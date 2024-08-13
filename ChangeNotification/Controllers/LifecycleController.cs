using ChangeNotification.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json;

namespace ChangeNotification.Controllers
{
    [Route("api/")]
    [ApiController]
    public class LifecycleController(
        GraphServiceClient graphClient,
        SubscriptionStore subscriptionStore,
        ILogger<LifecycleController> logger) : ControllerBase
    {
        [HttpPost("lifecycle")]
        [AllowAnonymous]
        public async Task<IActionResult> Lifecycle([FromQuery] string? validationToken = null)
        {
            try
            {
                logger.LogInformation($"Received notification for Lifecycle with validationToken: {validationToken}");
                // If there is a validation token in the query string,
                // send it back in a 200 OK text/plain response
                if (!string.IsNullOrEmpty(validationToken))
                {
                    return Ok(validationToken);
                }

                using var bodyStreamReader = new StreamReader(Request.Body);
                var content = await bodyStreamReader.ReadToEndAsync();
                var notifications = JsonConvert.DeserializeObject<ChangeNotificationCollection>(content);

                if (notifications == null || notifications.Value == null) return Accepted();

                logger.LogInformation($"Lifecycle notification with validationToken was parsed: {validationToken}");

                // Process any lifecycle events
                var lifecycleNotifications = notifications.Value.Where(n => n.LifecycleEvent != null);
                foreach (var lifecycleNotification in lifecycleNotifications)
                {
                    logger.LogInformation("Received {eventType} notification for subscription {subscriptionId}",
                        lifecycleNotification.LifecycleEvent.ToString(), lifecycleNotification.SubscriptionId);

                    if (lifecycleNotification.LifecycleEvent == LifecycleEventType.ReauthorizationRequired)
                    {
                        await RenewSubscriptionAsync(lifecycleNotification);
                        logger.LogInformation($"Lifecycle notification with validationToken was handled: {validationToken}");
                    }
                }

                logger.LogInformation($"Lifecycle notification with validationToken was finished: {validationToken}");
                // Return 202 to Graph to confirm receipt of notification.
                // Not sending this will cause Graph to retry the notification.
                return Accepted();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error listening notification: {ex.Message}", ex.ToString());
                return BadRequest();
            }
        }

        private async Task RenewSubscriptionAsync(Microsoft.Graph.Models.ChangeNotification lifecycleNotification)
        {
            var subscriptionId = lifecycleNotification.SubscriptionId?.ToString();

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                var subscription = subscriptionStore.GetSubscriptionRecord(subscriptionId);
                if (subscription != null &&
                    !string.IsNullOrEmpty(subscription.UserId) &&
                    !string.IsNullOrEmpty(subscription.TenantId))
                {
                    var update = new Subscription
                    {
                        ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(1),
                    };

                    await graphClient.Subscriptions[subscriptionId].PatchAsync(update);

                    logger.LogInformation("Renewed subscription");
                }
            }
        }
    }
}
