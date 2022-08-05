using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

public class ProtoComHostedService : IHostedService
{
    private ActorSystem _actorSystem;

    public ProtoComHostedService(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting a cluster member");
        
        await _actorSystem
            .Cluster()
            .StartMemberAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Shutting down a cluster member");
        
        await _actorSystem
            .Cluster()
            .ShutdownAsync();    }
}