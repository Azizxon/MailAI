using Newtonsoft.Json;

namespace Shared.Settings
{
    public class ServiceBusSettings
    {
        [JsonProperty(nameof(ConnectionString))]
        public string ConnectionString { get; set; }

        [JsonProperty(nameof(QueueName))]
        public string QueueName { get; set; }
    }
}
