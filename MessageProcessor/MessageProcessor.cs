using System.ClientModel;
using Microsoft.Graph;
using Microsoft.Graph.Me.Messages.Item.Move;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace MessageProcessor
{
    internal class MessageProcessor(
        IConfiguration configuration,
        GraphServiceClient graphServiceClient,
        ILogger<MessageProcessor> logger
        )
    {
        public async Task<ChatMessageContentPart?> GetCompleteChatAsync(string prompt)
        {
            try
            {
                var endpoint = configuration.GetValue<string>("OpenAiEndpoint");
                var key = configuration.GetValue<string>("OpenAiKey");
                var openAiClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
                var chatClient = openAiClient.GetChatClient("gpt-4");

                var message = new SystemChatMessage(prompt);
                var completion = await chatClient.CompleteChatAsync(new ChatMessage[] { message });

                return completion.Value.Content.FirstOrDefault();
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }

            return default;
        }

        public async Task<ActionType> DetermineActionTypeAsync(string messageId)
        {
            var message = await graphServiceClient.Me.Messages[messageId].GetAsync();
            if (message is { Body: not null })
            {
                var prompt = @$"If TEXT below contains salary information for current month return MOVE
                                Otherwise DELETE;
                                Current Date: {DateTime.UtcNow}
                                TEXT:{message.Body.Content}";

                var completion = await GetCompleteChatAsync(prompt);
                if (completion != null)
                {
                    if (completion.Text.Contains("MOVE"))
                    {
                        return ActionType.Move;
                    }
                    if (completion.Text.Contains("DELETE"))
                    {
                        return ActionType.Delete;
                    }
                }
            }

            return ActionType.None;
        }
        public async Task Process(ActionType actionType, string messageId)
        {
            switch (actionType)
            {
                case ActionType.Delete:
                    await graphServiceClient.Me.Messages[messageId].DeleteAsync();
                    break;
                case ActionType.Move:
                    var mailFolders = await graphServiceClient.Me.MailFolders.GetAsync();
                    if (mailFolders is { Value: not null })
                    {
                        var importantFolder = mailFolders.Value.FirstOrDefault(mf => mf.DisplayName != null && mf.DisplayName.Contains("Important"));
                        if (importantFolder is not null)
                        {
                            var moveRequest = new MovePostRequestBody
                            {
                                DestinationId = importantFolder.Id
                            };
                            await graphServiceClient.Me.Messages[messageId].Move.PostAsync(moveRequest);
                        }
                    }
                    break;
            }
        }
    }
}
