using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Models;
using Shared.Settings;

namespace MessageProcessor
{
    public class ServiceBusWorker : BackgroundService
    {
        private readonly ILogger<ServiceBusWorker> _logger;
        private readonly ServiceBusProcessor _serviceBusProcessor;
        private readonly MessageProcessor _messageProcessor;
        public ServiceBusWorker(
            ILogger<ServiceBusWorker> logger, 
            IOptions<ServiceBusSettings> sbConfig,
            IServiceProvider serviceProvider)
        {
            var serviceBusClient = new ServiceBusClient(sbConfig.Value.ConnectionString);
            _serviceBusProcessor = serviceBusClient.CreateProcessor(sbConfig.Value.QueueName);
            _serviceBusProcessor.ProcessMessageAsync += MessageHandler;
            _serviceBusProcessor.ProcessErrorAsync += ErrorHandler;

            var scope = serviceProvider.CreateScope();
            _messageProcessor = scope.ServiceProvider.GetRequiredService<MessageProcessor>();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _serviceBusProcessor.StartProcessingAsync(stoppingToken);
                _logger.LogInformation("ServiceBus worker running at: {time}", DateTimeOffset.Now);

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _serviceBusProcessor.StopProcessingAsync(stoppingToken);
                await base.StopAsync(stoppingToken);
                _logger.LogInformation("ServiceBus worker stopped at: {time}", DateTimeOffset.Now);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            var message = args.Message.Body.ToObjectFromJson<BusMessage>();
            var actionType = await _messageProcessor.DetermineActionTypeAsync(message.MessageId);
            await _messageProcessor.Process(actionType, message.MessageId);

            await args.CompleteMessageAsync(args.Message);
        }

        private async Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError($"Message handler encountered an exception {args.Exception}.");
            await Task.CompletedTask;
        }
    }

}
