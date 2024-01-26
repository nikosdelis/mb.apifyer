# Mb.APIfyer

In IoT, message based architecture is more than common and it's fantastic, right? Wouldn't it be great though, if you could get a response on your messages? Something like:
```
- Tell me your status
- My status is XXXXX
```
Now we can't really do that with message brokers. In best case we can get an Ack for our message. Mb.APIfyer tries to change exactly that! Mb.APIfyer (Mb is short for message broker) is a crazy little thing that wraps message brokers and makes them look and behave like APIs (or API-ish). Currently only MQTT is supported but others shouldn't be impossible to add. So instead of messages you send requests to endpoints (topics) with payloads (message body), and you get responses as well; just like any API.

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Installation

Nuget package available on <a href="https://www.nuget.org/packages/NikosDelis.OpenSource.MbApifyer">nuget.org</a>! The nuget package will be updated on every tag (major and minor versions get tagged).

## Usage

Register with Dependency Injection and APIfy your Message Broker.
```
string myClientId = $"anonymous-client-{Guid.NewGuid()}";

// Let's APIfy an Mqtt-client
MqttApifyer ma = new MqttApifyer("127.0.0.1", myClientId);

var serviceProvider = new ServiceCollection()
    .AddSingleton<MbApifyer>(m => new MbApifyer(myClientId, ma))
    .BuildServiceProvider();

```

Put your code in a nice using-statement so that your connection get disposed when you don't need it any more.
```
using (var apifyer = serviceProvider.GetService<MbApifyer>())
{
    ...
}
```

If you are going to receive requests, then you need to <b>register your endpoints</b>.
The APIfyer has already your clientId from the Constructor, just pass in an endpoint name and a Func (what will happen if you get a request to that endpoint) which gets and returns string.
```
await apifyer.RegisterEndpointAsync("ENDPOINT-NAME", (message) =>
{
    // Deserialize the message which comes in nice and serialized.
    // Do something
    
    string response = "something something something";
    return response;
});


```

You can send 2 types of request, either <b>fire & forget</b> or <b>fire & expect response</b>
```
// Fire & Forget (expects no response)
await apifyer.SendVoidRequestAsync("RECEIVER'S-CLIEND-ID", "ENDPOINT-NAME", new { Message = "Hello World" });

// Fire & expect response
string response = await apifyer.SendRequestAsync("RECEIVER'S-CLIEND-ID", "ENDPOINT-NAME", new { Message = "Hello World" });
```

## Contributing

All feedback is welcome. This project is very young and it could all be a brain fart. If you have a good idea, or want to create unit tests or add another message broker (for whom I have an interface called IMessageBroker), then just make me a Pull Request and we take it from there!

## License

This project is licensed under the [MIT License](LICENSE).

## Contact

Contact info available in my profile here.