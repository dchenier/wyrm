# Wyrm
Wyrm connects distributed services through a common API.

## Example (Using RabbitMQ provider)
```csharp
public static async Task Main(string[] args)
{
    Host.CreateDefaultBuilder(args)
        .ConfigureServices(serviceCollection =>
        {
            serviceCollection.AddWyrm(options =>
            {
                // configure a provider, such as RabbitMQ
                options.UseRabbitMq("rabbitmq")

                // Add one or more services here
                    .AddEventHandler<MyEventService>()
                    .AddEventHandler<MyOtherService>();
            });
         }
}

// The EventAttribute listens for messages, converts the payload
// in the event to the Model class, and calls the HandleAsync method
[Wyrm.Events.Event("A.RabbitMQ.Queue")]
public class MyEventService : Wyrm.Events.EventHandler<Model> {
    public override async Task HandleAsync(Model payload, CancellationToken token)
    {
        // ...handle queue message here
        await doSomeWork(payload, token);
    }
}

// If a return value is specified, it will be posted to another queue
[Wyrm.Events.Event("Some.RabbitMQ.Queue")]
[Wyrm.Events.Event("Another.RabbitMQ.Queue", Direction=Direction.Out)]
public class MyOtherService : Wyrm.Events.EventHandler<InModel, OutModel> {

    // the returned result is serialized and posted to the out queue (if not null)
    public override async Task<OutModel> HandleAsync(InModel payload, CancellationToken token)
    {
        await doSomeWork(payload, token); 
        return new OutModel(); 
    }
}
```

## Configuration
```csharp
public static async Task Main(string[] args)
{
    Host.CreateDefaultBuilder(args)
        .ConfigureServices(serviceCollection =>
        {
            serviceCollection.AddWyrm(options =>
            {
                // configure a provider, such as RabbitMQ
                // host is required; port is optional
                options.UseRabbitMq(host: "rabbitmq", port: 5672);

                // Add one or more services here
                options.AddEventHandler<EventService1>();

                // with configuration
                options.AddEventHandler<EventService2>(options: opt => 
                {
                    // can have multiple instances for parallel processing
                    opt.InstanceCount = 10;
                });

                // implementation factory pattern:
                options.AddEventHandler<EventService3>(implementationFactory: svcs => 
                {
                    return new EventService3();
                }
            });
         }
}
```

