using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proto.Cluster;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Testing;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using ProtoCom.Api;
using ProtoCom.Api.Modules.Cart;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddActorSystem(hostContext.Configuration);

        if(hostContext.HostingEnvironment.IsDevelopment())
        {
            Console.WriteLine("==> Development mode");
            services.AddSingleton<IClusterProvider>(new TestProvider(new TestProviderOptions(), new InMemAgent()));
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
        services.AddHostedService<ProtoComHostedService>();
    })
    .Build();

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
Proto.Log.SetLoggerFactory(loggerFactory);

await host.RunAsync();