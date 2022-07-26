using System.Text.Json;
using Carter;
using Microsoft.AspNetCore.Http.Json;
using Proto;
using ProtoCom.Api;

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

builder.Services.AddActorSystem();

var app = builder.Build();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
Log.SetLoggerFactory(loggerFactory);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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