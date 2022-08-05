using System.Text.Json;
using Carter;
using Microsoft.AspNetCore.Http.Json;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Testing;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using ProtoCom.Api;
using ProtoCom.Api.Modules.Cart;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.IncludeFields = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddHostedService<ActorSystemClusterHostedService>();

builder.Services.AddActorSystem(builder.Configuration);

if(builder.Environment.IsDevelopment())
{
    Console.WriteLine("==> Development mode");
    ConsulProviderConfig config = new ConsulProviderConfig();
    builder.Services.AddSingleton<IClusterProvider>(
                new ConsulProvider(config, conf => {
                    conf.Address = new Uri("http://localhost:8500");
                }));

    // builder.Services.AddSingleton<IClusterProvider>(new TestProvider(new TestProviderOptions(), new InMemAgent()));
    builder.Services.AddSingleton(
        GrpcNetRemoteConfig.BindToLocalhost()
    );
}
else {
    builder.Services.AddSingleton<IClusterProvider>(new KubernetesProvider());
    builder.Services.AddSingleton(
        GrpcNetRemoteConfig
            .BindToAllInterfaces(advertisedHost: builder.Configuration["ProtoActor:AdvertisedHost"])
            .WithProtoMessages(MessagesReflection.Descriptor)
    );
}

var app = builder.Build();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
Log.SetLoggerFactory(loggerFactory);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Lifetime.ApplicationStopping.Register(() => {
    var actorSystem = app.Services.GetService<ActorSystem>();
    Thread.Sleep(5000);
    Console.WriteLine("Stopping actor system");
    actorSystem.Cluster().ShutdownAsync().Wait();
    // Console.WriteLine("ApplicationStopping called, sleeping for 10s");
    
    // Thread.Sleep(5000);
});

app.MapCarter();

app.Run("http://*:5000");

// public static class ProtoActorExtensions
// {
//     public static void AddProtoActor(this IServiceCollection services)
//     {
//         services.AddSingleton(sp => {
//             var system = new ActorSystem();
//             return system;
//         });
//         services.AddSingleton<IRootContext>(sp => {
//             var system = sp.GetService<ActorSystem>()!;
//             var context = system.Root;
//             return context; 
//         });
//     }
// }