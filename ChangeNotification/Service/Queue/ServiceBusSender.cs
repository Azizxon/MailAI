using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Models;
using Shared.Settings;

namespace ChangeNotification.Service.Queue
{
    public class ServiceBusSender(
        IOptions<ServiceBusSettings> serviceBusSettings,
        ILogger<ServiceBusSender> logger)
    {
        public async Task SendMessageAsync(string messageId)
        {
            // Create a Service Bus client using the connection string
            var client = new ServiceBusClient(serviceBusSettings.Value.ConnectionString);

            // Get a sender for the specified queue
            var sender = client.CreateSender(serviceBusSettings.Value.QueueName);

            // Create a message object
            var jsonObject = JsonConvert.SerializeObject(new BusMessage()
            {
                MessageId = messageId
            });
            var serviceBusMessage = new ServiceBusMessage(jsonObject)
            {
                MessageId = string.Concat(messageId.Take(128))
            };

            try
            {
                // Send the message to the queue
                await sender.SendMessageAsync(serviceBusMessage);
                logger.LogInformation($"Message sent successfully: {messageId}");
            }
            finally
            {
                // Dispose of the sender and client objects
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
}
