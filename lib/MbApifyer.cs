using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a message broker apifyer that allows registering endpoints and sending/receiving messages.
/// </summary>
public sealed class MbApifyer : IDisposable
{
    private readonly string _myRecipientId;
    private readonly IMessageBroker _broker;
    private Dictionary<string, Func<object, string>> _RegisteredEndpoints = new Dictionary<string, Func<object, string>>();
    private Dictionary<string, string> _AwaitingResponses = new Dictionary<string, string>();
    private MbApifyer() { throw new InvalidOperationException("Default constructor is not supported. Please use the constructor that accepts dependencies."); }

    public MbApifyer(string recipientId, IMessageBroker broker)
    {
        _myRecipientId = recipientId;
        _broker = broker;
        
        _broker.RegisterOnMessageReceivedHandler(async (topic, message) =>
        {
            if (topic.EndsWith("/response"))
                _AwaitingResponses[topic] = message;
            else if (topic.EndsWith("/request"))
            {
                string matchingEndpoint = _RegisteredEndpoints.Keys.FirstOrDefault(x => topic.StartsWith(x)) ?? string.Empty;
                
                if (!string.IsNullOrEmpty(matchingEndpoint))
                {
                    var response = _RegisteredEndpoints[matchingEndpoint](message);

                    string responseTopic = topic.Replace("/request", "/response");

                    await _broker.PublishAsync(response, responseTopic, CancellationToken.None);
                }
            }
        });

        _broker.Connect();
    }

    public async Task RegisterEndpointAsync(string endpoint, Func<object, string> actionToPerform)
    {
        await _broker.SubscribeAsync($"apify/{_myRecipientId}/{endpoint}/+/request");

        _RegisteredEndpoints[$"apify/{_myRecipientId}/{endpoint}/"] = actionToPerform;
    }

    public async Task SendVoidRequestAsync(string recipientId, string endpoint, object payload)
    {
        Guid requestId = Guid.NewGuid();
        string requestTopic = $"apify/{recipientId}/{endpoint}/{requestId}/request";

        await _broker.PublishAsync(payload, requestTopic, CancellationToken.None);
    }

    public async Task<string> SendRequestAsync(string recipientId, string endpoint, object payload, int timeoutInSeconds = 30)
    {
        string response = string.Empty;

        Guid requestId = Guid.NewGuid();
        string requestTopic = $"apify/{recipientId}/{endpoint}/{requestId}/request";
        string responseTopic = $"apify/{recipientId}/{endpoint}/{requestId}/response";

        await _broker.SubscribeAsync(responseTopic);

        await _broker.PublishAsync(payload, requestTopic, CancellationToken.None);

        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));
        
        while (!cts.IsCancellationRequested)
        {
            if (_AwaitingResponses.ContainsKey(responseTopic))
            {
                response = _AwaitingResponses[responseTopic];
                _AwaitingResponses.Remove(responseTopic);
                await _broker.UnsubscribeAsync(responseTopic);

                break;
            }
            await Task.Delay(100);
        }

        cts.Token.ThrowIfCancellationRequested();

        return response;
    }

    public void Dispose()
    {
        _broker.Dispose();
    }
}
