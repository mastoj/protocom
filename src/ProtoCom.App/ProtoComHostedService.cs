using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

public class ProtoComHostedService : IHostedService
{
    private ActorSystem _actorSystem;
    private IHostApplicationLifetime _hostApplicationLifetime;

    public ProtoComHostedService(ActorSystem actorSystem, IHostApplicationLifetime hostApplicationLifetime)
    {
        _actorSystem = actorSystem;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting a cluster member");
        
        _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);

        await _actorSystem
            .Cluster()
            .StartMemberAsync();
    }

    private void OnStopping()
    {
        Console.WriteLine("Stopping a cluster member");
        _actorSystem.ShutdownAsync().Wait();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Shutting down a cluster member");
        
        await _actorSystem
            .Cluster()
            .ShutdownAsync();    }
}