using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Cart;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Testing;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using ProtoCom.Api;
using ProtoCom.Contracts;
using Weasel.Core;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddAppActorSystem(hostContext.Configuration);

        if(hostContext.HostingEnvironment.IsDevelopment())
        {
            Console.WriteLine("==> Development mode");
            ConsulProviderConfig config = new ConsulProviderConfig();
            services.AddSingleton<IClusterProvider>(
                new ConsulProvider(config, conf => {
                    conf.Address = new Uri("http://localhost:8500");
                }));
                // new TestProvider(new TestProviderOptions(), new InMemAgent()));
            services.AddSingleton(
                GrpcNetRemoteConfig.BindToLocalhost()
            );
        }
        else {
            services.AddSingleton<IClusterProvider>(new KubernetesProvider());
            services.AddSingleton(
                GrpcNetRemoteConfig
                    .BindToAllInterfaces(advertisedHost: hostContext.Configuration["ProtoActor:AdvertisedHost"])
                    .WithProtoMessages(MessagesReflection.Descriptor)
            );
        }
        services
            .AddHostedService<ProtoComHostedService>()
            .AddMarten(options => {
                options.Connection("host=localhost;port=5432;database=protocom;user id=protocom;password=password");
                options.AutoCreateSchemaObjects = AutoCreate.All;
                options.Events.AddEventType(typeof(CartCreated));
                options.Events.AddEventType(typeof(CartItemAdded));
                options.Events.AddEventType(typeof(CartItemRemoved));
            });
    })
    .Build();

// app.Lifetime.ApplicationStopping.Register(() => {
//     var actorSystem = app.Services.GetService<ActorSystem>();
//     Thread.Sleep(5000);
//     Console.WriteLine("Stopping actor system");
//     actorSystem.Cluster().ShutdownAsync().Wait();
//     // Console.WriteLine("ApplicationStopping called, sleeping for 10s");
    
//     // Thread.Sleep(5000);
// });

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
Proto.Log.SetLoggerFactory(loggerFactory);

await host.RunAsync();