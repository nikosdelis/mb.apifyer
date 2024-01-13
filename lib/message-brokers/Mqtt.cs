using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

public sealed class MqttApifyer : IMessageBroker
{
    private MqttClientOptions _options;
    private IMqttClient _mqttClient;
    private OnMessageReceivedHandler _MessageReceivedHandlerDelegate;

    private MqttApifyer() { throw new InvalidOperationException("Default constructor is not supported. Please use the constructor that accepts dependencies."); }

    /// <summary>
    /// Anonymously connects to the MQTT server
    /// </summary>
    /// <param name="mqttServer">127.0.0.1 for localhost</param>
    /// <param name="clientId">Usually device name</param>
    /// <param name="port">1883 by default</param>
    public MqttApifyer(string mqttServer, string clientId, int port = 1883)
    {
        _options = new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithKeepAlivePeriod(TimeSpan.FromMinutes(1))
                    .WithTcpServer(mqttServer, port)
                    .WithCleanStart()
                    .Build();

        _mqttClient = new MqttFactory().CreateMqttClient();
    }

    public void Connect()
    {
        _mqttClient.ApplicationMessageReceivedAsync += async (e) =>
        {
            _MessageReceivedHandlerDelegate(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
        };

        var connResult = _mqttClient.ConnectAsync(_options, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void RegisterOnMessageReceivedHandler(OnMessageReceivedHandler onMessageReceived)
    {
        _MessageReceivedHandlerDelegate += onMessageReceived;
    }

    public void Dispose()
    {
        _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync(object msg, string topic, CancellationToken cancellationToken)
    {
        if (!_mqttClient.IsConnected) Connect();

        var payloadString = System.Text.Json.JsonSerializer.Serialize(msg);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payloadString)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag(false)
            .Build();

        await _mqttClient.PublishAsync(message, cancellationToken);
    }

    public async Task SubscribeAsync(string topic)
    {
        await _mqttClient.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce, CancellationToken.None);
    }

    public async Task UnsubscribeAsync(string topic)
    {
        await _mqttClient.UnsubscribeAsync(topic, CancellationToken.None);
    }
}