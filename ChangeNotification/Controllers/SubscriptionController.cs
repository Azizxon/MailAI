using ChangeNotification.Service.Certificate;
using ChangeNotification.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Settings;

namespace ChangeNotification.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController(
        GraphServiceClient graphServiceClient,
        IConfiguration configuration,
        SubscriptionStore subscriptionStore,
        CertificateService certificateService,
        IOptions<AdSettings> addSettings,
        ILogger<SubscriptionController> logger) : ControllerBase
    {
        public CertificateService CertificateService { get; } = certificateService;

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(await graphServiceClient.Subscriptions.GetAsync()) ?? throw new InvalidOperationException();
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
                return BadRequest();
            }
        }

        [HttpDelete("{subscriptionId}")]
        public async Task<IActionResult> DeleteById(string subscriptionId)
        {
            try
            {
                await graphServiceClient.Subscriptions[subscriptionId].DeleteAsync();
                return Ok();
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }

            return BadRequest();
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var notificationHost = configuration.GetValue<string>("NotificationHost");
                // Create the subscription
                var subscription = new Subscription
                {
                    ChangeType = "created",
                    NotificationUrl = $"{notificationHost}/api/listen",
                    LifecycleNotificationUrl = $"{notificationHost}/api/lifecycle",
                    Resource = "me/mailFolders('Inbox')/messages",
                    ClientState = Guid.NewGuid().ToString(),
                    // Subscription only lasts for one hour
                    ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(1),
                };
                var newSubscription = await graphServiceClient.Subscriptions.PostAsync(subscription);
                // Add the subscription to the subscription store
                var user = await graphServiceClient.Me.GetAsync();
                if (newSubscription != null && user != null)
                {
                    subscriptionStore.SaveSubscriptionRecord(new SubscriptionRecord
                    {
                        Id = newSubscription.Id,
                        UserId = user.Id,
                        TenantId = addSettings.Value.TenantId,
                        ClientState = newSubscription.ClientState,
                    });
                    return Ok(newSubscription);
                }
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }

            return BadRequest();
        }
    }
}
