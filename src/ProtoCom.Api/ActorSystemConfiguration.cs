using Modules.Cart;
using Modules.Product;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using ProtoCom.Contracts;

namespace ProtoCom.Api;

public static class ActorSystemConfiguration
{
    public static void AddActorSystem(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton(provider =>
        {
            // actor system configuration

            var actorSystemConfig = ActorSystemConfig
                .Setup();

            // remote configuration

            var remoteConfig = GrpcNetRemoteConfig
                .BindToAllInterfaces(advertisedHost: configuration["ProtoActor:AdvertisedHost"])
                .WithProtoMessages(MessagesReflection.Descriptor);

            // cluster configuration

            var clusterConfig = ClusterConfig
                .Setup(
                    clusterName: "ProtoClusterTutorial",
                    clusterProvider: provider.GetService<IClusterProvider>(),
                    // clusterProvider: new TestProvider(new TestProviderOptions(), new InMemAgent()),
                    identityLookup: new PartitionIdentityLookup()
                );
                // .WithClusterKind(
                //     kind: CartGrainActor.Kind,
                //     prop: Props.FromProducer(() =>
                //         new CartGrainActor(
                //             (context, clusterIdentity) =>
                //             {
                //                 context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
                //                 return new CartGrain(context, clusterIdentity);
                //             }
                //         )
                //     )
                // )
                // .WithClusterKind(
                //     kind: ProductGrainActor.Kind,
                //     prop: Props.FromProducer(() =>
                //         new ProductGrainActor(
                //             (context, clusterIdentity) =>
                //             {
                //                 context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
                //                 return new ProductGrain(context, clusterIdentity);
                //             }
                //         )
                //     )
                // );

            // create the actor system

            return new ActorSystem(actorSystemConfig)
                .WithServiceProvider(provider)
                .WithRemote(provider.GetService<GrpcNetRemoteConfig>())
                .WithCluster(clusterConfig);
        });
    }
}