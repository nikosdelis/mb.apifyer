using System;
using System.Threading;
using System.Threading.Tasks;

public delegate void OnMessageReceivedHandler(string topic, string message);

public interface IMessageBroker : IDisposable
{
    public void RegisterOnMessageReceivedHandler(OnMessageReceivedHandler onMessageReceived);
    public void Connect();
    public Task PublishAsync(object message, string topic, System.Threading.CancellationToken cancellationToken);
    public Task SubscribeAsync(string topic);
    public Task UnsubscribeAsync(string topic);
}